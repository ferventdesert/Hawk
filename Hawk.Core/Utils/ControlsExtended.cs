using System;
using System.Collections.Generic;
using Hawk.Core.Utils;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;

using Microsoft.Win32;

namespace Hawk.Core.Utils
{
    public enum FileOperate
    {
        Save,
        Read
    }

    //扩展下所有Control类，把线程操作Invoke提出来。 
    public static class ControlExtended
    {
        #region Delegates

        public delegate void InvokeHandler();

        #endregion

        #region Public Methods

        public static IDockableManager DockableManager
        {
            get
            {
                var item = Application.Current.MainWindow as IDockableManager;
                return item;
            }
        }

        public static bool UserCheck(string message, string title = null)

        {
            if (title == null)
                title = GlobalHelper.Get("key_99");
            return MessageBox.Show(message, title, MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes;
        }

        public static void RemoveContentWithName(this IDockableManager manager, string name)
        {
            var m = manager.ViewDictionary.FirstOrDefault(d => d.Name == name);
            if (m != null)
                manager.RemoveDockableContent(m.View);
        }

        public static void AddDockAbleContent(this IDockableManager manager, FrmState thisState, object model,
            object thisControl, params string[] objects)
        {
            manager.AddDockAbleContent(thisState, thisControl, objects);
            manager.ViewDictionary.Last().Model = model;
        }

        public static void RemoveDockAbleContent(this IDockableManager manager, object model)
        {
            manager.RemoveDockableContent(model);
        }

        public static void ActiveModelContent(this IDockableManager manager, object model)
        {
            var view = manager.ViewDictionary.FirstOrDefault(d => d.Model == model);
            if (view == null)
                return;
          
            manager.ActiveThisContent(view.View);
        }

        public static bool SafeCheck(this bool check, string name, LogType type = LogType.Info)
        {
            var res = check;
            if (res)
            {
                if (type > LogType.Important)
                {
                    XLogSys.Print.Info(name + GlobalHelper.Get("key_97"));
                }
            }
            else
            {
                name = name + GlobalHelper.Get("key_98");
                if (type > LogType.Debug)
                {
                    XLogSys.Print.Warn(name);
                }
                if (type > LogType.Info)
                {
                    UIInvoke(() => { MessageBox.Show(name, GlobalHelper.Get("key_99")); });
                }
            }
            return res;
        }


        public static void SafeInvoke(this Action action, LogType type = LogType.Info, string name = null,
            bool isui = false)
        {
            if (name == null)
            {
                name = GlobalHelper.Get("key_100");
            }
            try
            {
                if (isui)
                    UIInvoke(action);
                else
                {
                    action();
                }

                var str = name + GlobalHelper.Get("key_101");
                if (type >= LogType.Important)
                {
                    XLogSys.Print.Info(str);
                }
                if (type >= LogType.Vital)
                {
                    UIInvoke(() => { MessageBox.Show(str, GlobalHelper.Get("key_99")); });
                }
            }
            catch (Exception ex)
            {
                var str = name + GlobalHelper.Get("key_102");
                var dict=new Dictionary<string,string>();
                dict.Add("key", str);
                //(HockeyClient.Current as HockeyClient).HandleException(ex);
                switch (type)
                {
                    case LogType.Debug:
                        XLogSys.Print.WarnFormat(str, ex.Message);
                        break;
                    case LogType.Info:
                        XLogSys.Print.ErrorFormat(str, ex.ToString());
                        break;
                    case LogType.Important:
                        XLogSys.Print.ErrorFormat(str, ex.ToString());
                        UIInvoke(() => { MessageBox.Show(string.Format(str, ex.Message), GlobalHelper.Get("error_message")); });
                        break;
                    case LogType.Vital:
                        XLogSys.Print.Fatal(str, ex);
                        UIInvoke(() => { MessageBox.Show(string.Format(str, ex), GlobalHelper.Get("error_message")); });
                        break;
                }
            }
        }

        public static bool SafeInvoke<T>(Func<T> action, ref T result, LogType type = LogType.Info, string name = null,
            bool isUIAction = false)
        {
            if (name == null)
            {
                name = GlobalHelper.Get("key_100");
            }
            try
            {
                var res = isUIAction == false ? action() : UIInvoke(action);
                var str = name + GlobalHelper.Get("key_101");
                if (type >= LogType.Important)
                {
                    XLogSys.Print.Info(str);
                }
                if (type >= LogType.Vital)
                {
                    UIInvoke(() => { MessageBox.Show(str, GlobalHelper.Get("key_99")); });
                }
                result = res;
                return true;
            }
            catch (Exception ex)
            {
                var str = name + GlobalHelper.Get("key_102");
                switch (type)
                {
                    case LogType.Debug:
                        XLogSys.Print.WarnFormat(str, ex.Message);
                        break;
                    case LogType.Info:
                        XLogSys.Print.ErrorFormat(str, ex.ToString());
                        break;
                    case LogType.Important:
                        XLogSys.Print.ErrorFormat(str, ex.ToString());
                        UIInvoke(() => { MessageBox.Show(string.Format(str, ex.Message), GlobalHelper.Get("error_message")); });
                        break;
                    case LogType.Vital:
                        XLogSys.Print.Fatal(str, ex);
                        UIInvoke(() => { MessageBox.Show(string.Format(str, ex), GlobalHelper.Get("error_message")); });
                        break;
                }
            }
            return false;
        }


        public static bool CheckFilePath(this IFileConnector connector, FileOperate readOrWrite)
        {
            if (connector.FileName == null && MainDescription.IsUIForm)
            {
                if (readOrWrite == FileOperate.Read)
                {
                    var ofd = new OpenFileDialog();

                    ofd.DefaultExt = connector.ExtentFileName;
                    ofd.Filter = String.Join("|", connector.ExtentFileName.Split(' ').Select(d => string.Format("(*{0})|*{0}", d)));

                    if (ofd.ShowDialog() == true)
                    {
                        connector.FileName = ofd.FileName;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var ofd = new SaveFileDialog();
                    ofd.FileName = connector.FileName;
                    ofd.DefaultExt = connector.ExtentFileName.Split(' ')[0];
                    ofd.Filter = String.Join("|",  connector.ExtentFileName.Split(' ').Select(d=> string.Format("(*{0})|*{0}",d)));

                    if (ofd.ShowDialog() == true)
                    {
                        connector.FileName = ofd.FileName;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
          

            return true;
        }

        public static void SetBusy(ProgressBarState state , string title = null, string message = null, int percent = 0)
        {
            if (title == null)
                title = GlobalHelper.Get("key_3");
            if (message == null)
                message = GlobalHelper.Get("LongTask");
            if (Application.Current == null)
                return;
            ControlExtended.UIInvoke(() =>
            {
                var item = Application.Current.MainWindow as IDockableManager;
                if (item == null)
                    return;
                UIInvoke(() => item.SetBusy(state, title, message, percent));
            });
          
        }

        public static void UIInvoke(this Control control, InvokeHandler handler)
        {
            if (!control.Dispatcher.CheckAccess())
            {
                control.Dispatcher.Invoke(handler);
            }
            else
            {
                handler();
            }
        }

        public static void UIBeginInvoke(this Control control, InvokeHandler handler)
        {
            if (!control.Dispatcher.CheckAccess())
            {
                control.Dispatcher.BeginInvoke(handler);
            }
            else
            {
                handler();
            }
        }


        public static T UIInvoke<T>(Func<T> handler)
        {
            if (Application.Current == null)
            {
                if (MainDescription.IsUIForm == false)
                    return handler();
                return default(T);
                ;
            }
            var dispatcher = Application.Current.Dispatcher;
            if (dispatcher == null)
                return default(T);
            if (!dispatcher.CheckAccess())
            {
                return (T) dispatcher.Invoke(handler);
            }
            return handler.Invoke();
        }

        public static void UIInvoke(Action handler)
        {
            if (Application.Current == null)
                return;
            var dispatcher = Application.Current.Dispatcher;
            if (dispatcher == null)
                return;
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(handler);
            }
            else
            {
                handler.Invoke();
            }
        }

        public static void UIBeginInvoke(Action handler)
        {
            if (Application.Current == null)
                return;
            var dispatcher = Application.Current.Dispatcher;

            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(handler);
            }
            else
            {
                handler.Invoke();
            }
        }

        #endregion
    }
}