using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JustDownload.Shared
{
    public interface IRecordManager
    {
        Task Save(DownloadRecord record);
        Task Save(IEnumerable<DownloadRecord> records);
        Task<ICollection<DownloadRecord>> GetRecords();
    }
}
