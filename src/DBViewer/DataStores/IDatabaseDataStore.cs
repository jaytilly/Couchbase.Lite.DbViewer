using DbViewer.Models;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using DbViewer.Shared.Dtos;

namespace DbViewer.DataStores
{
    public interface IDatabaseDatastore
    {
        IObservable<CachedDatabaseRegistry> CacheUpdated { get; }

        void Cleanup(CachedDatabaseRegistry cacheRegistry);
        Task DeleteDatabaseAsync(CachedDatabase database, CancellationToken cancellationToken);
        Task<CachedDatabaseRegistry> GetRegistryAsync(CancellationToken cancellationToken);
        Task SaveFromStreamAsync(Stream databaseDownloadStream, DatabaseInfo databaseInfo, CancellationToken cancellationToken);
    }
}
