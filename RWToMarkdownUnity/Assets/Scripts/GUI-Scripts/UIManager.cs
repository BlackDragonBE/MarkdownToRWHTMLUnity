using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public ConversionMaster ConvMaster;

    public TMP_InputField MarkdownText;
    public TMP_InputField HtmlText;
    public Slider StatusSlider;
    public TextMeshProUGUI StatusText;

    public TextMeshProUGUI MarkdownPage;
    public TextMeshProUGUI HtmlPage;

    public GameObject LoadingCanvas;
    public GameObject MarkdownToHtmlCanvas;
    public GameObject AnalysisCanvas;
    public GameObject WebGLUploadCanvas;

    public GameObject ConverterOptionsWindow;

    public GameObject MarkdownGroup;
    public GameObject HtmlGroup;

    public GameObject ImageLinkerButton;
    public GameObject CopyHtmlTopButton;
    public GameObject PasteHtmlTopButton;
    public GameObject HemingwayButton;
    public GameObject AnalysisButton;

    public Toggle ToggleFirstImageAlignedRight;
    public Toggle ToggleBorderedImages;

    public int MaximumCharactersPerPage = 1000;

    private int _markdownPage;
    private int _htmlPage;

    public void OnEnable()
    {
        Instance = this;
    }

    private void Start()
    {
        HideAllExceptFirstStep();
    }

    private void HideAllExceptFirstStep()
    {
        SetImageLinkButtonVisible(false);
        SetMarkdownGroupVisible(false);
        SetHtmlGroupVisible(false);
        SetCopyHtmlTopButtonVisible(false);
        SetPasteHtmlTopButtonVisible(false);
        SetHemingwayButtonVisible(false);
        SetAnalysisButtonVisible(false);
    }

    public void SetImageLinkButtonVisible(bool visible)
    {
        ImageLinkerButton.SetActive(visible);
    }

    public void SetMarkdownGroupVisible(bool visible)
    {
        MarkdownGroup.SetActive(visible);
    }

    public void SetHtmlGroupVisible(bool visible)
    {
        HtmlGroup.SetActive(visible);
    }

    public void SetCopyHtmlTopButtonVisible(bool visible)
    {
        CopyHtmlTopButton.SetActive(visible);
    }

    public void SetPasteHtmlTopButtonVisible(bool visible)
    {
        PasteHtmlTopButton.SetActive(visible);
    }

    public void SetHemingwayButtonVisible(bool visible)
    {
        HemingwayButton.SetActive(visible);
    }

    public void SetAnalysisButtonVisible(bool visible)
    {
        AnalysisButton.SetActive(visible);
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
        if (_htmlPage > ConvMaster.HTML.Length / MaximumCharactersPerPage)
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

        bool firstCharOK = ConvMaster.HTML.Length > firstCharIndex;
        bool lastCharOK = ConvMaster.HTML.Length > lastCharIndex;

        if (!firstCharOK)
        {
            return;
        }

        _htmlPage = page;
        HtmlPage.text = "Page " + (page + 1) + " / " + (ConvMaster.HTML.Length / MaximumCharactersPerPage + 1);

        if (!lastCharOK) // Not enough characters left to show just a part, show all of it
        {
            HtmlText.text = ConvMaster.HTML.Substring(firstCharIndex);
        }
        else // Everything ok, show subset
        {
            HtmlText.text = ConvMaster.HTML.Substring(firstCharIndex, MaximumCharactersPerPage);
        }
    }

    public void ShowMarkdownToHtmlCanvas()
    {
        MarkdownToHtmlCanvas.SetActive(true);
    }

    public void HideMarkdownToHtmlCanvas()
    {
        MarkdownToHtmlCanvas.SetActive(false);
    }

    public void ShowAnalysisCanvas(string text)
    {
        AnalysisCanvas.SetActive(true);
        AnalysisCanvas.GetComponent<MarkdownAnalysisCanvas>().SetText(text);
    }

    public void HideAnalysisCanvas()
    {
        AnalysisCanvas.SetActive(false);
        AnalysisCanvas.GetComponent<MarkdownAnalysisCanvas>().SetText("");
    }

    public void ShowConverterOptionsWindow()
    {
        HideAllExceptFirstStep();
        ConverterOptionsWindow.SetActive(true);
    }

    public void HideConverterOptionsWindow()
    {
        ConverterOptionsWindow.SetActive(false);
    }
}