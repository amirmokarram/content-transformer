using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ContentTransformer.Common.ContentSource;
using ContentTransformer.Services.ContentSource.Sources;
using FluentAssertions;
using Xunit;

namespace ContentTransformer.Test
{
    public class ContentSourceTest
    {
        [Fact]
        public void Content_source_file_system_test()
        {
            const int totalFiles = 5;

            using (FileSystemContentSource fsContentSource = new FileSystemContentSource())
            {
                string pathToWatch = Path.Combine(Environment.CurrentDirectory, "$fsContentSource");
                if (Directory.Exists(pathToWatch))
                    Directory.Delete(pathToWatch, true);
                
                void ContentGenerator()
                {
                    for (int i = 0; i < totalFiles; i++)
                    {
                        using (StreamWriter streamWriter = File.CreateText(Path.Combine(pathToWatch, $"SampleFile{i}.txt")))
                            streamWriter.WriteLine("Sample Content...");
                        Thread.Sleep(500);
                    }
                }

                int contentCounter = 0;
                Directory.CreateDirectory(pathToWatch);
                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    {FileSystemContentSource.ArchiveNameConfig, "archive"},
                    {FileSystemContentSource.PathConfig, pathToWatch}
                };
                fsContentSource.Init(parameters);
                fsContentSource.SourceChanged += (sender, args) =>
                {
                    contentCounter++;
                    Debug.WriteLine("File: {0}", args.Items.First().Uri);
                };
                fsContentSource.Start();
                ContentGenerator();
                
                contentCounter.Should().Be(totalFiles);
            }
        }

        [Fact]
        public void Content_source_ftp_test()
        {
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            
            using (FtpContentSource fsContentSource = new FtpContentSource())
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    {FtpContentSource.ArchiveNameConfig, "archive"},
                    {FtpContentSource.HostConfig, "ftp://192.168.1.100"},
                    {FtpContentSource.UsernameConfig, "mokarram"},
                    {FtpContentSource.PasswordConfig, "4168"}
                };
                fsContentSource.Init(parameters);
                fsContentSource.SourceChanged += (sender, args) =>
                {
                    foreach (ContentSourceItem item in args.Items)
                        Debug.WriteLine("File: {0}", item.Uri);

                    manualResetEvent.Set();
                    manualResetEvent.Reset();
                };
                fsContentSource.Start();

                manualResetEvent.WaitOne();

                //fsContentSource.Pause();
                //Thread.Sleep(20000);
                //fsContentSource.Resume();

                //manualResetEvent.WaitOne();
            }
        }
    }
}
