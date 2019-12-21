using System.Globalization;
using System.Windows;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;


namespace Hawk
{
    /// <summary>
    ///     App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static void SetCountryCode()
        {
            // This fallback country code does not reflect the physical device location, but rather the
            // country that corresponds to the culture it uses.
            var countryCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            AppCenter.SetCountryCode(countryCode);
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SetCountryCode();
            AppCenter.Start("1a85d57d-ef2e-4267-83c7-cdcb19a1392d",
                typeof(Analytics), typeof(Crashes));
            bool didAppCrash = await Crashes.HasCrashedInLastSessionAsync();
            if (didAppCrash)
            {
                ErrorReport crashReport = await Crashes.GetLastSessionCrashReportAsync();
            }

            Crashes.ShouldProcessErrorReport = (ErrorReport report) => {
                return true; // return true if the crash report should be processed, otherwise false.
            };

            //AppHelper.LoadLanguage();
            //TODO: instance is null
        }
    }
}