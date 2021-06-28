using Dawn;
using DbViewer.Api;
using DbViewer.DataStores;
using DbViewer.Models;
using DbViewer.Shared.Dtos;
using Refit;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DbViewer.Services
{
    public class HubService : IHubService
    {
        private readonly Dictionary<Uri, IDbHubHttpClient> _hubClients = new Dictionary<Uri, IDbHubHttpClient>();

        private readonly ILogger _logger = Log.ForContext<HubService>();
        private readonly IDatabaseDatastore _dbCacheService;
        private readonly IHubDatastore _hubCacheService;

        public HubService(IDatabaseDatastore dbCacheService, IHubDatastore hubCacheService)
        {
            _dbCacheService = Guard.Argument(dbCacheService, nameof(dbCacheService))
                                   .NotNull()
                                   .Value;

            _hubCacheService = Guard.Argument(hubCacheService, nameof(hubCacheService))
                                    .NotNull()
                                    .Value;
        }

        public DateTimeOffset LastRefreshTime { get; set; }

        public async Task<DownloadResult> DownloadDatabaseAsync(Uri hubUri, DatabaseInfo databaseInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var connection = GetConnection(hubUri);

            var downloadResult = new DownloadResult
            {
                DatabaseInfo = databaseInfo
            };

            Stream stream = null;

            try
            {
                var httpResponseMessage = await connection.GetDatabaseAsync(databaseInfo.DisplayDatabaseName, cancellationToken).ConfigureAwait(false);
                stream = await httpResponseMessage.Content.ReadAsStreamAsync()
                                                  .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(DownloadDatabaseAsync));
            }

            if (stream == null)
                throw new InvalidDataException(nameof(DownloadDatabaseAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                databaseInfo.RequestAddress = hubUri;

                await _dbCacheService.SaveFromStreamAsync(stream, databaseInfo, cancellationToken)
                                     .ConfigureAwait(false);

                downloadResult.WasSuccesful = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(DownloadDatabaseAsync));
            }

            return downloadResult;
        }

        public IDbHubHttpClient GetConnection(Uri hubUri)
        {
            if (!_hubClients.TryGetValue(hubUri, out var client))
            {
                try
                {
                    client = RestService.For<IDbHubHttpClient>(hubUri.ToString(), new RefitSettings
                    {
                        ContentSerializer = new NewtonsoftJsonContentSerializer()
                    });

                    _hubClients.Add(hubUri, client);

                    LastRefreshTime = DateTimeOffset.Now;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, nameof(GetConnection));
                }
            }

            return client;
        }

        public Task<IEnumerable<DatabaseInfo>> ListAllHubDatabasesAsync(Uri hubUri, CancellationToken cancellationToken) => GetConnection(hubUri).ListAllAsync(cancellationToken);

        public Task<HubInfo> GetCachedHubAsync(string hubId, CancellationToken cancellationToken) => _hubCacheService.GetCachedHubAsync(hubId, cancellationToken);

        public Task<List<HubInfo>> ListAllKnownHubsAsync(CancellationToken cancellationToken) => _hubCacheService.ListAllAsync(cancellationToken);

        public async Task<HubInfo> TryAddHubAsync(Uri hubUri, CancellationToken cancellationToken)
        {
            var connection = GetConnection(hubUri);

            HubInfo hubInfo = null;

            try
            {
                hubInfo = await connection.GetHubInfoAsync(cancellationToken)
                                          .ConfigureAwait(false);

                hubInfo.HostAddress = hubUri.ToString();

                await _hubCacheService.SaveHubAsync(hubInfo, cancellationToken)
                                      .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(TryAddHubAsync));
            }

            return hubInfo;
        }

        public async Task<bool> UpdateHubAsync(HubInfo hubInfo, CancellationToken cancellationToken)
        {
            var uri = new Uri(hubInfo.HostAddress);
            var connection = GetConnection(uri);

            try
            {
                await connection.UpdateHubAsync(hubInfo, cancellationToken)
                                .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(UpdateHubAsync));
                return false;
            }

            try
            {
                await _hubCacheService.SaveHubAsync(hubInfo, cancellationToken)
                                      .ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(UpdateHubAsync));
            }

            return false;
        }

        public Task<DocumentInfo> SaveDocument(DocumentInfo documentInfo, CancellationToken cancellationToken)
        {
            var connection = GetConnection(documentInfo.DatabaseInfo.RequestAddress);


            return connection.SaveDocument(documentInfo, cancellationToken);
        }

        public Task<DocumentInfo> FetchDocument(DatabaseInfo databaseInfo, string documentId, CancellationToken cancellationToken)
        {
            var connection = GetConnection(databaseInfo.RequestAddress);

            return connection.GetDocument(new DocumentRequest(databaseInfo, documentId), cancellationToken);
        }
    }
}
