using System;
using System.Collections.Generic;
using System.Text;

namespace JustDownload.Shared
{
    public class DownloadRecord
    {
        public string Name { get; set; }
        public string Filename { get; set; }
        public Uri Source { get; set; }
        public Uri Destination { get; set; }
    }
}
