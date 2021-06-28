using DbViewer.Shared.Dtos;

namespace DbViewer.Models
{
    public class DownloadResult
    {
        public DatabaseInfo DatabaseInfo { get; set; }
        public bool WasSuccesful { get; set; }
    }
}
