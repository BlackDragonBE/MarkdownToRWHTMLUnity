using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Crosstales.FB;
using System.IO;
using System;
using System.Collections.Generic;
using DragonMarkdown.DragonConverter;
using DragonMarkdown.Utility;
using System.Text;
using System.Net;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using HtmlAgilityPack;

public class HtmlToMarkdownMaster : MonoBehaviour
{
    [HideInInspector]
    public string OriginalHTML;

    [HideInInspector]
    public string PreparedHTML;

    [HideInInspector]
    public string Markdown;

    [HideInInspector]
    public string HtmlFilePath;

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseConverter();
        }
    }

    public void CloseConverter()
    {
        SceneManager.LoadScene("ConverterChooser");
    }

    private IEnumerator OutputRoutine(string url)
    {
        //UIManager.Instance.ShowLoadingScreen();

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                // Show error status
            }
            else
            {
                ConvertHtmlToMarkdown(www.downloadHandler.text);
            }
        }

        //UIManager.Instance.HideLoadingScreen();
    }

    public void DoConversion()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HtmlToMarkdownUIManager.Instance.WebGLUploadCanvas.SetActive(true);
        UploadFile(gameObject.name, "OnFileUpload", ".html, .htm, .txt", false);
#else
        string path = FileBrowser.OpenSingleFile("Open HTML File", "",
                        new ExtensionFilter[] { new ExtensionFilter("HTML Files", new string[] { "html", "htm", "txt" }) });

        if (File.Exists(path))
        {
            HtmlFilePath = path;
            //UIManager.Instance.ShowLoadingScreen();
            ConvertHtmlToMarkdown(File.ReadAllText(path));
        }
#endif
    }

    private void ConvertHtmlToMarkdown(string html)
    {
        OriginalHTML = html;
        PreparedHTML = CleanHTML(html);
        Markdown = new Html2Markdown.Converter().Convert(PreparedHTML);
        Markdown = CleanMarkdown(Markdown);

        HtmlToMarkdownUIManager.Instance.SetImageLinkButtonVisible(true);
        HtmlToMarkdownUIManager.Instance.SetMarkdownGroupVisible(true);
        HtmlToMarkdownUIManager.Instance.SetHtmlGroupVisible(true);
        HtmlToMarkdownUIManager.Instance.SetCopyMarkdownTopButtonVisible(true);

        HtmlToMarkdownUIManager.Instance.LoadMarkdownPage(0);
        HtmlToMarkdownUIManager.Instance.LoadHtmlPage(0);
    }

    private string CleanHTML(string htmlText)
    {
        htmlText = new StringBuilder(htmlText)
        .Replace("<pre", "<code")
        .Replace("</pre", "</code")
        .Replace("<em", "<b")
        .Replace("</em", "</b")
        .Replace("’", "'")
        .Replace("[caption", "<caption")
        //.Replace("[/caption]", "")
        .Replace("[/caption]", "</caption>")
        .Replace("]<img", "><img")
        .ToString();

        if (htmlText.Contains("�"))
        {
            //ColoredConsole.WriteLineWithColor("- Broken symbol found, this file isn't encoded using UTF-8 and doesn't contain a BOM. Some characters may be incorrect.", ConsoleColor.Red);
            htmlText = htmlText.Replace("�", "'");
        }

        htmlText = ConvertCaptionsToMarkdown(htmlText);

        return htmlText;
    }

    private static string ConvertCaptionsToMarkdown(string htmlText)
    {
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(htmlText);
        HtmlNodeCollection captionNodes = doc.DocumentNode.SelectNodes("//caption");

        if (captionNodes != null)
        {
            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//caption"))
            {
                string captionText = node.InnerText;
                string imgAlt = node.FirstChild.Attributes["alt"].Value;

                node.FirstChild.SetAttributeValue("alt", imgAlt + "|" + captionText);

                node.InnerHtml = node.InnerHtml.Replace("\">" + captionText, ">");

                node.RemoveButKeepChildren();
            }
        }

        htmlText = doc.DocumentNode.OuterHtml;
        return htmlText;
    }

    private string CleanMarkdown(string markdown)
    {
        markdown = new StringBuilder(markdown)
        .Replace("<div class=\"note\">\r\n", ">")
        .Replace("\r\n</div>", "")
        .Replace("<code lang=\"csharp\">", "```csharp")
        .Replace("<code lang=\"cs\">", "```cs\r\n")
        .Replace("</code>", "```\r\n")
        .Replace("<del>", "~~")
        .Replace("</del>", "~~")
        .Replace("</del>", "~~")

        .ToString();

        markdown = FixBrokenCode(markdown);
        markdown = markdown.Replace("```\r\n\r\n", "```\r\n");
        return markdown;
    }

    private string FixBrokenCode(string markdown)
    {
        string str1 = markdown;
        int num;
        do
        {
            num = 0;
            List<string> originals = new List<string>();
            List<string> replacements = new List<string>();
            using (StringReader stringReader = new StringReader(str1))
            {
                string str2;
                while ((str2 = stringReader.ReadLine()) != null)
                {
                    if (str2.Contains("```") && !str2.StartsWith("```"))
                    {
                        ++num;
                        originals.Add(str2.Trim());
                        replacements.Add("```\r\n");
                        string[] strArray = str2.Split(new char[] { '>' }, StringSplitOptions.None);
                        for (int index = 0; index < strArray.Length - 1; ++index)
                        {
                            string wrongCasedClass = strArray[index].Replace("<", "").Replace(">", "").Replace("/", "");
                            originals.Add("<" + wrongCasedClass + ">()");
                            string correctClassCase = FindCorrectClassCase(wrongCasedClass);
                            replacements.Add("<" + correctClassCase + ">()");
                        }
                    }
                }
            }
            str1 = DragonUtil.BatchReplaceText(str1, originals, replacements);
        }
        while (num > 0);
        return str1;
    }

    private string FindCorrectClassCase(string wrongCasedClass)
    {
        string str1 = wrongCasedClass;
        string str2 = "<" + wrongCasedClass + ">()";
        int length = str2.Length;
        int startIndex = PreparedHTML.IndexOf(str2, StringComparison.CurrentCultureIgnoreCase);
        if (startIndex != -1)
            str1 = PreparedHTML.Substring(startIndex, length).Replace("<", "").Replace(">()", "");
        return str1;
    }

    public void CopyMarkdownToClipboard()
    {
        Markdown.CopyToClipboard();
        // UIManager.Instance.SetStatusText("Copied markdown to clipboard!");
    }
}