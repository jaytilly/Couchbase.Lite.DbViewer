using Couchbase.Lite;
using Couchbase.Lite.Query;
using Dawn;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DbViewer.Models
{
    public class DatabaseConnection : IDatabaseConnection
    {
        private Database _database;

        public bool IsConnected => _database != null;

        public bool Connect(string dbDirectory, string dbName)
        {
            Guard.Argument(dbDirectory, nameof(dbDirectory))
                 .NotNull()
                 .NotEmpty();

            if (!Directory.Exists(dbDirectory))
            {
                return false;
            }

            var dbConfig = new DatabaseConfiguration
            {
                Directory = dbDirectory
            };

            _database = new Database(dbName, dbConfig);

            return true;
        }

        public bool Disconnect()
        {
            if (_database?.Config == null)
                return true;

            _database.Close();
            _database = null;

            return true;
        }

        public List<string> ListAllDocumentIds(bool sort = false)
        {
            var documentIds = GetAllDocumentsIds(_database, sort);

            if (documentIds == null)
                return new List<string>();

            return documentIds.ToList();
        }

        private IEnumerable<string> GetAllDocumentsIds(Database db, bool sort = false)
        {
            if (_database == null)
            {
                return Enumerable.Empty<string>();
            }

            var docIds = QueryBuilder
                .Select(SelectResult.Expression(Meta.ID),
                    SelectResult.Property("Type")).From(DataSource.Database(db)).Execute()
                .Select(i => i.GetString("id"))
                .Where(docId => docId != null).ToList();

            if (sort)
            {
                docIds.Sort();
            }

            return docIds;
        }

        public Document GetDocumentById(string id)
        {
            return _database.GetDocument(id);
        }

        public void SaveDocument(MutableDocument document)
        {
            _database.Save(document);
        }
    }
}