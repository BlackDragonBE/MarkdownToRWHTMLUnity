using UnityEngine;
using UnityEditor;

public class SetWebGLSettings : ScriptableObject
{
    [MenuItem("Tools/WEBGL/Set Memory Size/1024")]
    private static void DoIt()
    {
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.memorySize = 512; // tweak this value for your project

        //Debug.Log(PlayerSettings.WebGL.emscriptenArgs);
    }
}