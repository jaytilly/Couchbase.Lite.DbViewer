using Akavache;
using DbViewer.Models;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Threading;
using Serilog;
using DbViewer.Shared.Dtos;

namespace DbViewer.DataStores
{
    public class DatabaseDatastore : IDatabaseDatastore
    {
        private const string DatabaseCacheKey = "Database_Cache";

        private readonly ILogger _logger = Log.ForContext<DatabaseDatastore>();

        public DatabaseDatastore()
        {
            CacheUpdated = new BehaviorSubject<CachedDatabaseRegistry>(new CachedDatabaseRegistry());
        }

        public IObservable<CachedDatabaseRegistry> CacheUpdated { get; }

        public async Task DeleteDatabaseAsync(CachedDatabase database, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var registry = await GetRegistryAsync(cancellationToken);

            registry.DatabaseCollection.RemoveAll(db => db.LocalDatabasePathFull == database.LocalDatabasePathFull);

            SaveRegistry(registry);
        }

        public async Task SaveFromStreamAsync(Stream databaseDownloadStream, DatabaseInfo databaseInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var registry = await GetRegistryAsync(cancellationToken)
                .ConfigureAwait(false);

            var dbItem = registry.DatabaseCollection
                                            .FirstOrDefault(
                                    db => string.Equals(db.RemoteDatabaseInfo.DisplayDatabaseName, databaseInfo.DisplayDatabaseName));

            if (dbItem == null || !Directory.Exists(dbItem.LocalDatabasePathRoot))
            {
                dbItem = new CachedDatabase(databaseInfo, DateTimeOffset.Now);
                registry.DatabaseCollection
                        .Add(dbItem);
            }
            else
            {
                dbItem.ActiveConnection?.Disconnect();
            }

            if (File.Exists(dbItem.ArchiveFullPath))
            {
                File.Delete(dbItem.ArchiveFullPath);
            }

            using (var fileStream = new FileStream(dbItem.ArchiveFullPath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                databaseDownloadStream.CopyTo(fileStream);
                fileStream.Close();
            }

            UnzipDbStream(dbItem);

            dbItem.DownloadTime = DateTimeOffset.Now;
            SaveRegistry(registry);
        }

        public bool UnzipDbStream(CachedDatabase cachedDb)
        {
            try
            {
                if (Directory.Exists(cachedDb.LocalDatabasePathFull))
                {
                    Directory.Delete(cachedDb.LocalDatabasePathFull, true);
                }

                Directory.CreateDirectory(cachedDb.LocalDatabasePathFull);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(UnzipDbStream));
                return false;
            }

            try
            {
                var fastZip = new FastZip();
                fastZip.ExtractZip(cachedDb.ArchiveFullPath, cachedDb.LocalDatabasePathFull, null);

                File.Delete(cachedDb.ArchiveFullPath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(UnzipDbStream));
                return false;
            }

            return true;
        }

        public void Cleanup(CachedDatabaseRegistry cacheRegistry)
        {
            var dbsWithBadPaths = new List<CachedDatabase>();
            foreach (var item in cacheRegistry.DatabaseCollection)
            {
                if (!Directory.Exists(item.LocalDatabasePathFull))
                {
                    dbsWithBadPaths.Add(item);
                }
            }

            foreach (var badDb in dbsWithBadPaths)
            {
                cacheRegistry.DatabaseCollection
                             .Remove(badDb);
            }

            SaveRegistry(cacheRegistry);
        }

        private void SaveRegistry(CachedDatabaseRegistry registry)
        {
            BlobCache.LocalMachine
                     .InsertObject(DatabaseCacheKey, registry)
                     .Subscribe(
            _ =>
            {
                ((ISubject<CachedDatabaseRegistry>)CacheUpdated).OnNext(registry);
            });
        }

        public async Task<CachedDatabaseRegistry> GetRegistryAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_inMemoryRegistry == null)
            {
                try
                {
                    _inMemoryRegistry = await BlobCache.LocalMachine
                                                       .GetOrCreateObject(DatabaseCacheKey, () => new CachedDatabaseRegistry());

                    Cleanup(_inMemoryRegistry);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, nameof(GetRegistryAsync));
                }
            }

            return _inMemoryRegistry;
        }

        private CachedDatabaseRegistry _inMemoryRegistry;
    }
}
