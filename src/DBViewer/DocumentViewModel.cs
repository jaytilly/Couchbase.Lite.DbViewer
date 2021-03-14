﻿using Couchbase.Lite;
using DBViewer.Data;
using Newtonsoft.Json;
using ReactiveUI;

namespace DBViewer
{
    public class DocumentViewModel : ReactiveObject
    {
        private readonly DataService _dataService;
        private readonly DocumentGroupViewModel _parentViewModel;

        private string _documentId;

        public DocumentViewModel(DocumentGroupViewModel documentGroupViewModel, DataService dataService,
            string documentId)
        {
            _parentViewModel = documentGroupViewModel;
            _dataService = dataService;

            DocumentId = documentId;
        }

        public Document Document { get; }

        public string DocumentId
        {
            get => _documentId;
            set => this.RaiseAndSetIfChanged(ref _documentId, value, nameof(DocumentId));
        }

        /// <summary>
        ///     We should load the document and pass back so that tree nodes can be created
        /// </summary>
        /// <returns></returns>
        internal string GetJson()
        {
            var document = _dataService.GetDocumentById(DocumentId);
            return JsonConvert.SerializeObject(document, Formatting.Indented);
        }
    }
}