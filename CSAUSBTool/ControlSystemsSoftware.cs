﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSAUSBTool
{
    public class ControlSystemsSoftware
    {
        public string Name { get; }
        private Uri Uri { get; }
        private string Hash { get; }
        public string FileName { get; }
        private bool Unzip { get; set; }

        public ControlSystemsSoftware(string name, string fileName, string url, string hash, bool unzip)
        {
            if (url == "")
                return;

            Name = name;
            FileName = fileName;
            Uri = new Uri(url);
            Hash = hash;
            Unzip = unzip;
        }

        public async void Download(string path, DownloadProgressChangedEventHandler progress, bool async)
        {
            if (FileName == "") return;
            if (Uri.ToString().StartsWith("local:"))
            {
                CopyLocal(Uri.ToString().Replace("local:", ""), path);
                return;
            }

            if (FileName.Contains("VSCode") || FileName.Contains("WPILib"))
            {
                await DownloadHTTP(path);
            }
            else
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadProgressChanged += progress;

                    client.DownloadFileCompleted += (sender, eventargs) =>
                    {
                        Console.Out.WriteLine("Download finished for: " + Name);
                    };
                    if (async)
                        client.DownloadFileAsync(Uri, path + @"\" + FileName);
                    else
                    {
                        client.DownloadFile(Uri, path + @"\" + FileName);
                        Console.Out.WriteLine("Download finished for: " + Name);
                        Thread.Sleep(1000);
                        IsValid(path);
                    }
                }
            }
        }

        async Task DownloadHTTP(string path)
        {
            var noCancel = new CancellationTokenSource();

            using (var client = new HttpClientDownloadWithProgress(Uri.ToString(), path + @"\" + FileName))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    if (progressPercentage != null)
                    {
                        CSAUSBTool.toolStripProgressBar.Value = (int)progressPercentage;
                    }
                };

                await client.StartDownload(noCancel.Token);
            }
        }

        public void CopyLocal(string sourcePath, string destPath)
        {
            File.Copy(sourcePath, destPath + @"\" + FileName);
        }

        public bool IsValid(string path)
        {
            string calc = CalculateMd5(path + @"\" + FileName);
            Console.Out.WriteLine(FileName + " provided md5: " + Hash + " calculated md5: " + calc);
            return Hash == calc;
        }

        public void UnzipFile(string path)
        {
            if (!Unzip) return;
            ZipFile.ExtractToDirectory(path + @"\" + FileName, FileName);
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        private string CalculateMd5(string filepath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filepath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}