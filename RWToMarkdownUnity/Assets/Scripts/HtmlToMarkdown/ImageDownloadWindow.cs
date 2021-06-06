using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using System.IO;
using DragonMarkdown.DragonConverter;
using System;
using DragonMarkdown.Utility;
using UnityEngine.Networking;
using System.Runtime.InteropServices;

public class ImageDownloadWindow : MonoBehaviour
{
    public HtmlToMarkdownMaster ConvMaster;

    public TextMeshProUGUI ConsoleText;
    public GameObject DownloadButton;
    public GameObject CancelButton;

    private CustomCertificateHandler certHandler;

    private Dictionary<string, bool> _downloadDictionary = new Dictionary<string, bool>();
    private int _amountOfImagesDownloaded = 0;

    private void Awake()
    {
        certHandler = new CustomCertificateHandler();
    }

    public void DownloadImages()
    {
        StartCoroutine(DownloadImagesRoutine());
    }

    public IEnumerator DownloadImagesRoutine()
    {
        CancelButton.SetActive(false);
        DownloadButton.SetActive(false);

        string imageDirectory = Path.GetDirectoryName(ConvMaster.HtmlFilePath) + "/images";
        Directory.CreateDirectory(imageDirectory);
        List<string> imageUrls = Converter.FindAllImageLinksInHtml(ConvMaster.OriginalHTML);
        List<string> localImagePaths = new List<string>();

        _downloadDictionary.Clear();
        ConsoleText.text = "";
        HtmlToMarkdownUIManager.Instance.SetProgressMaxValue(imageUrls.Count);
        HtmlToMarkdownUIManager.Instance.SetProgress(0);

        foreach (string fileUrl in imageUrls)
        {
            _downloadDictionary.Add(fileUrl, false);
            string fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
            localImagePaths.Add("images/" + fileName);

            //DragonUtil.DownloadFile(str, imageDirectory + "/" + fileName, null, 0, null);
            StartCoroutine(DownloadFile(fileUrl, imageDirectory + "/" + fileName));
        }

        yield return new WaitUntil(() => _amountOfImagesDownloaded == imageUrls.Count);

        int failedDownloads = 0;

        foreach (var link in _downloadDictionary)
        {
            if (link.Value) // Image found
            {
                WriteToLinkConsole("[OK] " + link.Key);
            }
            else
            {
                WriteToLinkConsole("<color=#FF0000>[DOWNLOAD FAILED!] " + link.Key + "</color>");
                failedDownloads++;
            }
        }

        ConvMaster.OriginalHTML = DragonUtil.BatchReplaceText(ConvMaster.OriginalHTML, imageUrls, localImagePaths);
        ConvMaster.Markdown = DragonUtil.BatchReplaceText(ConvMaster.Markdown, imageUrls, localImagePaths);

        CancelButton.SetActive(true);
        HtmlToMarkdownUIManager.Instance.LoadHtmlPage(0);
        HtmlToMarkdownUIManager.Instance.LoadMarkdownPage(0);
        //Console.WriteLine("Downloads complete!");
    }

    private IEnumerator DownloadFile(string fileUrl, string savePath)
    {
        var uwr = new UnityWebRequest(fileUrl);
        uwr.method = UnityWebRequest.kHttpVerbGET;
        var dh = new DownloadHandlerFile(savePath);

        dh.removeFileOnAbort = true;
        uwr.downloadHandler = dh;

        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
        {
            WriteToLinkConsole("ERROR " + fileUrl + " :" + uwr.error);
            _downloadDictionary[fileUrl] = false;
        }
        else
        {
            //Debug.Log("Download saved to: " + savePath.Replace("/", "\\") + "\r\n" + uwr.error);
            _downloadDictionary[fileUrl] = true;
        }

        HtmlToMarkdownUIManager.Instance.IncreaseProgress();
        _amountOfImagesDownloaded++;
        HtmlToMarkdownUIManager.Instance.SetStatusText("Images downloaded: " + _amountOfImagesDownloaded + " / " + _downloadDictionary.Count);
    }

    private void WriteToLinkConsole(string text)
    {
        ConsoleText.text += text + "\n";
    }
}