using DbViewer.Shared.Dtos;
using System.Collections.Generic;

namespace DbViewer.Hub.Services
{
    public interface IDbScanner
    {
        public IEnumerable<DatabaseInfo> Scan();
    }
}
