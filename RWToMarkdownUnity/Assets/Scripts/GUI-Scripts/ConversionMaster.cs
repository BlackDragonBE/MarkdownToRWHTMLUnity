using UnityEngine;
using System.Collections;
using DragonMarkdown.DragonConverter;
using Crosstales.FB;
using System.IO;
using DragonMarkdown.ContentScan;
using DragonMarkdown.Utility;
using TMPro;
using CielaSpike;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ConversionMaster : MonoBehaviour
{
    public TMP_InputField MarkdownText;
    public TMP_InputField HtmlText;
    public TextMeshProUGUI LinkImageText;
    public Slider StatusSlider;
    public TextMeshProUGUI StatusText;

    public GameObject LoadingCanvas;
    public GameObject ImageLinkerCancelButton;

    private string _markdownPath;
    private string _htmlPath;

    private bool _useContentScanner;
    private bool _saveOutputToHtml;

    private CustomCertificateHandler certHandler;
    private bool _lastFileFoundOnServer = false;

    // Use this for initialization
    private void Start()
    {
        certHandler = new CustomCertificateHandler();
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    public void OnPointerDown(PointerEventData eventData)
    {
        UploadFile(gameObject.name, "OnFileUpload", ".md", false);
    }

    // Called from browser
    public void OnFileUpload(string url)
    {
        StartCoroutine(OutputRoutine(url));
    }
#else
    //
    // Standalone platforms & editor
    //

#endif

    private IEnumerator OutputRoutine(string url)
    {
        var loader = new WWW(url);
        yield return loader;
        MarkdownText.text = loader.text;
    }

    public void DoConversion()
    {
        string path;

        // CreateAndSaveSettings();
        path = FileBrowser.OpenSingleFile("Open Markdown File", "",
                        new ExtensionFilter[] { new ExtensionFilter("Markdown Files", new string[] { "md", "markdown", "mdown","mkdn",
                        "mkd","mdwn","mdtxt","mdtext","text","txt","rmd"}) });

        ShowLoadingScreen();

        Task task;
        this.StartCoroutineAsync(Convert(path), out task);
    }

    private IEnumerator Convert(string path)
    {
        ConverterOptions options = GetConverterOptions();

        if (path != null)
        {
            if (File.Exists(path))
            {
                _markdownPath = path;
                _htmlPath = null;

                using (StreamReader sr = new StreamReader(path))
                {
                    string md = sr.ReadToEnd().Replace("\t", "  "); ;
                    string html = Converter.ConvertMarkdownStringToHtml(md, options);
                    yield return Ninja.JumpToUnity;

                    MarkdownText.text = md;
                    HtmlText.text = html;
                    yield return Ninja.JumpBack;

                    if (_useContentScanner)
                    {
                        print(ContentScanner.ParseScanrResults(ContentScanner.ScanMarkdown(MarkdownText.text, _markdownPath)));
                    }

                    if (_saveOutputToHtml)
                    {
                        _htmlPath = DragonUtil.GetFullPathWithoutExtension(path) + ".html";
                        Converter.ConvertMarkdownFileToHtmlFile(path, _htmlPath, options);
                    }

                    yield return Ninja.JumpToUnity;

                    HideLoadingScreen();
                    StatusText.text = "Converted markdown! Copy HTML on right side or start Image Linker (experimental).";
                }
            }
        }
        else
        {
            yield return Ninja.JumpToUnity;
            HideLoadingScreen();
            StatusText.text = "No valid markdown chosen!";
        }
    }

    private ConverterOptions GetConverterOptions()
    {
        return new ConverterOptions
        {
            FirstImageIsAlignedRight = false,
            ReplaceImageWithAltWithCaption = true
        };
    }

    private void ShowLoadingScreen()
    {
        LoadingCanvas.SetActive(true);
    }

    private void HideLoadingScreen()
    {
        LoadingCanvas.SetActive(false);
    }

    public void StartLinking()
    {
        Task task;
        this.StartCoroutineAsync(LinkImages(), out task);
    }

    public IEnumerator LinkImages()
    {
        yield return Ninja.JumpToUnity;

        ImageLinkerCancelButton.SetActive(false);

        if (!File.Exists(_markdownPath))
        {
            WriteToLinkImageText("Markdown file not found! Aborting.");
            ImageLinkerCancelButton.SetActive(true);
            yield break;
        }

        string imageUrlPrefix = "https://koenig-media.raywenderlich.com/uploads/" + DateTime.Now.Year + "/" + DateTime.Now.Month.ToString("00") + "/";
        print(imageUrlPrefix);
        var links = Converter.FindAllImageLinksInHtml(HtmlText.text, Path.GetDirectoryName(_markdownPath));

        if (links.Count == 0)
        {
            WriteToLinkImageText("No images found to upload! Aborting.");
            ImageLinkerCancelButton.SetActive(true);
            yield break;
        }

        List<string> fullImagePaths = new List<string>();
        List<string> localImagePaths = new List<string>();
        List<string> fileNames = new List<string>();
        List<string> imageUrls = new List<string>();
        int failedLinks = 0;

        foreach (ImageLinkData link in links)
        {
            fullImagePaths.Add(link.FullImagePath);
            localImagePaths.Add(link.LocalImagePath);
            fileNames.Add(Path.GetFileName(link.FullImagePath));
        }

        LinkImageText.text = "";
        WriteToLinkImageText(fullImagePaths.Count + " image paths found.");
        WriteToLinkImageText("Started linking process...");

        StatusSlider.maxValue = fileNames.Count;

        for (int i = 0; i < fileNames.Count; i++)
        {
            string potentialUrl = imageUrlPrefix + fileNames[i];
            //WriteToLinkImageText("Checking " + potentialUrl);
            StatusText.text = "Attempting link " + (i + 1) + " / " + fileNames.Count;

            yield return URLExists(potentialUrl);

            if (_lastFileFoundOnServer)
            {
                WriteToLinkImageText(potentialUrl + " OK!");
                imageUrls.Add(potentialUrl);
            }
            else
            {
                WriteToLinkImageText("<color=#FF0000>" + potentialUrl + " NOT FOUND!</color>");
                failedLinks++;
                imageUrls.Add(null);
            }

            StatusSlider.value++;

            yield return new WaitForEndOfFrame();
        }

        MarkdownAndHtml markdownAndHtml = Converter.ReplaceLocalImageLinksWithUrls(_markdownPath, MarkdownText.text,
    null, HtmlText.text, true, localImagePaths, imageUrls);

        MarkdownText.text = markdownAndHtml.Markdown;
        HtmlText.text = markdownAndHtml.Html;

        if (failedLinks == 0)
        {
            StatusText.text = "Succesfully linked all " + fileNames.Count + " images!";
        }
        else
        {
            StatusText.text = "Failed to link " + failedLinks + " images. See text output.";
        }

        ImageLinkerCancelButton.SetActive(true);
        WriteToLinkImageText("Finished operation. Please copy the HTML from the right panel and paste it into new post.");
    }

    private void WriteToLinkImageText(string text)
    {
        LinkImageText.text += text + "\n";
    }

    public IEnumerator URLExists(string url)
    {
        UnityWebRequest www = UnityWebRequest.Head(url);
        www.certificateHandler = certHandler;

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError || www.responseCode > 400)
        {
            WriteToLinkImageText("Error: " + www.error);
            _lastFileFoundOnServer = false;
        }
        else
        {
            _lastFileFoundOnServer = true;
        }

        www.Dispose();
    }
}