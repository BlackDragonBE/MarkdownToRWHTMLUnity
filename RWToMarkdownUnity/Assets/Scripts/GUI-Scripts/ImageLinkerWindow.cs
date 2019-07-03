using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using DragonMarkdown.DragonConverter;
using System;
using System.IO;
using UnityEngine.Networking;

public class ImageLinkerWindow : MonoBehaviour
{
    public ConversionMaster ConvMaster;

    public TextMeshProUGUI ConsoleText;
    public GameObject LinkButton;
    public GameObject CancelButton;

    public Toggle OverrideDateCheckbox;
    public TMP_InputField MonthInput;
    public TMP_InputField YearInput;

    private CustomCertificateHandler certHandler;

    private Dictionary<string, bool> _linksDictionary = new Dictionary<string, bool>();
    private int _amountOfLinksChecked = 0;

    // Use this for initialization
    private void Awake()
    {
        certHandler = new CustomCertificateHandler();

        YearInput.text = DateTime.Now.Year.ToString();
        MonthInput.text = DateTime.Now.Month.ToString("00");
    }

    public void StartLinking()
    {
        StartCoroutine(LinkImages());
    }

    public IEnumerator LinkImages()
    {
        CancelButton.SetActive(false);

        if (ConvMaster.Markdown.Length == 0)
        {
            WriteToLinkConsole("No markdown loaded! Aborting.");
            CancelButton.SetActive(true);
            yield break;
        }

        var links = Converter.FindAllLocalImageLinksInHtml(ConvMaster.HTML);

        if (links.Count == 0)
        {
            WriteToLinkConsole("No images found to upload! Aborting.");
            CancelButton.SetActive(true);
            yield break;
        }

        string imageUrlPrefix = GetImageUrlPrefix();

        List<string> localImagePaths = new List<string>();
        List<string> fileNames = new List<string>();
        List<string> imageUrls = new List<string>();
        int failedLinks = 0;

        foreach (ImageLinkData link in links)
        {
            localImagePaths.Add(link.LocalImagePath);
            fileNames.Add(Path.GetFileName(link.LocalImagePath));
        }

        ConsoleText.text = "";
        WriteToLinkConsole(localImagePaths.Count + " image paths found.");
        WriteToLinkConsole("Starting linking process...");
        WriteToLinkConsole("Using URL prefix: " + imageUrlPrefix);

        UIManager.Instance.SetProgress(0);
        UIManager.Instance.SetProgressMaxValue(fileNames.Count);
        UIManager.Instance.SetStatusText("Checking image URLs...");

        _linksDictionary.Clear();
        _amountOfLinksChecked = 0;

        for (int i = 0; i < fileNames.Count; i++)
        {
            string potentialUrl = imageUrlPrefix + fileNames[i];
            _linksDictionary.Add(potentialUrl, false);

#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(UrlExistsWithPHP(potentialUrl));
#else
            StartCoroutine(UrlExists(potentialUrl));
#endif
        }

        yield return new WaitUntil(() => _amountOfLinksChecked == fileNames.Count);

        foreach (var link in _linksDictionary)
        {
            if (link.Value) // Image found
            {
                WriteToLinkConsole("[OK] " + link.Key);
                imageUrls.Add(link.Key);
            }
            else
            {
                WriteToLinkConsole("<color=#FF0000>[NOT FOUND!] " + link.Key + "</color>");
                failedLinks++;
                imageUrls.Add(null);
            }
        }

        MarkdownAndHtml markdownAndHtml = Converter.ReplaceLocalImageLinksWithUrls(ConvMaster.MarkdownPath, ConvMaster.Markdown,
null, ConvMaster.HTML, true, localImagePaths, imageUrls);

        ConvMaster.Markdown = markdownAndHtml.Markdown;
        ConvMaster.HTML = markdownAndHtml.Html;

        UIManager.Instance.LoadMarkdownPage(0);
        UIManager.Instance.LoadHtmlPage(0);

        if (failedLinks == 0)
        {
            UIManager.Instance.SetStatusText("Succesfully linked all " + fileNames.Count + " images!");
        }
        else
        {
            UIManager.Instance.SetStatusText("Failed to link " + failedLinks + " images. See text output.");
        }

        CancelButton.SetActive(true);
        WriteToLinkConsole("Finished operation! Both the markdown and HTML have been adjusted. Please don't run linker again until you've opened another markdown file.");
    }

    private string GetImageUrlPrefix()
    {
        if (!OverrideDateCheckbox.isOn)
        {
            return $"https://koenig-media.raywenderlich.com/uploads/{DateTime.Now.Year}/{DateTime.Now.Month:00}/";
        }
        else
        {
            return $"https://koenig-media.raywenderlich.com/uploads/{YearInput.text:yyyy}/{MonthInput.text:00}/";
        }
    }

    private void WriteToLinkConsole(string text)
    {
        ConsoleText.text += text + "\n";
    }

    public IEnumerator UrlExists(string url)
    {
        UnityWebRequest www = UnityWebRequest.Head(url);
        www.certificateHandler = certHandler;
        www.timeout = 30;

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError || www.responseCode > 400)
        {
            //WriteToLinkConsole("Error: " + www.error);
            _linksDictionary[url] = false;
        }
        else
        {
            _linksDictionary[url] = true;
        }

        www.Dispose();

        UIManager.Instance.IncreaseProgress();
        _amountOfLinksChecked++;
    }

    // todo: check all urls at the same time and use a dictionary to store boolean
    /// <summary>
    /// Check if file exists online using PHP. If the file exists, _lastFileFoundOnServer is set to true.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public IEnumerator UrlExistsWithPHP(string url)
    {
        string phpUrl = "https://blackdragonsoftware.be/PHP/FileExists.php?url=" + url;

        UnityWebRequest www = UnityWebRequest.Get(phpUrl);
        www.certificateHandler = certHandler;
        www.timeout = 30;

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError || www.responseCode > 400)
        {
            WriteToLinkConsole("PHP Error: " + www.error);
        }
        else
        {
            _linksDictionary[url] = www.downloadHandler.text == "true";
        }

        www.Dispose();

        UIManager.Instance.IncreaseProgress();
        _amountOfLinksChecked++;
    }
}