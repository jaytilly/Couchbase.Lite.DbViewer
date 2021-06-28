using DbViewer.Shared.Dtos;
using Refit;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DbViewer.Api
{
    public interface IDbHubHttpClient
    {
        [Get("/Hubs")]
        Task<HubInfo> GetHubInfoAsync(CancellationToken cancellationToken);

        [Get("/Databases")]
        Task<IEnumerable<DatabaseInfo>> ListAllAsync(CancellationToken cancellationToken);

        [Get("/Databases/Name/{displayName}")]
        Task<HttpResponseMessage> GetDatabaseAsync(string displayName, CancellationToken cancellationToken);

        [Put("/Hubs/Update/{hubInfo}")]
        Task UpdateHubAsync(HubInfo hubInfo, CancellationToken cancellationToken);

        [Put("/Databases/Document/{documentInfo}")]
        Task<DocumentInfo> SaveDocument(DocumentInfo documentInfo, CancellationToken cancellationToken);

        [Get("/Databases/Document")]
        Task<DocumentInfo> GetDocument([Body]DocumentRequest documentRequest, CancellationToken cancellationToken);
    }
}
