using UnityEngine;
using System.Collections;
using TMPro;

public class MarkdownAnalysisCanvas : MonoBehaviour
{
    public TextMeshProUGUI AnalysisConsoleText;

    public void SetText(string text)
    {
        //AnalysisConsoleText.text = text;
        AnalysisConsoleText.SetText(text);
    }
}