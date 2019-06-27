using UnityEngine;
using System.Collections;
using DragonMarkdown.DragonConverter;
using Crosstales.FB;
using System.IO;
using DragonMarkdown.ContentScan;
using DragonMarkdown.Utility;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;

public class ConversionMaster : MonoBehaviour
{
    [HideInInspector]
    public string Markdown;

    [HideInInspector]
    public string HTML;

    [HideInInspector]
    public string MarkdownPath;

    private string _htmlPath;

    private bool _useContentScanner;
    private bool _saveOutputToHtml;

    // Use this for initialization
    private void Awake()
    {
        UIManager.Instance.SetMarkdownText("");
        UIManager.Instance.SetHtmlText("");
        Application.targetFrameRate = 30;
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
        UIManager.Instance.ShowLoadingScreen();

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                UIManager.Instance.SetStatusText(www.error);
            }
            else
            {
                ConvertMarkdownAndFillTextFields(www.downloadHandler.text);
            }
        }

        UIManager.Instance.HideLoadingScreen();
    }

    private void ConvertMarkdownAndFillTextFields(string markdown)
    {
        if (markdown.Length == 0)
        {
            return;
        }

        Markdown = markdown;
        ConverterOptions options = GetConverterOptions();
        HTML = Converter.ConvertMarkdownStringToHtml(Markdown, options);
        UIManager.Instance.SetImageLinkButtonVisible(true);
        UIManager.Instance.SetMarkdownGroupVisible(true);
        UIManager.Instance.SetHtmlGroupVisible(true);
        UIManager.Instance.SetCopyHtmlTopButtonVisible(true);
        UIManager.Instance.SetPasteHtmlTopButtonVisible(true);
        UIManager.Instance.SetHemingwayButtonVisible(true);

        UIManager.Instance.LoadMarkdownPage(0);
        UIManager.Instance.LoadHtmlPage(0);
    }

    public void DoConversion()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UIManager.Instance.WebGLUploadCanvas.SetActive(true);
        UploadFile(gameObject.name, "OnFileUpload", ".md, .markdown, .txt, .mdown, .mkdn, .mkd, .mdwn, .mdtext, .mdtxt, .text, .rmd", false);
#else
        string path = FileBrowser.OpenSingleFile("Open Markdown File", "",
                        new ExtensionFilter[] { new ExtensionFilter("Markdown Files", new string[] { "md", "markdown", "mdown","mkdn",
                        "mkd","mdwn","mdtxt","mdtext","text","txt","rmd"}) });

        if (File.Exists(path))
        {
            UIManager.Instance.ShowLoadingScreen();
            Convert(path);
        }

#endif
    }

    private void Convert(string path)
    {
        ConverterOptions options = GetConverterOptions();

        if (path != null)
        {
            if (File.Exists(path))
            {
                MarkdownPath = path;
                _htmlPath = null;

                using (StreamReader sr = new StreamReader(path))
                {
                    Markdown = sr.ReadToEnd().Replace("\t", "  ");
                    ConvertMarkdownAndFillTextFields(Markdown);

                    if (_useContentScanner)
                    {
                        print(ContentScanner.ParseScanrResults(ContentScanner.ScanMarkdown(Markdown, MarkdownPath)));
                    }

                    if (_saveOutputToHtml)
                    {
                        _htmlPath = DragonUtil.GetFullPathWithoutExtension(path) + ".html";
                        Converter.ConvertMarkdownFileToHtmlFile(path, _htmlPath, options);
                    }

                    UIManager.Instance.HideLoadingScreen();
                    UIManager.Instance.SetStatusText("Converted markdown! Copy HTML on right side or start Image Linker (experimental).");
                }
            }
        }
        else
        {
            UIManager.Instance.HideLoadingScreen();
            UIManager.Instance.SetStatusText("No valid markdown chosen!");
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

    public void CopyMarkdownToClipboard()
    {
        Markdown.CopyToClipboard();
        UIManager.Instance.SetStatusText("Copied markdown to clipboard!");
    }

    public void CopyHtmlToClipboard()
    {
        HTML.CopyToClipboard();
        UIManager.Instance.SetStatusText("Copied HTML to clipboard!");
    }
}