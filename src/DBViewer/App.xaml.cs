using Akavache;
using DbViewer.DataStores;
using DbViewer.Dialogs;
using DbViewer.Repos;
using DbViewer.Services;
using DbViewer.ViewModels;
using DbViewer.Views;
using Prism.Ioc;
using Prism.Plugin.Popups;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Essentials.Implementation;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;

namespace DbViewer
{
    public partial class App
    {
        public App()
        {
            InitializeComponent();

            var logger = new LoggerConfiguration()
                        //.WriteTo.Console(Serilog.Events.LogEventLevel.Verbose)
                        .CreateLogger();

            Log.Logger = logger;

            Application.Current.UserAppTheme = OSAppTheme.Light;
        }

        protected override void OnStart()
        {
        }
        
        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }

        protected override async void OnInitialized()
        {
            var result = await NavigationService.NavigateAsync(nameof(CachedDatabaseListPage));

            if (!result.Success)
            {
                System.Diagnostics.Debugger.Break();
            }

            BlobCache.ApplicationName = AppInfo.Name;
            BlobCache.EnsureInitialized();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IHubService,HubService>();
            containerRegistry.RegisterSingleton<IDatabaseDatastore, DatabaseDatastore>();
            containerRegistry.RegisterSingleton<IHubDatastore, HubDatastore>();
            containerRegistry.RegisterSingleton<IHubRepo, HubRepo>();
            containerRegistry.RegisterSingleton<IPreferences, PreferencesImplementation>();

            containerRegistry.RegisterPopupDialogService();

            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<CachedDatabaseListPage, CachedDatabaseListViewModel>();
            containerRegistry.RegisterForNavigation<DatabaseBrowserPage, DatabaseBrowserViewModel>();
            containerRegistry.RegisterForNavigation<DatabaseSearchPage, DatabaseSearchViewModel>();
            containerRegistry.RegisterForNavigation<DocumentViewerPage, DocumentViewerViewModel>();
            containerRegistry.RegisterForNavigation<HubListPage, HubListViewModel>();
            containerRegistry.RegisterForNavigation<HubDetailPage, HubDetailViewModel>();
            containerRegistry.RegisterForNavigation<HubSettingsPage, HubSettingsViewModel>();
            containerRegistry.RegisterForNavigation<ServiceSettingsPage, ServiceSettingsViewModel>();

            containerRegistry.RegisterDialog<GeneralMessageDialog, GeneralMessageDialogViewModel>(DialogNames.General);
        }
    }
}