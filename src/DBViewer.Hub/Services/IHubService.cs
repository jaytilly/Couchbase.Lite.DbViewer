using DbViewer.Shared.Dtos;
using System.Collections.Generic;

namespace DbViewer.Hub.Services
{
    public interface IHubService
    {
        public HubInfo GetLatestHub();
        public void SaveLatestHub(HubInfo hubInfo);
        public List<IDbScanner> GetAllDbScanners();
        DocumentInfo SaveDocumentToDatabase(DocumentInfo document);
        DocumentInfo GetDocumentById(DatabaseInfo databaseInfo, string documentId);
    }
}
