using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;

namespace DbViewer.Droid
{
    [Activity(Label = "DbViewer", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            InitializePlugins(savedInstanceState);

            Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            if (Prism.PrismApplicationBase.Current.Resources.TryGetValue("ColoredBackgroundMediumColor", out object mainColorObj))
            {
                var color = (Xamarin.Forms.Color)mainColorObj;
                Window.SetStatusBarColor(Android.Graphics.Color.ParseColor(color.ToHex()));
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void InitializePlugins(Bundle savedInstanceState)
        {
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Akavache.Registrations.Start(nameof(DbViewer));
            Couchbase.Lite.Support.Droid.Activate(this);

            Rg.Plugins.Popup.Popup.Init(this);
        }
    }
}