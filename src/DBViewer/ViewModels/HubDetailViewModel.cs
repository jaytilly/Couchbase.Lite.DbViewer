using Dawn;
using DbViewer.Models;
using DbViewer.Services;
using DbViewer.Shared.Dtos;
using DbViewer.Views;
using Prism.Navigation;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace DbViewer.ViewModels
{
    public class HubDetailViewModel : NavigationViewModelBase, INavigatedAware
    {
        public const string HubIdNavParam = nameof(HubIdNavParam);
        private const string LastHubAddressKey = "LastHubAddress";

        private readonly IHubService _hubService;
        private readonly ILogger _logger = Log.ForContext<HubDetailViewModel>();

        public HubDetailViewModel(IHubService hubService, INavigationService navigationService)
            : base(navigationService)
        {
            _hubService = Guard
                .Argument(hubService, nameof(hubService))
                .NotNull()
                .Value;

            var canScanChanged = this.WhenAnyValue(x => x.IsScanning)
                .Select(isScanning => !isScanning)
                .ObserveOn(RxApp.MainThreadScheduler);

            var canDownloadChanged = this.WhenAnyValue(x => x.IsDownloading)
                .Select(isDownloading => !isDownloading)
                .ObserveOn(RxApp.MainThreadScheduler);

            var isNotBusyChanged = canScanChanged.CombineLatest(canDownloadChanged,
                resultSelector: (canScan, canDownload) => (canScan && canDownload));

            RescanCommand =
                ReactiveCommand.CreateFromTask(ExecuteRescanForAllDatabasesAsync, canExecute: canScanChanged);
            DownloadCheckedCommand =
                ReactiveCommand.CreateFromTask(ExecuteDownloadCheckedAsync, canExecute: isNotBusyChanged);

            ViewHubSetupCommand = ReactiveCommand.CreateFromTask(ExecuteViewHubSetupAsync);

            UpdateStatus(string.Empty);
        }

        public ReactiveCommand<Unit, Unit> RescanCommand { get; }

        public ReactiveCommand<Unit, Unit> DownloadCheckedCommand { get; }

        public ReactiveCommand<Unit, Unit> ViewHubSetupCommand { get; }

        public string HubAddress
        {
            get => _hubAddress;
            set => this.RaiseAndSetIfChanged(ref _hubAddress, value);
        }


        public string HubName
        {
            get => _hubName;
            set => this.RaiseAndSetIfChanged(ref _hubName, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set => this.RaiseAndSetIfChanged(ref _isScanning, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set => this.RaiseAndSetIfChanged(ref _isDownloading, value);
        }

        public ObservableCollection<RemoteDatabaseViewModel> RemoteDatabases
        {
            get => _remoteDatabases;
            set => this.RaiseAndSetIfChanged(ref _remoteDatabases, value);
        }

        public void OnNavigatedFrom(INavigationParameters parameters)
        {
        }

        public void OnNavigatedTo(INavigationParameters parameters)
        {
            if (parameters.ContainsKey(HubIdNavParam))
            {
                var hubId = parameters.GetValue<string>(HubIdNavParam);

                ReloadHubAsync(hubId, CancellationToken.None);
            }
        }

        private void UpdateStatus(string status)
        {
            RunOnUi(
                () => { Status = status; });
        }

        private void ReloadHubAsync(string hubId, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                try
                {
                    UpdateStatus("Loading...");

                    _hubInfo = await _hubService.GetCachedHubAsync(hubId, cancellationToken).ConfigureAwait(false);

                    RunOnUi(
                        () =>
                        {
                            HubName = _hubInfo.HubName;
                            HubAddress = _hubInfo.HostAddress;

                            UpdateStatus($"Loaded Hub");
                        });

                    cancellationToken.ThrowIfCancellationRequested();

                    await ExecuteRescanForAllDatabasesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, nameof(OnNavigatedTo));

                    UpdateStatus($"Error Occurred...");
                }
            });
        }

        private async Task ExecuteRescanForAllDatabasesAsync(CancellationToken cancellationToken)
        {
            UpdateStatus("Scanning...");

            IsScanning = true;

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(_hubInfo.HostAddress))
            {
                return;
            }

            var hubUri = new Uri(_hubInfo.HostAddress);

            Preferences.Set(LastHubAddressKey, _hubInfo.HostAddress);

            IEnumerable<DatabaseInfo> dbList = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                dbList = await _hubService.ListAllHubDatabasesAsync(hubUri, cancellationToken)
                    .ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                ClearAndUpdateUI(dbList);

                cancellationToken.ThrowIfCancellationRequested();

                UpdateStatus("Scan complete");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(ExecuteRescanForAllDatabasesAsync));

                UpdateStatus("Error scanning..");
            }

            IsScanning = false;
        }

        private void ClearAndUpdateUI(IEnumerable<DatabaseInfo> dbList)
        {
            if (dbList == null)
            {
                return;
            }

            var viewModels = dbList.Select(db => new RemoteDatabaseViewModel(db));

            RunOnUi(
                () =>
                {
                    RemoteDatabases.Clear();

                    foreach (var vm in viewModels)
                    {
                        RemoteDatabases.Add(vm);
                    }
                });
        }

        private Task ExecuteViewHubSetupAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var navParams = new NavigationParameters
            {
                {
                    nameof(HubSettingsViewModel.HubIdNavParam),
                    _hubInfo.Id
                }
            };

            return NavigationService.NavigateAsync(nameof(HubSettingsPage), navParams);
        }

        private async Task ExecuteDownloadCheckedAsync(CancellationToken cancellationToken)
        {
            try
            {
                var hubUri = new Uri(HubAddress);

                foreach (var vm in RemoteDatabases)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (vm.ShouldDownload)
                    {
                        await _hubService.DownloadDatabaseAsync(hubUri, vm.DatabaseInfo, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }


                RunOnUi(() =>
                {
                    NavigationService.NavigateAsync(nameof(CachedDatabaseListPage)).ConfigureAwait(false);
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(ExecuteRescanForAllDatabasesAsync));
            }
        }

        private HubInfo _hubInfo;
        private string _hubAddress;
        private string _hubName;
        private string _status;
        private bool _isScanning;
        private bool _isDownloading;

        private ObservableCollection<RemoteDatabaseViewModel> _remoteDatabases =
            new ObservableCollection<RemoteDatabaseViewModel>();
    }
}