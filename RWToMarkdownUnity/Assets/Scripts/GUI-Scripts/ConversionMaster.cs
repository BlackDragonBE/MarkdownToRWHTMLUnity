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
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;

public class ConversionMaster : MonoBehaviour
{
    public TMP_InputField MarkdownText;
    public TMP_InputField HtmlText;
    public TextMeshProUGUI LinkImageText;
    public Slider StatusSlider;
    public TextMeshProUGUI StatusText;

    public TextMeshProUGUI MarkdownPage;
    public TextMeshProUGUI HtmlPage;

    public GameObject LoadingCanvas;
    public GameObject WebGLUploadCanvas;
    public GameObject ImageLinkerCancelButton;

    public int MaximumCharactersPerPage = 1000;

    private string _markdownPath;
    private string _htmlPath;

    private string _markdown;
    private string _html;

    private bool _useContentScanner;
    private bool _saveOutputToHtml;

    private CustomCertificateHandler certHandler;
    private bool _lastFileFoundOnServer = false;

    private int _markdownPage;
    private int _htmlPage;

    // Use this for initialization
    private void Awake()
    {
        certHandler = new CustomCertificateHandler();
        MarkdownPage.text = "";
        HtmlPage.text = "";
    }

    // VERY EXPERIMENTAL WEBGL STUFF
#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    // Called from browser
    public void OnFileUpload(string url)
    {
        StartCoroutine(OutputRoutine(url));
    }
#endif

    private IEnumerator OutputRoutine(string url)
    {
        ShowLoadingScreen();

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                MarkdownText.text = www.error;
            }
            else
            {
                _markdown = www.downloadHandler.text;
                ConverterOptions options = GetConverterOptions();
                _html = Converter.ConvertMarkdownStringToHtml(www.downloadHandler.text, options);
                LoadMarkdownPage(0);
                LoadHtmlPage(0);
            }
        }

        HideLoadingScreen();
    }

    public void DoConversion()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLUploadCanvas.SetActive(true);
        UploadFile(gameObject.name, "OnFileUpload", ".md, .markdown, .txt, .mdown, .mkdn, .mkd, .mdwn, .mdtext, .mdtxt, .text, .rmd", false);
#else

        string path;

        // CreateAndSaveSettings();
        path = FileBrowser.OpenSingleFile("Open Markdown File", "",
                        new ExtensionFilter[] { new ExtensionFilter("Markdown Files", new string[] { "md", "markdown", "mdown","mkdn",
                        "mkd","mdwn","mdtxt","mdtext","text","txt","rmd"}) });

        ShowLoadingScreen();

        Task task;
        this.StartCoroutineAsync(Convert(path), out task);
