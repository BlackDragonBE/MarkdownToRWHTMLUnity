using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System;

public class HtmlToMarkdownUIManager : MonoBehaviour
{
    public static HtmlToMarkdownUIManager Instance;

    public HtmlToMarkdownMaster ConvMaster;

    public TMP_InputField MarkdownText;
    public TMP_InputField HtmlText;
    public Slider StatusSlider;
    public TextMeshProUGUI StatusText;

    public TextMeshProUGUI MarkdownPage;
    public TextMeshProUGUI HtmlPage;

    public GameObject LoadingCanvas;
    public GameObject WebGLUploadCanvas;

    public GameObject MarkdownGroup;
    public GameObject HtmlGroup;

    public GameObject ImageDownloadButton;
    public GameObject CopyMarkdownTopButton;

    public GameObject ImageDownloadWindow;

    public int MaximumCharactersPerPage = 1000;

    private int _markdownPage;
    private int _htmlPage;

    public void OnEnable()
    {
        Instance = this;
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ImageDownloadButton.GetComponent<Button>().interactable = false;
        ImageDownloadButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "NOT AVAILABLE IN WEBGL\nOpen Image Downloader...";
#endif
        SetImageLinkButtonVisible(false);
        SetMarkdownGroupVisible(false);
        SetHtmlGroupVisible(false);
        SetCopyMarkdownTopButtonVisible(false);
    }

    public void SetImageLinkButtonVisible(bool visible)
    {
        ImageDownloadButton.SetActive(visible);
    }

    public void SetMarkdownGroupVisible(bool visible)
    {
        MarkdownGroup.SetActive(visible);
    }

    public void SetHtmlGroupVisible(bool visible)
    {
        HtmlGroup.SetActive(visible);
    }

    public void SetCopyMarkdownTopButtonVisible(bool visible)
    {
        CopyMarkdownTopButton.SetActive(visible);
    }

    public void SetMarkdownText(string text)
    {
        MarkdownText.text = text;
    }

    public void SetHtmlText(string text)
    {
        HtmlText.text = text;
    }

    public void SetStatusText(string text)
    {
        StatusText.text = text;
    }

    public void SetProgressMaxValue(int max)
    {
        StatusSlider.maxValue = max;
    }

    public void SetProgress(int progress)
    {
        StatusSlider.value = progress;
    }

    public void IncreaseProgress()
    {
        StatusSlider.value++;
    }

    public void ShowLoadingScreen()
    {
        LoadingCanvas.SetActive(true);
    }

    public void HideLoadingScreen()
    {
        LoadingCanvas.SetActive(false);
    }

    public void ShowDownloadWindow()
    {
        ImageDownloadWindow.SetActive(true);
        HtmlGroup.SetActive(false);
    }

    public void HideDownloadWindow()
    {
        ImageDownloadWindow.SetActive(false);
        HtmlGroup.SetActive(true);
    }

    public void ShowNextMarkdownPage()
    {
        if (_markdownPage > ConvMaster.Markdown.Length / MaximumCharactersPerPage)
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

    public void LoadMarkdownPage(int page)
    {
        int firstCharIndex = page * MaximumCharactersPerPage;
        int lastCharIndex = firstCharIndex + MaximumCharactersPerPage;

        bool firstCharOK = ConvMaster.Markdown.Length > firstCharIndex;
        bool lastCharOK = ConvMaster.Markdown.Length > lastCharIndex;

        if (!firstCharOK)
        {
            return;
        }

        _markdownPage = page;
        MarkdownPage.text = "Page " + (page + 1) + " / " + (ConvMaster.Markdown.Length / MaximumCharactersPerPage + 1);

        if (!lastCharOK) // Not enough characters left to show just a part, show all of it
        {
            MarkdownText.text = ConvMaster.Markdown.Substring(firstCharIndex);
        }
        else // Everything ok, show subset
        {
            MarkdownText.text = ConvMaster.Markdown.Substring(firstCharIndex, MaximumCharactersPerPage);
        }
    }

    public void ShowNextHtmlPage()
    {
        if (_htmlPage > ConvMaster.OriginalHTML.Length / MaximumCharactersPerPage)
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

    public void LoadHtmlPage(int page)
    {
        int firstCharIndex = page * MaximumCharactersPerPage;
        int lastCharIndex = firstCharIndex + MaximumCharactersPerPage;

        bool firstCharOK = ConvMaster.OriginalHTML.Length > firstCharIndex;
        bool lastCharOK = ConvMaster.OriginalHTML.Length > lastCharIndex;

        if (!firstCharOK)
        {
            return;
        }

        _htmlPage = page;
        HtmlPage.text = "Page " + (page + 1) + " / " + (ConvMaster.OriginalHTML.Length / MaximumCharactersPerPage + 1);

        if (!lastCharOK) // Not enough characters left to show just a part, show all of it
        {
            HtmlText.text = ConvMaster.OriginalHTML.Substring(firstCharIndex);
        }
        else // Everything ok, show subset
        {
            HtmlText.text = ConvMaster.OriginalHTML.Substring(firstCharIndex, MaximumCharactersPerPage);
        }
    }
}