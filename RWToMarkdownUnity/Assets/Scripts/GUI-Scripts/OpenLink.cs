using UnityEngine;
using System.Collections;

public class OpenLink : MonoBehaviour
{
    public string Url;

    public void OpenWebLink()
    {
        Application.OpenURL(Url);
    }
}