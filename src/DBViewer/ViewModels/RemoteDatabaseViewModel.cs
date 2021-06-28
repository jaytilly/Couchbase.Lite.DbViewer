using Dawn;
using DbViewer.Shared.Dtos;
using ReactiveUI;

namespace DbViewer.ViewModels
{
    public class RemoteDatabaseViewModel : ViewModelBase
    {
        public RemoteDatabaseViewModel(DatabaseInfo databaseInfo)
        {
            DatabaseInfo = Guard.Argument(databaseInfo, nameof(databaseInfo))
                  .NotNull()
                  .Value;

            DisplayName = DatabaseInfo.DisplayDatabaseName;
        }

        public DatabaseInfo DatabaseInfo { get; set; }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set => this.RaiseAndSetIfChanged(ref _displayName, value);
        }

        private bool _shouldDownload;
        public bool ShouldDownload
        {
            get => _shouldDownload;
            set => this.RaiseAndSetIfChanged(ref _shouldDownload, value);
        }
    }
}
