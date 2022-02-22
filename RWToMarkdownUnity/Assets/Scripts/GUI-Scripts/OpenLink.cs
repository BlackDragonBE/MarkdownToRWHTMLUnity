using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class OpenLink : MonoBehaviour
{
    public string Url;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void openWindow(string url);
#endif

#if UNITY_WEBGL && !UNITY_EDITOR

    private void OpenLinkJS()
    {
         openWindow(Url);
    }
#endif

    public void OpenWebLink()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        openWindow(Url);
#else
        Application.OpenURL(Url);
#endif
    }
}