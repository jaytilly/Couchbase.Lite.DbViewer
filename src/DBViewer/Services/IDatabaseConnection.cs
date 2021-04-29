﻿using Couchbase.Lite;
using System.Collections.Generic;

namespace DBViewer.Services
{
    public interface IDatabaseConnection
    {
        bool IsConnected { get; }

        bool Connect(string dbDirectory, string dbName);
        List<string> ListAllDocumentIds();
        Document GetDocumentById(string id);
        bool Disconnect();
    }
}