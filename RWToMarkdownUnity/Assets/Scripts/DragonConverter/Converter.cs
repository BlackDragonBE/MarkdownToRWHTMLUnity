using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using DragonMarkdown.Utility;
using HtmlAgilityPack;
using Markdig;

namespace DragonMarkdown.DragonConverter
{
    public static class Converter
    {
        public static string ConvertMarkdownStringToHtml(string markdown, ConverterOptions options = null, string rootPath = null, bool prepareForPreview = false)
        {
            if (options == null)
            {
                options = new ConverterOptions();
            }

            //MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseEmphasisExtras().UseCustomContainers().Build();

            string output = Markdown.ToHtml(markdown, pipeline);

            // HTML readability improvements & RW specific changes

            // Code
            if (!prepareForPreview)
            {
                output = new StringBuilder(output)
                .Replace("<pre><code class=", "\r\n<pre lang=")
                .Replace("lang-", "")
                .Replace("language-", "")
                .Replace("</code></pre>", "</pre>\r\n")
                .ToString();
            }

            // Add attributes
            AddClassToImages(ref output, options.FirstImageIsAlignedRight, rootPath);

            AddExtraAttributesToLinks(ref output);

            output = new StringBuilder(output)
            .Replace("\r\n", "|||")
            .Replace("|||", "\n")
            .Replace("<p>", "\n")
            .Replace("<br>", "\n")
            .Replace("</p>", "")
            .Replace("<h1", "\n<h1")
            .Replace("<h2", "\n<h2")
            .Replace("<h3", "\n<h3")
            .Replace("<h4", "\n<h4")
            .Replace("<em>", "<i>")
            .Replace("</em>", "</i>")
            .Replace("<strong>", "<em>")
            .Replace("</strong>", "</em>")
            .Replace("</blockquote>", "</div>")
            .Replace("<blockquote>\n", "\n<blockquote>")
            .Replace("<blockquote>\n<em>Note", "<div class=\"note\">\n<em>Note")
            .Replace("<blockquote>", "<div>")
            .ToString();

            // Spoiler
            ConvertSpoilers(ref output);

            if (options.ReplaceImageWithAltWithCaption && !prepareForPreview)
            {
                ConvertImagesWithAltToCaptions(ref output);
            }

            // Final cleanup
            output = output.Replace("<div></div>", "");
            if (!prepareForPreview)
            {
                output = WebUtility.HtmlDecode(output);
            }

            output = output.Trim();
            return output;
        }

        public static void ConvertMarkdownFileToHtmlFile(string markdownFilePath, string htmlFilePath,
            ConverterOptions options = null)
        {
            using (StreamReader sr = new StreamReader(markdownFilePath))
            {
                string html = ConvertMarkdownStringToHtml(sr.ReadToEnd(), options, Path.GetDirectoryName(markdownFilePath));
                using (StreamWriter sw = new StreamWriter(htmlFilePath))
                {
                    sw.Write(html);
                    sw.Flush();
                }
            }
        }

        private static void AddClassToImages(ref string html, bool firstImageRightAligned, string rootPath = null)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection imgNodes = doc.DocumentNode.SelectNodes("//img[@src]");
            string size = "full";

            if (imgNodes == null || imgNodes.Count == 0)
            {
                return;
            }

            for (var i = 0; i < imgNodes.Count; i++)
            {
                HtmlNode node = imgNodes[i];

                // If root path is known, check if images are too big for full size class
                if (rootPath != null && File.Exists(DragonUtil.GetFullFilePath(node.Attributes["src"].Value, rootPath)))
                {
                    var imageSize = ImageHelper.GetDimensions(DragonUtil.GetFullFilePath(node.Attributes["src"].Value, rootPath));

                    if (imageSize.x > 700)
                    {
                        size = "large";
                    }
                    else
                    {
                        size = "full";
                    }
                }

                if (i == 0 && firstImageRightAligned) // First image should be right aligned, it's the 250x250 image
                {
                    HtmlAttribute classAttribute = doc.CreateAttribute("class", "alignright size-" + size);
                    node.Attributes.Add(classAttribute);
                }
                else
                {
                    HtmlAttribute classAttribute = doc.CreateAttribute("class", "aligncenter size-" + size);
                    node.Attributes.Add(classAttribute);
                }
            }

            html = doc.DocumentNode.OuterHtml;
        }

        private static void AddExtraAttributesToLinks(ref string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection linkNodes = doc.DocumentNode.SelectNodes("//a");

            if (linkNodes == null || linkNodes.Count == 0)
            {
                return;
            }

            for (var i = 0; i < linkNodes.Count; i++)
            {
                HtmlNode node = linkNodes[i];

                HtmlAttribute relAttribute = doc.CreateAttribute("rel", "noopener");
                node.Attributes.Add(relAttribute);

                HtmlAttribute targetAttribute = doc.CreateAttribute("target", "_blank");
                node.Attributes.Add(targetAttribute);
            }

            html = doc.DocumentNode.OuterHtml;
        }

