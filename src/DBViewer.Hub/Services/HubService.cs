using Dawn;
using DbViewer.Hub.Couchbase;
using DbViewer.Shared.Couchbase;
using DbViewer.Shared.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DbViewer.Hub.Services
{
    public class HubService : IHubService
    {
        private const string DataFolder = "Data";
        private const string HubInfoFilePath = "HubInfo.json";

        private readonly ILogger<HubService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDatabaseConnection _databaseConnection;

        private HubInfo _hubInfo;

        public HubService(ILogger<HubService> logger, IServiceProvider serviceProvider, IDatabaseConnection databaseConnection)
        {
            _logger = Guard.Argument(logger, nameof(logger))
                           .NotNull()
                           .Value;

            _serviceProvider = Guard.Argument(serviceProvider, nameof(serviceProvider))
                                    .NotNull()
                                    .Value;

            _databaseConnection = Guard.Argument(databaseConnection, nameof(databaseConnection))
                  .NotNull()
                  .Value;

        }

        public HubInfo GetLatestHub()
        {
            _logger.LogDebug("Fetching latest hub info.");

            if (_hubInfo != null)
            {
                return _hubInfo;
            }


            var path = GetPath();

            if (!File.Exists(path))
            {
                _logger.LogDebug("Hub info does not exist. Creating.. ");

                var newHub = CreateNew();
                SaveLatestHub(newHub);

                _logger.LogDebug("Hub info created.");
                return newHub;
            }

            _logger.LogDebug("Loading hub info from disk. ");

            var hubInfoJsonContexts = File.ReadAllText(path);

            _hubInfo = JsonConvert.DeserializeObject<HubInfo>(hubInfoJsonContexts);

            return _hubInfo;
        }

        public void SaveLatestHub(HubInfo hubInfo)
        {
            _logger.LogDebug("Saving latest hub info...");

            var path = GetPath(true);

            var hubInfoJsonContexts = JsonConvert.SerializeObject(hubInfo);

            File.WriteAllText(path, hubInfoJsonContexts);

            _logger.LogDebug("Hub info saved.");

            _hubInfo = hubInfo;
        }

        public List<IDbScanner> GetAllDbScanners()
        {
            var hubInfo = GetLatestHub();

            var scanners = new List<IDbScanner>();

            var serviceTypes = hubInfo.ServiceDefinitions;

            foreach (var serviceInfo in hubInfo.ActiveServices)
            {
                var matchingType = serviceTypes.FirstOrDefault(st => st.Id == serviceInfo.ServiceTypeId);

                if (matchingType == null)
                {
                    continue;
                }

                var serviceType = Type.GetType(matchingType.FullyQualifiedAssemblyTypeName);

                var service = _serviceProvider.GetService(serviceType);

                if (service is IDbScanner scanner)
                {
                    scanners.Add(scanner);

                    if (service is IService hubService)
                    {
                        hubService.InitiateService(serviceInfo);
                    }
                }
            }

            return scanners;
        }

        public DocumentInfo SaveDocumentToDatabase(DocumentInfo documentInfo)
        {
            var isConnected = _databaseConnection.Connect(documentInfo.DatabaseInfo.RemoteRootDirectory, documentInfo.DatabaseInfo.DisplayDatabaseName);

            // TODO: <James Thomas: 6/27/21> Switch to full response object instead of null
            if (!isConnected)
            {
                return null;
            }

            var document = _databaseConnection.GetDocumentById(documentInfo.DocumentId);

            var dictionary = CbUtils.ParseTo<Dictionary<string, object>>(documentInfo.DataAsJson);

            var mutableDoc = document.ToMutable();
            mutableDoc.SetData(dictionary);

            _databaseConnection.SaveDocument(mutableDoc);

            mutableDoc.Dispose();

            _databaseConnection.Disconnect();

            return GetDocumentById(documentInfo.DatabaseInfo, documentInfo.DocumentId);
        }

        public DocumentInfo GetDocumentById(DatabaseInfo databaseInfo, string documentId)
        {
            var isConnected = _databaseConnection.Connect(databaseInfo.RemoteRootDirectory, databaseInfo.DisplayDatabaseName);

            // TODO: <James Thomas: 6/27/21> Switch to full response object instead of null
            if (!isConnected)
            {
                return null;
            }

            var document = _databaseConnection.GetDocumentById(documentId);

            var updatedJson = JsonConvert.SerializeObject(document, Formatting.Indented);

            var documentInfo = new DocumentInfo(databaseInfo, documentId, document.RevisionID, updatedJson);

            document.Dispose();

            _databaseConnection.Disconnect();

            return documentInfo;
        }

        private static string GetPath(bool ensure = false)
        {
            var filePath = Path.Combine(DataFolder, HubInfoFilePath);

            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }

            return filePath;
        }

        private HubInfo CreateNew()
        {
            var serviceTypes = ServiceScanner.GetServicesForAssembly(GetType().Assembly);
            var localDbScanner = serviceTypes.FirstOrDefault(st => st.FullyQualifiedAssemblyTypeName == typeof(LocalDbScanner).AssemblyQualifiedName);

            var hubInfo = new HubInfo
            {
                HubName = $"{Environment.MachineName} - Hub",
                Id = Guid.NewGuid().ToString(),
                ServiceDefinitions = serviceTypes,
                ActiveServices = localDbScanner == null ? new List<ServiceInfo>() : new List<ServiceInfo>() { CreateServiceFromDefinition(localDbScanner) }
            };

            return hubInfo;
        }

        private ServiceInfo CreateServiceFromDefinition(ServiceDefinition definition)
        {
            var serviceInfo = new ServiceInfo
            {
                Id = Guid.NewGuid().ToString(),
                ServiceName = definition.Name,
                ServiceTypeId = definition.Id
            };

            foreach (var prop in definition.Properties)
            {
                serviceInfo.Properties.Add(prop);
            }

            return serviceInfo;
        }
    }
}
