using Dawn;

namespace DbViewer.Shared.Dtos
{
    public class DocumentRequest
    {
        public DocumentRequest(DatabaseInfo databaseInfo, string documentId)
        {
            DatabaseInfo = Guard.Argument(databaseInfo, nameof(databaseInfo))
                  .NotNull()
                  .Value;

            DocumentId = Guard.Argument(documentId, nameof(documentId))
                  .NotNull()
                  .Value;
        }

        public DatabaseInfo DatabaseInfo { get; set; }

        public string DocumentId { get; set; }
    }
}