        private static void ConvertImagesWithAltToCaptions(ref string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection imgNodes = doc.DocumentNode.SelectNodes("//img[@src]");

            if (imgNodes == null || imgNodes.Count == 0)
            {
                return;
            }

            for (int i = 0; i < imgNodes.Count; i++)
            {
                HtmlNode imgNode = imgNodes[i];
                if (imgNode.Attributes["alt"] != null && imgNode.Attributes["alt"].Value != "")
                {
                    HtmlNode parent = imgNode.ParentNode;

                    HtmlDocument newDoc = new HtmlDocument();
                    HtmlNode newElement = newDoc.CreateElement("caption");
                    newElement.SetAttributeValue("align", imgNode.Attributes["class"].Value);
                    newElement.AppendChild(imgNode);
                    newElement.InnerHtml += imgNode.Attributes["alt"].Value;

                    parent.ReplaceChild(newElement, imgNode);

                    ReplaceOuterHtmlWithSquareBrackets(newElement);
                }
            }

            html = doc.DocumentNode.OuterHtml;
        }

        private static void ConvertSpoilers(ref string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection divNodes = doc.DocumentNode.SelectNodes("//div");

            if (divNodes == null || divNodes.Count == 0)
            {
                return;
            }

            for (int i = 0; i < divNodes.Count; i++)
            {
                if (divNodes[i].InnerHtml.StartsWith("\n<em>Spoiler:"))
                {
                    string spoilerTitle = divNodes[i].ChildNodes[1].InnerText.Split(':')[1].Trim();
                    divNodes[i].RemoveChild(divNodes[i].ChildNodes[1]);
                    divNodes[i].Attributes.Add("title", spoilerTitle);
                    divNodes[i].Name = "spoiler";

                    ReplaceOuterHtmlWithSquareBrackets(divNodes[i]);
                }
            }

            html = doc.DocumentNode.OuterHtml;
        }

        public static List<string> FindAllImageLinksInHtml(string html)
        {
            List<string> stringList = new List<string>();
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//img");
            if (htmlNodeCollection == null)
                return stringList;
            foreach (HtmlNode htmlNode in htmlNodeCollection)
                stringList.Add(htmlNode.GetAttributeValue("src", null));
            return stringList;
        }

        public static List<ImageLinkData> FindAllLocalImageLinksInHtml(string html)
        {
            List<ImageLinkData> links = new List<ImageLinkData>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection imgNodes = doc.DocumentNode.SelectNodes("//img");

            if (imgNodes == null)
            {
                return links;
            }

            for (int i = 0; i < imgNodes.Count; i++)
            {
                // Skip if web link
                if (imgNodes[i].GetAttributeValue("src", "").StartsWith("http") ||
                    imgNodes[i].GetAttributeValue("src", "").StartsWith("www"))
                {
                    continue;
                }

                string localPath = imgNodes[i].GetAttributeValue("src", null);

                ImageLinkData imageData = new ImageLinkData
                {
                    LocalImagePath = localPath,
                };

                links.Add(imageData);
            }

            return links;
        }

        public static MarkdownAndHtml ReplaceLocalImageLinksWithUrls(string markdownPath, string markdownText, string htmlPath, string htmlText, bool onlyUpdateHtml, List<string> localImagePaths, List<string> imageUrls)
        {
            markdownText = DragonUtil.BatchReplaceText(markdownText, localImagePaths, imageUrls);
            htmlText = DragonUtil.BatchReplaceText(htmlText, localImagePaths, imageUrls);
            //var htmlText = ConvertMarkdownStringToHtml(markdownText,);

            if (htmlPath != null)
            {
                DragonUtil.QuickWriteFile(htmlPath, htmlText);
                Console.WriteLine("Replaced HTML!");
            }

            if (!onlyUpdateHtml)
            {
                DragonUtil.QuickWriteFile(markdownPath, markdownText);
                Console.WriteLine("Replaced Markdown!");
            }

            return new MarkdownAndHtml { Markdown = markdownText, Html = htmlText };
        }

        private static void ReplaceOuterHtmlWithSquareBrackets(HtmlNode node)
        {
            string inner = node.InnerHtml;
            string newOuter = node.OuterHtml;

            newOuter = new StringBuilder(newOuter)
            .Replace(inner, "")
            .Replace("<", "[")
            .Replace(">", "]")
            .ToString();

            var newNode = HtmlNode.CreateNode(newOuter);
            newNode.InnerHtml = newNode.InnerHtml.Replace("][", "]" + inner.Trim() + "[");
            node.ParentNode.ReplaceChild(newNode, node);
        }
    }

    public struct ImageLinkData
    {
        public string LocalImagePath;
    }
}