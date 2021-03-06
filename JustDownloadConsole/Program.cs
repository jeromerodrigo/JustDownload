﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustDownload.Shared;

namespace JustDownloadConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Console.WriteLine($"JustDownload Console v{version}");

            Task.WaitAll(StartDownloading());
        }

        static async Task StartDownloading()
        {
            var recordManager = JustDownloadFactory.GetRecordManager();
            var downloader = JustDownloadFactory.GetDownloader();

            try
            {
                var records = await recordManager.GetRecords();

                if (records?.Any() ?? false)
                {
                    Console.WriteLine("Starting downloads...");

                    await downloader.GetFiles(records, record =>
                    {
                        Console.Error.WriteLine($"Error occurred when downloading {record.Filename} from {record.Source}");
                    });
                }
                else
                {
                    Console.WriteLine("No download records found.");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }
    }
}
