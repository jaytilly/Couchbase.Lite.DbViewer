using Dawn;
using DbViewer.Shared.Dtos;
using ReactiveUI;

namespace DbViewer.ViewModels
{
    public class ScanServiceListItemViewModel : ReactiveObject
    {
        public ScanServiceListItemViewModel(ServiceInfo serviceInfo)
        {
            ServiceInfo = Guard.Argument(serviceInfo, nameof(serviceInfo))
                              .NotNull()
                              .Value;


            DisplayName = ServiceInfo.ServiceName;
        }

        public ServiceInfo ServiceInfo { get; }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set => this.RaiseAndSetIfChanged(ref _displayName, value);
        }
    }
}
