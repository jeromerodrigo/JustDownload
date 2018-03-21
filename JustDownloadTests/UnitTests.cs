using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using JustDownload.Shared;

namespace JustDownload.Test
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public async Task TestGetRecords()
        {
            var recordMgr = JustDownloadFactory.GetRecordManager();

            var records = await recordMgr.GetRecords();

            Assert.IsNotNull(records);
            Assert.IsTrue(records.Count > 0);
        }

        [TestMethod]
        public async Task TestGetRecords_MalformedUri()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "malformed.txt");
            var recordMgr = JustDownloadFactory.GetRecordManager(new Dictionary<ConfigKey, object>() { { ConfigKey.TextRecordManagerFilePath, filePath } });

            var exception = await Assert.ThrowsExceptionAsync<UriFormatException>(async () =>
            {
                await recordMgr.GetRecords();
            });

            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public async Task TestGetRecords_InvalidScheme()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "invalidscheme.txt");
            var recordMgr = JustDownloadFactory.GetRecordManager(new Dictionary<ConfigKey, object>() { { ConfigKey.TextRecordManagerFilePath, filePath } });

            var exception = await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await recordMgr.GetRecords();
            });

            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public async Task TestGetFile_Success()
        {
            var downloader = JustDownloadFactory.GetDownloader();

            var record = new DownloadRecord()
            {
                Name = "Test",
                Filename = "result.pdf",
                Source = new Uri("http://viewnet.com.my/downloads/viewnet_diy_pricelist.pdf")
            };

            await downloader.GetFile(record);

            Assert.IsTrue(File.Exists(record.Filename));
        }

        [TestMethod]
        public async Task TestGetFile_NotFound()
        {
            var downloader = JustDownloadFactory.GetDownloader();

            var record = new DownloadRecord()
            {
                Name = "Nope",
                Filename = "TestGetFile_NotFound.pdf",
                Source = new Uri("http://google.com/test")
            };

            bool exceptionThrown = false;

            try
            {
                await downloader.GetFile(record);
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown!");
            Assert.IsFalse(File.Exists(record.Filename));
        }

        [TestMethod]
        public async Task TestGetFile_WeirdUrl()
        {
            var downloader = JustDownloadFactory.GetDownloader();

            var record = new DownloadRecord()
            {
                Name = "Nope",
                Filename = "TestGetFile_WeirdUrl.pdf",
                Source = new Uri("http://lalalalala.xz")
            };

            bool exceptionThrown = false;

            try
            {
                await downloader.GetFile(record);
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown!");
            Assert.IsFalse(File.Exists(record.Filename));
        }

        [TestMethod]
        public async Task TestGetFiles_Success()
        {
            var downloader = JustDownloadFactory.GetDownloader();

            var records = new DownloadRecord[]
            {
                new DownloadRecord()
                {
                    Name = "Test",
                    Filename = "TestGetFiles_Success result1.pdf",
                    Source = new Uri("http://viewnet.com.my/downloads/viewnet_diy_pricelist.pdf")
                },
                new DownloadRecord()
                {
                    Name = "Test 2",
                    Filename = "TestGetFiles_Success result2.pdf",
                    Source = new Uri("http://viewnet.com.my/downloads/viewnet_diy_pricelist.pdf")
                },
                new DownloadRecord()
                {
                    Name = "Test 3",
                    Filename = "TestGetFiles_Success result3.pdf",
                    Source = new Uri("http://viewnet.com.my/downloads/viewnet_diy_pricelist.pdf")
                },
            };

            await downloader.GetFiles(records);

            foreach (var record in records)
            {
                Assert.IsTrue(File.Exists(record.Filename));
            }
        }

        [TestMethod]
        public async Task TestGetFiles_OneFailure_ShouldContinue()
        {
            var downloader = JustDownloadFactory.GetDownloader();

            var records = new DownloadRecord[]
            {
                new DownloadRecord()
                {
                    Name = "Test",
                    Filename = "TestGetFiles_Failure_result1.pdf",
                    Source = new Uri("http://viewnet.com.my/downloads/viewnet_diy_pricelist.pdf")
                },
                new DownloadRecord()
                {
                    Name = "Test 2",
                    Filename = "TestGetFiles_Failure_result2.pdf",
                    Source = new Uri("http://google.com/lol")
                },
                new DownloadRecord()
                {
                    Name = "Test 3",
                    Filename = "TestGetFiles_Failure_result3.pdf",
                    Source = new Uri("http://viewnet.com.my/downloads/viewnet_diy_pricelist.pdf")
                },
                new DownloadRecord()
                {
                    Name = "Test 4",
                    Filename = "TestGetFiles_Failure_result4.pdf",
                    Source = new Uri("http://viewnet.com.my/downloads/viewnet_diy_pricelist.pdf")
                },
                new DownloadRecord()
                {
                    Name = "Test 5",
                    Filename = "TestGetFiles_Failure_result5.pdf",
                    Source = new Uri("http://google.com/lol")
                },
                new DownloadRecord()
                {
                    Name = "Test 6",
                    Filename = "TestGetFiles_Failure_result6.pdf",
                    Source = new Uri("http://google.com/lol")
                },
            };

            // Reset the counter
            HttpClientDownloader.GetFileCount = 0;

            await downloader.GetFiles(records);

            Assert.IsTrue(File.Exists(records[0].Filename));

            Assert.IsFalse(File.Exists(records[1].Filename));

            Assert.IsTrue(File.Exists(records[2].Filename));
            Assert.IsTrue(File.Exists(records[3].Filename));

            Assert.IsFalse(File.Exists(records[4].Filename));
            Assert.IsFalse(File.Exists(records[5].Filename));

            Assert.AreEqual(records.Count(), HttpClientDownloader.GetFileCount);
        }

        [TestMethod]
        public void TestGetBatch()
        {
            int batchSize = 2;
            var numbers = new int[] { 1, 2, 3, 4, 5 };

            var numberSet1 = Utility.GetBatch(numbers, 0, batchSize);

            Assert.IsNotNull(numberSet1);
            Assert.AreEqual(2, numberSet1.Count());
            Assert.IsTrue(numberSet1.Contains(1));
            Assert.IsTrue(numberSet1.Contains(2));

            var numberSet2 = Utility.GetBatch(numbers, 1, batchSize);

            Assert.IsNotNull(numberSet2);
            Assert.AreEqual(2, numberSet2.Count());
            Assert.IsTrue(numberSet2.Contains(3));
            Assert.IsTrue(numberSet2.Contains(4));

            var numberSet3 = Utility.GetBatch(numbers, 2, batchSize);

            Assert.IsNotNull(numberSet3);
            Assert.AreEqual(1, numberSet3.Count());
            Assert.IsTrue(numberSet3.Contains(5));
            Assert.IsFalse(numberSet3.Contains(6));
        }

        [TestMethod]
        public void TestCalculateNumberOfBatches()
        {
            {
                int numberOfBatches = Utility.CalculateTotalNumberOfBatches(7, 2);

                Assert.AreEqual(4, numberOfBatches);
            }

            {
                int numberOfBatches = Utility.CalculateTotalNumberOfBatches(7, 4);

                Assert.AreEqual(2, numberOfBatches);
            }

            {
                int numberOfBatches = Utility.CalculateTotalNumberOfBatches(7, 3);

                Assert.AreEqual(3, numberOfBatches);
            }
        }

        // TODO Test Save
    }
}
