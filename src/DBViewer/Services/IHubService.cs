using DbViewer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DbViewer.Api;
using System.Threading;
using DbViewer.Shared.Dtos;

namespace DbViewer.Services
{
    public interface IHubService
    {
        IDbHubHttpClient GetConnection(Uri hubUri);

        Task<HubInfo> TryAddHubAsync(Uri hubUri, CancellationToken cancellationToken);

        Task<HubInfo> GetCachedHubAsync(string hubId, CancellationToken cancellationToken);

        Task<IEnumerable<DatabaseInfo>> ListAllHubDatabasesAsync(Uri hubUri, CancellationToken cancellationToken);

        Task<DownloadResult> DownloadDatabaseAsync(Uri hubUri, DatabaseInfo databaseInfo, CancellationToken cancellationToken);

        Task<List<HubInfo>> ListAllKnownHubsAsync(CancellationToken cancellationToken);

        Task<bool> UpdateHubAsync(HubInfo hubInfo, CancellationToken cancellationToken);

        Task<DocumentInfo> SaveDocument(DocumentInfo documentInfo, CancellationToken cancellationToken);

        Task<DocumentInfo> FetchDocument(DatabaseInfo databaseInfo, string documentId, CancellationToken cancellationToken);
    }
}