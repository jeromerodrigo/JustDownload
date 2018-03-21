using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Linq;

namespace JustDownload.Shared
{
    public enum ConfigKey
    {
        TextRecordManagerFilePath
    }

    public class JustDownloadFactory
    {
        static IRecordManager recordManager;
        static IDownloader downloader;

        public static IRecordManager GetRecordManager(Dictionary<ConfigKey, object> config = null)
        {
            if (recordManager == null)
            {
                Debug.WriteLine("Using default TextRecordManager");
                recordManager = new TextRecordManager();
            }

            if (config?.ContainsKey(ConfigKey.TextRecordManagerFilePath) ?? false)
            {
                Debug.WriteLine("ConfigKey.TextRecordManagerFilePath is provided");

                var filePath = config[ConfigKey.TextRecordManagerFilePath] as string;

                if (String.IsNullOrWhiteSpace(filePath))
                {
                    throw new Exception("TextRecordManagerFilePath is empty!");
                }

                (recordManager as TextRecordManager).SetFilePath(filePath);
            }

            return recordManager;
        }

        public static IDownloader GetDownloader()
        {
            if (downloader == null)
            {
                downloader = new HttpClientDownloader();
            }

            return downloader;
        }
    }

    public class HttpClientDownloader : IDownloader
    {
        const int DEFAULT_CONCURRENT_DOWNLOADS = 2;
        static HttpClient httpClient = new HttpClient();

        public static int GetFileCount = 0;

        internal HttpClientDownloader()
        {
            
        }

        public async Task GetFile(DownloadRecord downloadRecord)
        {
            GetFileCount++;
            Debug.WriteLine($"Start download of {downloadRecord.Name} from {downloadRecord.Source}");

            try
            {
                var response = await httpClient.GetAsync(downloadRecord.Source);

                response.EnsureSuccessStatusCode();

                using (var httpStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(downloadRecord.Destination?.AbsolutePath ?? downloadRecord.Filename))
                {
                    httpStream.CopyTo(fileStream);
                    fileStream.Flush();
                }

                // Mark record as successful
                downloadRecord.IsSuccess = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{downloadRecord.Name}, Ex: {e.GetType()} {e.Message}");

                // Mark record as failed
                downloadRecord.IsSuccess = false;

                throw e;
            }
        }

        public async Task GetFiles(ICollection<DownloadRecord> downloadRecords, Action<DownloadRecord> onErrorAction = null)
        {
            foreach (var batch in Utility.SplitList(downloadRecords.ToList(), DEFAULT_CONCURRENT_DOWNLOADS))
            {
                try
                {
                    await Task.WhenAll(batch.Select(record => GetFile(record)));
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Caught exception: {e.GetType()} {e.Message}");

                    // Notify caller that exception has happened
                    if (onErrorAction != null)
                        batch.Where(record => !record.IsSuccess ?? true).ToList().ForEach(record => onErrorAction.Invoke(record));
                    
                    // TODO Make configurable stop on first failure
                }
            }

            Debug.WriteLine($"GetFileCount: {HttpClientDownloader.GetFileCount}");
        }
    }

    class TextRecordManager : IRecordManager
    {
        const string DefaultFileName = "records.txt";
        string recordsTextFilePath;

        public TextRecordManager()
        {
            recordsTextFilePath = Path.Combine(Directory.GetCurrentDirectory(), DefaultFileName);
        }

        public void SetFilePath(string filePath)
        {
            recordsTextFilePath = filePath;
        }

        public async Task<ICollection<DownloadRecord>> GetRecords()
        {
            if (!File.Exists(recordsTextFilePath))
            {
                throw new FileNotFoundException($"{recordsTextFilePath} was not found!");
            }

            List<string> lines;

            using (StreamReader reader = File.OpenText(recordsTextFilePath))
            {
                lines = new List<string>();

                while (!reader.EndOfStream)
                {
                    lines.Add(await reader.ReadLineAsync());
                }
            }

            if (lines == null || lines.Count == 0)
            {
                return new List<DownloadRecord>();
            }

            List<DownloadRecord> downloadRecords = lines.ConvertAll(line =>
            {
                return DownloadRecordMapper.MapFromTextLine(line);
            });

            return downloadRecords;
        }

        public Task Save(DownloadRecord record)
        {
            throw new NotImplementedException();
        }

        public Task Save(IEnumerable<DownloadRecord> records)
        {
            throw new NotImplementedException();
        }
    }

    class DownloadRecordMapper
    {
        public static DownloadRecord MapFromTextLine(string line)
        {
            string[] elements = line.Split(',');

            if (elements.Length < 3)
            {
                throw new ArgumentException($"Expected at least 3 values in the text line! But only got {elements.Length}");
            }

            string name = elements[0];
            string filename = elements[1];

            Uri locationUri = ValidateAndGetUri(elements[2]);

            if (locationUri.Scheme != Uri.UriSchemeHttp && locationUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new Exception("Invalid URL scheme detected! http and https only allowed.");
            }

            Uri destination = null;

            if (elements.Length > 3)
            {
                destination = ValidateAndGetUri(elements[3]);
            }

            return new DownloadRecord()
            {
                Name = name,
                Filename = filename,
                Source = locationUri,
                Destination = destination
            };
        }

        static Uri ValidateAndGetUri(string uriString)
        {
            if (!Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
            {
                throw new UriFormatException($"The url: {uriString} is not well formed!");
            }

            return new Uri(uriString);
        }
    }

    public static class Utility
    {
        public static IEnumerable<T> GetBatch<T>(IEnumerable<T> collection, int batchNumber, int itemsInBatch)
            => collection.Skip(batchNumber * itemsInBatch).Take(itemsInBatch);

        public static int CalculateTotalNumberOfBatches(int totalNumberOfItems, int itemsPerBatch) 
            => Convert.ToInt32(Math.Ceiling((double) totalNumberOfItems / itemsPerBatch));

        public static IEnumerable<List<T>> SplitList<T>(List<T> items, int nSize)
        {
            for (int i = 0; i < items.Count; i += nSize)
            {
                yield return items.GetRange(i, Math.Min(nSize, items.Count - i));
            }
        }
    }
}
