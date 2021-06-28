using Dawn;
using System;
using System.IO;
using Newtonsoft.Json;
using Xamarin.Essentials;
using DbViewer.Shared.Dtos;

namespace DbViewer.Models
{
    public class CachedDatabase
    {
        private readonly object _synclock = new object();

        public CachedDatabase()
        {
            
        }

        public CachedDatabase(DatabaseInfo remoteDatabaseInfo, DateTimeOffset downloadTime)
        {
            RemoteDatabaseInfo = Guard.Argument(remoteDatabaseInfo, nameof(remoteDatabaseInfo))
                  .NotNull()
                  .Value;

            DownloadTime = downloadTime;
        }

        [JsonIgnore]
        public IDatabaseConnection ActiveConnection { get; private set; }

        public bool Connect()
        {
            lock (_synclock)
            {
                if (ActiveConnection != null && ActiveConnection.IsConnected)
                    return true;

                ActiveConnection = new DatabaseConnection();

                try
                {
                    ActiveConnection.Connect(LocalDatabasePathRoot, RemoteDatabaseInfo.DisplayDatabaseName);
                    return true;
                }
                catch
                {
                    // TODO: <James Thomas: 4/28/83> Add logging 
                }

                return false;
            }
        }

        public bool Disconnect()
        {
            if (ActiveConnection != null)
            {
                try
                {
                    ActiveConnection.Disconnect();
                    ActiveConnection = null;
                    return true;
                }
                catch
                {
                    // TODO: <James Thomas: 4/28/83> Add logging 
                }

                return false;
            }

            return true;
        }

        public DatabaseInfo RemoteDatabaseInfo { get; set; }

        public DateTimeOffset DownloadTime { get; set; }

        /// <summary>
        /// The path to the directory that contains the database folder. Couchbase uses this with the subdirectory being the 'name'
        /// </summary>
        [JsonIgnore]
        public string LocalDatabasePathRoot => FileSystem.AppDataDirectory;

        /// <summary>
        /// The full path to the directoy that contains the unzipped database contents.
        /// </summary>
        [JsonIgnore]
        public string LocalDatabasePathFull => Path.Combine(LocalDatabasePathRoot, RemoteDatabaseInfo.FullDatabaseName);


        /// <summary>
        /// The full path to the zip file that was downloaded.
        /// </summary>
        [JsonIgnore]
        public string ArchiveFullPath => $"{LocalDatabasePathFull}.zip";
    }
}
