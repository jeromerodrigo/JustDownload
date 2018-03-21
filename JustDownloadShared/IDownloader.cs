using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JustDownload.Shared
{
    public interface IDownloader
    {
        Task GetFile(DownloadRecord downloadRecord);
        Task GetFiles(ICollection<DownloadRecord> downloadRecords, Action<DownloadRecord> onErrorAction = null);
    }
}
