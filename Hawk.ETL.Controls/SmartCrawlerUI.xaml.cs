using System;
using System.Windows.Controls;
using Hawk.Core.Utils.Plugins;
using WebBrowser = System.Windows.Forms.WebBrowser;

namespace Hawk.ETL.Controls
{
    /// <summary>
    ///     SmartCrawlerUI.xaml 的交互逻辑
    /// </summary>
    [XFrmWork("网页采集器")]
    public partial class SmartCrawlerUI : UserControl, ICustomView
    {
        private readonly WebBrowser browser;

        public SmartCrawlerUI()
        {
            InitializeComponent();
            browser = new WebBrowser();
            browser.ScriptErrorsSuppressed = true;
            windowsFormsHost.Child = browser;
        }

        public FrmState FrmState => FrmState.Large;

        public void UpdateHtml(string html)
        {
            browser.Navigate("about:blank");
            try
            {
                if (browser.Document != null)
                {
                    browser.Document.Write(string.Empty);
                }
            }
            catch (Exception e)
            {
            } // do nothing with this
            browser.DocumentText = html;
        }
    }
}