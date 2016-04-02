using System;
using System.Threading;
using log4net;

namespace Hawk.Core.Utils.Logs
{
    public enum LogType
    {
        Debug,
        Info,
        Important,
   
        Vital,
     

    }
    /// <summary>
    /// 单例适配器实现的调试系统
    /// </summary>
  public class XLogSys
    {
        #region Constants and Fields

      

        private readonly ILog thisXLog = LogManager.GetLogger("thisXLog");

        private static XLogSys print;

        #endregion

        #region Properties

        /// <summary>
        /// 输出提供的单例
        /// </summary>
        public static XLogSys Print
        {
            get
            {
                return print ?? (print = new XLogSys());
            }
        }

        #endregion

        #region Implemented Interfaces

        #region IXFrmWorkLog

        public void Debug(object message)
        {
            this.thisXLog.Debug(message);
        }

        public void Debug(object message, Exception exception)
        {
            this.thisXLog.Debug(message, exception);
        }

        public void Error(object message)
        {
            this.thisXLog.Error(message);
        }

        public void Error(object message, Exception exception)
        {
            this.thisXLog.Error(message, exception);
        }
        public void ErrorFormat(string message, params object[] paras)
        {
            this.thisXLog.Error(string.Format(message, paras));
        }

        public void Fatal(object message, Exception exception)
        {
            this.thisXLog.Fatal(message, exception);
        }

        public void Fatal(object message)
        {
            this.thisXLog.Fatal(message);
        }

        private readonly object lockobj = new object();
        public void Info(object message)
        {
            bool lockTaken = false;

            Monitor.TryEnter(this.lockobj, 300, ref lockTaken);
            if (lockTaken)
            {
                try
                {

                     this.thisXLog.Info(message);
                }
                finally
                {
                    Monitor.Exit(this.lockobj);
                }
            }
            else
            {
                return;
            }
        }

        public void Info(object message, Exception exception)
        {
            this.thisXLog.Info(message, exception);
        }

        public void InfoFormat(string message,params object[] items)
        {
            this.thisXLog.Info(string.Format(message,items));
        }
        public void WarnFormat(string message, params object[] items)
        {
            this.thisXLog.Warn(string.Format(message, items));
        }
        public void Warn(object message)
        {
            this.thisXLog.Warn(message);
        }

        public void Warn(object message, Exception exception)
        {
            this.thisXLog.Warn(message, exception);
        }

        #endregion

        #endregion
    }
}