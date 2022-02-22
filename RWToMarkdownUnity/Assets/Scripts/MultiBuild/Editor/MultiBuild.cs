using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

/*
This script will automatically make a Windows, Mac and Linux build in any folder you choose.

    Usage:
    - Change paramaters depending on your game / preferences
    - In Unity Editor: Build/Multi Build
    - Builds will be made and zipped by 7zip if chosen to do so and 7zip is installed on the system
*/

public class MultiBuild
{
    [MenuItem("Build/Multi Build")]
    public static void DoMultiBuild()
    {
        // Get filename.
        string path = EditorUtility.SaveFolderPanel("Choose Builds Folder", "", "");

        //PARAMETERS START
        string gameName = Application.productName; //Name of your game, product name by default

        //Scene paths
        string[] scenes = new string[EditorBuildSettings.scenes.Length];

        for (var i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }

        bool zipFolders = true; //Use 7zip to compress the created folders
        //PARAMETERS END

        // Build Win
        BuildPipeline.BuildPlayer(scenes, path + "/Win/" + gameName + ".exe", BuildTarget.StandaloneWindows64, BuildOptions.None);

        // Build Mac
        BuildPipeline.BuildPlayer(scenes, path + "/Mac/" + gameName + ".app", BuildTarget.StandaloneOSX, BuildOptions.None);

        // Build Linux
        BuildPipeline.BuildPlayer(scenes, path + "/Linux/" + gameName + ".x86", BuildTarget.StandaloneLinux64, BuildOptions.None);

        // 7zip
        if (zipFolders && File.Exists(@"C:\Program Files\7-Zip\7z.exe"))
        {
            ZipFolder(path + "/Win/", gameName + " Win.zip");
            ZipFolder(path + "/Mac/", gameName + " Mac.zip");
            ZipFolder(path + "/Linux/", gameName + " Linux.zip");
        }
    }

    private static void ZipFolder(string folderPath, string zipName)
    {
        Process proc = new Process();
        proc.StartInfo.FileName = @"C:\Program Files\7-Zip\7z.exe";
        proc.StartInfo.Arguments = "a -tzip " + '"' + zipName + '"' + " " + '"' + folderPath + '"';
        proc.Start();
    }
}