#endif
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
                    _markdown = sr.ReadToEnd().Replace("\t", "  ");
                    _html = Converter.ConvertMarkdownStringToHtml(_markdown, options);

                    yield return Ninja.JumpToUnity;
                    LoadMarkdownPage(0);
                    LoadHtmlPage(0);
                    yield return Ninja.JumpBack;

                    if (_useContentScanner)
                    {
                        print(ContentScanner.ParseScanrResults(ContentScanner.ScanMarkdown(_markdown, _markdownPath)));
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
        StartCoroutine(LinkImages());
    }

    public IEnumerator LinkImages()
    {
        ImageLinkerCancelButton.SetActive(false);

        if (!File.Exists(_markdownPath))
        {
            WriteToLinkImageText("Markdown file not found! Aborting.");
            ImageLinkerCancelButton.SetActive(true);
            yield break;
        }

        string imageUrlPrefix = "https://koenig-media.raywenderlich.com/uploads/" + DateTime.Now.Year + "/" + DateTime.Now.Month.ToString("00") + "/";
        var links = Converter.FindAllImageLinksInHtml(_html, Path.GetDirectoryName(_markdownPath));

        if (links.Count == 0)
        {
            WriteToLinkImageText("No images found to upload! Aborting.");
            ImageLinkerCancelButton.SetActive(true);
            yield break;
        }

        List<string> localImagePaths = new List<string>();
        List<string> fileNames = new List<string>();
        List<string> imageUrls = new List<string>();
        int failedLinks = 0;

        foreach (ImageLinkData link in links)
        {
            localImagePaths.Add(link.LocalImagePath);
            fileNames.Add(Path.GetFileName(link.FullImagePath));
        }

        LinkImageText.text = "";
        WriteToLinkImageText(localImagePaths.Count + " image paths found.");
        WriteToLinkImageText("Started linking process...");

        StatusSlider.maxValue = fileNames.Count;

        for (int i = 0; i < fileNames.Count; i++)
        {
            string potentialUrl = imageUrlPrefix + fileNames[i];
            //WriteToLinkImageText("Checking " + potentialUrl);
            StatusText.text = "Attempting link " + (i + 1) + " / " + fileNames.Count;

            yield return UrlExists(potentialUrl);

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

        MarkdownAndHtml markdownAndHtml = Converter.ReplaceLocalImageLinksWithUrls(_markdownPath, _markdown,
    null, _html, true, localImagePaths, imageUrls);

        //MarkdownText.text = markdownAndHtml.Markdown;
        //HtmlText.text = markdownAndHtml.Html;
        markdownAndHtml.Html.CopyToClipboard();

        if (failedLinks == 0)
        {
            StatusText.text = "Succesfully linked all " + fileNames.Count + " images!";
        }
        else
        {
            StatusText.text = "Failed to link " + failedLinks + " images. See text output.";
        }

        ImageLinkerCancelButton.SetActive(true);
        WriteToLinkImageText("Finished operation. HTML has been copied to your clipboard.");
    }

    private void WriteToLinkImageText(string text)
    {
        LinkImageText.text += text + "\n";
    }

    public IEnumerator UrlExists(string url)
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

    public void ShowNextMarkdownPage()
    {
        if (_markdownPage > _markdown.Length / MaximumCharactersPerPage)
        {
            return;
        }

        _markdownPage++;
        LoadMarkdownPage(_markdownPage);
    }

    public void ShowPreviousMarkdownPage()
    {
        if (_markdownPage < 1)
        {
            return;
        }

        _markdownPage--;
        LoadMarkdownPage(_markdownPage);
    }

    private void LoadMarkdownPage(int page)
    {
        int firstCharIndex = page * MaximumCharactersPerPage;
        int lastCharIndex = firstCharIndex + MaximumCharactersPerPage;

        bool firstCharOK = _markdown.Length > firstCharIndex;
        bool lastCharOK = _markdown.Length > lastCharIndex;

        if (!firstCharOK)
        {
            return;
        }

        MarkdownPage.text = "Page " + (page + 1) + " / " + (_markdown.Length / MaximumCharactersPerPage + 1);

        if (!lastCharOK) // Not enough characters left to show just a part, show all of it
        {
            MarkdownText.text = _markdown.Substring(firstCharIndex);
        }
        else // Everything ok, show subset
        {
            MarkdownText.text = _markdown.Substring(firstCharIndex, MaximumCharactersPerPage);
        }
    }

    public void ShowNextHtmlPage()
    {
        if (_htmlPage > _html.Length / MaximumCharactersPerPage)
        {
            return;
        }

        _htmlPage++;
        LoadHtmlPage(_htmlPage);
    }

    public void ShowPreviousHtmlPage()
    {
        if (_htmlPage < 1)
        {
            return;
        }

        _htmlPage--;
        LoadHtmlPage(_htmlPage);
    }

    private void LoadHtmlPage(int page)
    {
        int firstCharIndex = page * MaximumCharactersPerPage;
        int lastCharIndex = firstCharIndex + MaximumCharactersPerPage;

        bool firstCharOK = _html.Length > firstCharIndex;
        bool lastCharOK = _html.Length > lastCharIndex;

        if (!firstCharOK)
        {
            return;
        }

        HtmlPage.text = "Page " + (page + 1) + " / " + (_html.Length / MaximumCharactersPerPage + 1);

        if (!lastCharOK) // Not enough characters left to show just a part, show all of it
        {
            HtmlText.text = _html.Substring(firstCharIndex);
        }
        else // Everything ok, show subset
        {
            HtmlText.text = _html.Substring(firstCharIndex, MaximumCharactersPerPage);
        }
    }

    public void CopyMarkdownToClipboard()
    {
        _markdown.CopyToClipboard();
    }

    public void CopyHtmlToClipboard()
    {
        _html.CopyToClipboard();
    }
}