using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class ConverterChooser : MonoBehaviour
{
    public void OpenMarkdownToHtml()
    {
        SceneManager.LoadScene("MarkdownToHtml");
    }

    public void OpenHtmlToMarkdown()
    {
        SceneManager.LoadScene("HtmlToMarkdown");
    }
}