using System.Collections;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace Hawk.Core.Utils.Logs
{
    /// <summary>
    /// Description of RichTextBoxAppender.
    /// </summary>
    public class RichTextBoxAppender : AppenderSkeleton
    {
        #region Constants and Fields

        private readonly object _lock = new object();

        private LoggingEvent rcloggingevent;

        #endregion

        #region Properties


        private StatusBar statusBar;

        private RichTextBox RichTextBox;

        #endregion

        #region Public Methods

        public static void SetRichTextBox(RichTextBox rtb, StatusBar block = null)
        {
            rtb.IsReadOnly = true;

            foreach (IAppender appender in
                GetAppenders())
            {
                var richTextBoxAppender = appender as RichTextBoxAppender;
                if (richTextBoxAppender != null)
                {
                    richTextBoxAppender.RichTextBox = rtb;
                    richTextBoxAppender.statusBar = block;
                }
            }
        }

        #endregion

        #region Methods

        protected override void Append(LoggingEvent loggingevent)
        {
            lock (this._lock)
            {
                this.rcloggingevent = loggingevent;

                ControlExtended.UIBeginInvoke(this.WriteRichTextBox);

            }
        }

        private static IAppender[] GetAppenders()
        {
            var appenders = new ArrayList();

            appenders.AddRange(((Hierarchy)LogManager.GetRepository()).Root.Appenders);

            foreach (ILog log in LogManager.GetCurrentLoggers())
            {
                appenders.AddRange(((Logger)log.Logger).Appenders);
            }

            return (IAppender[])appenders.ToArray(typeof(IAppender));
        }



        private void WriteRichTextBox()
        {
            var writer = new StringWriter();

            this.Layout.Format(writer, this.rcloggingevent);

            var rc = new Run(writer.ToString());

            switch (this.rcloggingevent.Level.ToString())
            {
                case "INFO":

                    break;
                case "WARN":
                    rc.Foreground = Brushes.Yellow;
                    break;
                case "ERROR":
                    rc.Foreground = Brushes.Orange;
                    break;
                case "FATAL":
                    rc.Foreground = Brushes.DarkOrange;
                    break;
                case "DEBUG":
                    rc.Foreground = Brushes.DarkGreen;
                    break;
                default:
                    rc.Foreground = Brushes.White;
                    break;
            }
            if (statusBar != null)
            {
                var bar = this.statusBar.Items.GetItemAt(0) as StatusBarItem;
                if (bar != null)
                {
                    bar.Content = rc.Text;
                }
            }
            if(RichTextBox==null)
                return;
            var rc2 = this.RichTextBox.Document.Blocks.ElementAt(0) as Paragraph;
            rc2.Inlines.Add(rc);
            this.RichTextBox.ScrollToEnd();

        }

        #endregion
    }
}
