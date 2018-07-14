using System;
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
using ToastNotifications;
using ToastNotifications.Messages;

namespace Hawk.Core.Utils.Logs
{
    /// <summary>
    ///     Description of RichTextBoxAppender.
    /// </summary>
    public class RichTextBoxAppender : AppenderSkeleton
    {
        #region Public Methods

        public static void SetRichTextBox(RichTextBox rtb, StatusBar block = null, Notifier notifier = null)
        {
            rtb.IsReadOnly = true;

            foreach (var appender in
                GetAppenders())
            {
                var richTextBoxAppender = appender as RichTextBoxAppender;
                if (richTextBoxAppender != null)
                {
                    richTextBoxAppender.RichTextBox = rtb;
                    richTextBoxAppender.statusBar = block;
                    richTextBoxAppender.notifier = notifier;
                }
            }
        }

        #endregion

        #region Constants and Fields

        private readonly object _lock = new object();

        private LoggingEvent rcloggingevent;

        #endregion

        #region Properties

        private StatusBar statusBar;

        private RichTextBox RichTextBox;
        private Notifier notifier;

        #endregion

        #region Methods

        protected override void Append(LoggingEvent loggingevent)
        {
            lock (_lock)
            {
                rcloggingevent = loggingevent;

                ControlExtended.UIBeginInvoke(WriteRichTextBox);
            }
        }

        private string lastString;
        private DateTime lastTime;
        private int lastSameCount;

        private static IAppender[] GetAppenders()
        {
            var appenders = new ArrayList();

            appenders.AddRange(((Hierarchy) LogManager.GetRepository()).Root.Appenders);

            foreach (var log in LogManager.GetCurrentLoggers())
                appenders.AddRange(((Logger) log.Logger).Appenders);

            return (IAppender[]) appenders.ToArray(typeof (IAppender));
        }


        private void WriteRichTextBox()
        {
            var writer = new StringWriter();

            Layout.Format(writer, rcloggingevent);

            var rc = new Run(writer.ToString());
            var message = writer.ToString().Split('\n')[0];
            if (lastString == message)
            {
                lastSameCount++;
                if (lastSameCount >= 4 && DateTime.Now - lastTime < TimeSpan.FromSeconds(5))
                {
                    lastTime = DateTime.Now;
                    return;
                }
                lastTime = DateTime.Now;
            }
            else
            {
                lastString = message;
                lastSameCount = 0;
            }
            switch (rcloggingevent.Level.ToString())
            {
                case "INFO":

                    break;
                case "WARN":
                    notifier?.ShowWarning(message);
                    rc.Foreground = Brushes.Yellow;
                    break;
                case "ERROR":
                    notifier?.ShowError(message);
                    rc.Foreground = Brushes.Orange;
                    break;
                case "FATAL":
                    notifier?.ShowError(message);
                    rc.Foreground = Brushes.DarkOrange;
                    break;
                case "DEBUG":
                    rc.Foreground = Brushes.DarkGreen;
                    break;
                default:
                    rc.Foreground = Brushes.White;
                    break;
            }
            var bar = statusBar?.Items.GetItemAt(0) as StatusBarItem;
            if (bar != null)
                bar.Content = rc.Text;
            if (RichTextBox == null)
                return;
            var rc2 = RichTextBox.Document.Blocks.ElementAt(0) as Paragraph;
            rc2.Inlines.Add(rc);
            RichTextBox.ScrollToEnd();
        }

        #endregion
    }
}