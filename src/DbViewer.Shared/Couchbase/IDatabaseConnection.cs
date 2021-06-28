using Couchbase.Lite;
using System.Collections.Generic;

namespace DbViewer.Hub.Couchbase
{
    public interface IDatabaseConnection
    {
        bool IsConnected { get; }
        bool Connect(string dbDirectory, string dbName);
        List<string> ListAllDocumentIds(bool sort = false);
        Document GetDocumentById(string id);
        bool Disconnect();
        void SaveDocument(MutableDocument document);
    }
}
