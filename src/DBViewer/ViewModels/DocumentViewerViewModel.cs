using Couchbase.Lite;
using Dawn;
using DbViewer.Dialogs;
using DbViewer.Models;
using DbViewer.Services;
using DbViewer.Shared.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Navigation;
using Prism.Services.Dialogs;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace DbViewer.ViewModels
{
    public class DocumentViewerViewModel : NavigationViewModelBase, INavigationAware
    {
        private string _documentId;
        private readonly IDialogService _dialogService;
        private readonly IHubService _hubService;

        private IDatabaseConnection _databaseConnection;
        private Document _couchbaseDocument;

        public DocumentViewerViewModel(INavigationService navigationService, IDialogService dialogService, IHubService hubService)
            : base(navigationService)
        {
            _dialogService = Guard.Argument(dialogService, nameof(dialogService))
                  .NotNull()
                  .Value;

            _hubService = Guard.Argument(hubService, nameof(hubService))
                  .NotNull()
                  .Value;

            ShareCommand = ReactiveCommand.CreateFromTask(ExecuteShareAsync);
            SaveCommand = ReactiveCommand.CreateFromTask(ExecuteSaveAsync);
            ReloadCommand = ReactiveCommand.CreateFromTask(ExecuteReloadAsync);
        }

        public DocumentModel DocumentModel { get; private set; }

        public ReactiveCommand<Unit, Unit> ShareCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> ReloadCommand { get; }

        public string DocumentId
        {
            get => _documentId;
            set => this.RaiseAndSetIfChanged(ref _documentId, value, nameof(DocumentId));
        }

        private string _documentText;
        public string DocumentText
        {
            get => _documentText;
            set => this.RaiseAndSetIfChanged(ref _documentText, value);
        }

        public void OnNavigatedFrom(INavigationParameters parameters)
        {

        }

        public void OnNavigatedTo(INavigationParameters parameters)
        {
            if (parameters.ContainsKey(nameof(Models.DocumentModel)))
            {
                DocumentModel = parameters.GetValue<DocumentModel>(nameof(Models.DocumentModel));
            }

            Reload();
        }

        private void Reload()
        {
            var documentText = GetJson();

            RunOnUi(() =>
            {
                DocumentId = DocumentModel?.DocumentId;
                DocumentText = documentText;
            });
        }

        private string GetJson()
        {
            if (DocumentModel?.Database == null)
            {
                return "";
            }

            _databaseConnection = DocumentModel.Database.ActiveConnection;
            _couchbaseDocument = _databaseConnection.GetDocumentById(DocumentModel.DocumentId);

            var jsonOutput = JsonConvert.SerializeObject(_couchbaseDocument, Formatting.Indented);

            return jsonOutput;
        }

        private async Task ExecuteSaveAsync(CancellationToken cancellationToken)
        {
            var errorMessage = "";
            JObject json = null;

            try
            {
                json = JObject.Parse(DocumentText);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error saving JSON: {ex.Message}";
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                var dialogParameters = new DialogParameters
                {
                    { DialogNames.MainMessageParam, errorMessage }
                };

                await _dialogService.ShowDialogAsync(DialogNames.General, dialogParameters);

                return;
            }

            var documentInfo = new DocumentInfo(DocumentModel.Database.RemoteDatabaseInfo, _couchbaseDocument.Id, _couchbaseDocument.RevisionID, DocumentText);

            var updatedDocument = await _hubService.SaveDocument(documentInfo, cancellationToken);

            if (updatedDocument == null)
            {
                // TODO: <James Thomas: 6/27/21> Handle 

                //Log and return since we didn't save to source
                // Do we retore json?

                return;
            }

            UpdateFromDocumentInfo(updatedDocument);
        }

        private async Task ExecuteReloadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var updatedDocument = await _hubService.FetchDocument(DocumentModel.Database.RemoteDatabaseInfo, _couchbaseDocument.Id, cancellationToken);

                UpdateFromDocumentInfo(updatedDocument);
            }
            catch (Exception ex)
            {

            }
        }

        private void UpdateFromDocumentInfo(DocumentInfo documentInfo)
        {
            var dictionary = Shared.Couchbase.CbUtils.ParseTo<Dictionary<string, object>>(documentInfo.DataAsJson);

            var mutableDoc = _couchbaseDocument.ToMutable();
            mutableDoc.SetData(dictionary);

            _databaseConnection.SaveDocument(mutableDoc);

            _couchbaseDocument = mutableDoc;

            DocumentText = documentInfo.DataAsJson;
        }

        private async Task ExecuteShareAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var textRequest = new ShareTextRequest(DocumentText);
            await Share.RequestAsync(textRequest).ConfigureAwait(false);
        }
    }
}