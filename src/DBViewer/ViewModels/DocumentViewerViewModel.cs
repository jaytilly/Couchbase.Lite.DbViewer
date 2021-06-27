using Couchbase.Lite;
using Dawn;
using DbViewer.Dialogs;
using DbViewer.Models;
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

        private IDatabaseConnection _databaseConnection;
        private Document _couchbaseDocument;

        public DocumentViewerViewModel(INavigationService navigationService, IDialogService dialogService)
            : base(navigationService)
        {
            _dialogService = Guard.Argument(dialogService, nameof(dialogService))
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


            var dictionary = ParseTo<Dictionary<string,object>>(DocumentText);

            var mutableDoc = _couchbaseDocument.ToMutable();
            mutableDoc.SetData(dictionary);

            _databaseConnection.SaveDocument(mutableDoc);
        }

        private static T ParseTo<T>(string json)
        {
            T retVal = default(T);
            try
            {
                var settings = new JsonSerializerSettings
                {
                    DateParseHandling = DateParseHandling.DateTimeOffset,
                    TypeNameHandling = TypeNameHandling.All
                };
                retVal = JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch
            {

            }

            return retVal;
        }

        private async Task ExecuteReloadAsync(CancellationToken cancellationToken)
        {

        }

        private async Task ExecuteShareAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var textRequest = new ShareTextRequest(DocumentText);
            await Share.RequestAsync(textRequest).ConfigureAwait(false);
        }
    }
}