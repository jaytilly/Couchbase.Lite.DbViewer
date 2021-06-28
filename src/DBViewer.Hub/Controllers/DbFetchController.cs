using Dawn;
using DbViewer.Hub.Services;
using DbViewer.Shared;
using DbViewer.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DbViewer.Hub.Controllers
{
    [ApiController]
    [Route("Databases")]
    public class DbFetchController : ControllerBase
    {
        private readonly ILogger<DbFetchController> _logger;
        private readonly IHubService _hubService;

        public DbFetchController(ILogger<DbFetchController> logger, IHubService hubService)
        {
            _logger = Guard.Argument(logger, nameof(logger))
                  .NotNull()
                  .Value;

            _hubService = Guard.Argument(hubService, nameof(hubService))
                  .NotNull()
                  .Value;
        }

        [HttpGet]
        public IEnumerable<DatabaseInfo> ListAllDbs()
        {
            _logger.LogInformation("Fetching DB Info");

            var scanners = _hubService.GetAllDbScanners();

            if (scanners == null)
                return Enumerable.Empty<DatabaseInfo>();

            var dbs = new List<DatabaseInfo>();

            foreach(var scanner in scanners)
            {
               dbs.AddRange(scanner.Scan());
            }

            return dbs;
        }

        [HttpGet("Name/{displayName}")]
        public Stream GetDatabase([FromRoute] string displayName)
        {
            var listDbs = ListAllDbs();

            var dbInfo = listDbs.FirstOrDefault(db => db.DisplayDatabaseName.Equals(displayName, StringComparison.OrdinalIgnoreCase));

            if (dbInfo == null)
                return null;

            var dbPath = Path.Combine(dbInfo.RemoteRootDirectory, dbInfo.FullDatabaseName);
            var zipPath = dbPath + ".zip";

            if (System.IO.File.Exists(zipPath))
            {
                System.IO.File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(dbPath, zipPath);

            return !System.IO.File.Exists(zipPath) ? null : new FileStream(zipPath, FileMode.Open);
        }

        [HttpPut("Document/{documentInfo}")]
        public  DocumentInfo SaveDocument(DocumentInfo documentInfo)
        {
            var updateDocumentInfo = _hubService.SaveDocumentToDatabase(documentInfo);

            return updateDocumentInfo;
        }

        [HttpGet("Document")]
        public  DocumentInfo GetDocument([FromBody] DocumentRequest documentRequest)
        {
            var documentInfo = _hubService.GetDocumentById(documentRequest.DatabaseInfo, documentRequest.DocumentId);

            return documentInfo;
        }
    }
}
