﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DragonMarkdown.Utility
{
    public static class DragonUtil
    {
        public static class CurrentOperatingSystem
        {
            public static bool IsWindows() =>
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            public static bool IsMacOS() =>
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            public static bool IsLinux() =>
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        public static string CurrentDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public static bool IsRunningPortable()
        {
            return Assembly.GetEntryAssembly().Location.EndsWith(".dll") && !File.Exists(DragonUtil.CurrentDirectory + "/System.Private.CoreLib.dll");
        }

        public static string RemoveAllQuotes(string path)
        {
            return path.Replace("\"", "").Replace("'", "").Trim();
        }

        public static string SurroundWithQuotes(string str)
        {
            return '"' + str + '"';
        }

        public static string SurroundWithSingleQuotes(string str)
        {
            return "'" + str + "'";
        }

        public static string BatchReplaceText(string text, List<string> originals, List<string> replacements)
        {
            string newText = text;

            for (var i = 0; i < replacements.Count; i++)
            {
                string original = originals[i];
                string replacement = replacements[i];
                newText = newText.Replace(original, replacement);
            }

            return newText;
        }

        public static bool QuickWriteFile(string path, string content)
        {
            if (!CheckFolderWritePermission(Path.GetDirectoryName(path))) return false;

            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(content);
                sw.Flush();
                sw.Close();
                return true;
            }
        }

        public static string QuickReadFile(string path)
        {
            string output = null;

            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    output = sr.ReadToEnd();
                }
            }

            return output;
        }

        public static string GetFullPathWithoutExtension(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        }

        public static void OpenFileInDefaultApplication(string path)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    //Process.Start(new ProcessStartInfo("cmd", "/c " + SurroundWithQuotes("start " + SurroundWithSingleQuotes(path))));
                    ProcessStartInfo p = new ProcessStartInfo(path) { UseShellExecute = true };
                    Process.Start(p);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", SurroundWithSingleQuotes(path));
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", SurroundWithSingleQuotes(path));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Returns true if the app doesn't poop itself while trying to write a file to the folder.
        /// </summary>
        /// <returns></returns>
        public static bool CheckFolderWritePermission(string folderPath)
        {
            try
            {
                string filePath = folderPath + "/test.txt";

                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    sw.WriteLine("test");
                    sw.Flush();
                    sw.Close();
                }

                File.Delete(filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetPasswordChars(int length, char character)
        {
            string chars = "";

            for (int index = 0; index < length; index++)
            {
                chars += character;
            }

            return chars;
        }

        public static async Task DownloadFile(string url, string fileSavePath, string userAgent = null, int fileSizeInKilobytes = 0, IProgress<SimpleTaskProgress> taskProgress = null)
        {
            using (var client = new HttpClient())
            {
                if (userAgent != null)
                {
                    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                }

                using (HttpResponseMessage response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result)
                {
                    response.EnsureSuccessStatusCode();

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(fileSavePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var totalRead = 0L;
                        var totalReads = 0L;
                        var buffer = new byte[8192];
                        var isMoreToRead = true;

                        do
                        {
                            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);

                                totalRead += read;
                                totalReads += 1;

                                if (totalReads % 256 != 0) continue;

                                taskProgress?.Report(new SimpleTaskProgress { CurrentProgress = (int)(totalRead / 1024), TotalProgress = fileSizeInKilobytes });

                                if (fileSizeInKilobytes > 0)
                                {
                                    long progress = totalRead / fileSizeInKilobytes * 100;

                                    Console.WriteLine(String.Format("Download progress: {2:n0} %  {0:n0}/{1:n0}", totalRead / 1024,
                                        fileSizeInKilobytes, progress));
                                }
                                else
                                {
                                    Console.WriteLine(String.Format("Total kiloBytes downloaded so far: {0:n0}", totalRead / 1024));
                                }
                            }
                        }
                        while (isMoreToRead);

                        taskProgress?.Report(new SimpleTaskProgress { CurrentProgress = (int)(totalRead / 1024), TotalProgress = fileSizeInKilobytes });
                    }
                }
            }
        }

        public static async Task TryToMakeExecutable(string filePath)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo
                {
                    UseShellExecute = false,
                };

                if (CurrentOperatingSystem.IsLinux())
                {
                    info.FileName = "gksudo";
                    info.Arguments = "chmod +x " + SurroundWithQuotes(filePath);
                }
                else
                {
                    string scriptPath = Directory.GetParent(filePath).FullName;
                    info.FileName = "osascript";
                    info.Arguments = SurroundWithQuotes(scriptPath + "/AddWritePermissions.scpt");

                    //info.FileName = "sudo";
                    //info.Arguments = "chmod +x " + SurroundWithQuotes(filePath);
                }

                Console.WriteLine(info.FileName + " " + info.Arguments);
                Process p = new Process { StartInfo = info };
                p.Start();
                p.WaitForExit();

                await Task.Delay(25);

                Console.WriteLine("OK!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't make executable: " + filePath);
                Console.WriteLine(e);
            }
        }

        public static string GetFullFilePath(string localFilePath, string rootPath)
        {
            return rootPath + "/" + localFilePath;
        }

        public static Encoding GetEncoding(string filename)
        {
            using (StreamReader streamReader = new StreamReader(filename, Encoding.Default, true))
            {
                if (streamReader.Peek() >= 0)
                    streamReader.Read();
                return streamReader.CurrentEncoding;
            }
        }

        public static void CopyToClipboard(this string s)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebNativeDialog.SetUpOverlayDialog("Select all text below and copy:", s, "Close");
#else

            GUIStyle style = new GUIStyle();
            style.richText = false;

            TextEditor te = new TextEditor();
            te.style = style;
            te.text = s;
            te.SelectAll();
            te.Copy();

#endif
        }
    }
}