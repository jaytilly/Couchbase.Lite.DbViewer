using Dawn;
using DbViewer.Shared;
using DbViewer.Shared.Dtos;
using DbViewer.Shared.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DbViewer.Hub.Services
{
    [ServiceType("Local Directory Scanner", "local-directory-scanner")]
    public class LocalDbScanner : IDbScanner, IService
    {
        private const string LocalPath_ConfigKey = "LocalDbDirectory";

        private readonly ILogger<LocalDbScanner> _logger;
        private ServiceInfo _serviceInfo;

        public LocalDbScanner(ILogger<LocalDbScanner> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public void InitiateService(ServiceInfo serviceInfo)
        {
            _serviceInfo = Guard.Argument(serviceInfo)
                                .NotNull()
                                .Value;

            _logger.LogInformation($"Initiating service {serviceInfo.ServiceName} of type {serviceInfo.ServiceTypeId}");

            LocalDirectory = _serviceInfo.Properties.FirstOrDefault(prop => prop.Key == LocalPath_ConfigKey)?.Value;
        }

        [ServiceProperty(LocalPath_ConfigKey, "Local Scan Directory", "TestDatabase", "This is the path to root directory that contains Couchbase Lite database directories.")]
        public string LocalDirectory { get; set; }

        public IEnumerable<DatabaseInfo> Scan()
        {
            var list = new List<DatabaseInfo>();

            _logger.LogInformation($"DBRoot dir: {LocalDirectory}");

            if (!Directory.Exists(LocalDirectory))
            {
                _logger.LogInformation($"Directory {LocalDirectory} does not exist");
                return list;
            }

            var directories = Directory.GetDirectories(LocalDirectory);

            if (directories.IsNullOrEmpty())
            {
                _logger.LogInformation($"No databases found within {LocalDirectory}");
                return list;
            }

            foreach (var dir in directories)
            {
                _logger.LogInformation($"Adding database: {dir}");

                list.Add(new DatabaseInfo()
                {
                    DisplayDatabaseName = Path.GetFileNameWithoutExtension(dir),
                    FullDatabaseName = Path.GetFileName(dir),
                    RemoteRootDirectory = LocalDirectory
                });
            }

            return list;
        }
    }
}
