using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Microsoft.HockeyApp;
namespace Hawk
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Microsoft.HockeyApp.HockeyClient.Current.Configure("2b23e2e4a420438dbfb308d5ddc7d448")
            .SetContactInfo("Desert", "buptzym@qq.com");


            //send crashes to the HockeyApp server
            await HockeyClient.Current.SendCrashesAsync();
            await HockeyClient.Current.CheckForUpdatesAsync(true, () =>
            {
                if (Application.Current.MainWindow != null) { Application.Current.MainWindow.Close(); }
                return true;
            });


            // LoadLanguage();
        }




    }
}
