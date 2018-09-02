using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Microsoft.Scripting.Utils;

namespace Hawk.ETL.Managements
{
    public class TemporaryTask<T> : TaskBase,IDictionarySerializable
    {
        private Task CurrentTask;
        private int _currentPos;

        public TemporaryTask()
        {
            AutoDelete = true;
            WasCanceled = false;
            WasAborted = false;
            Level = 0;
        }
        [Browsable(false)]
        public bool IsFormal { get; set; }
        private bool WasAborted { get; set; }
        private bool WasCanceled { get; set; }
        public Action TaskAction { get; set; }

        public int CurrentPos
        {
            get { return _currentPos; }
            set
            {
                if (_currentPos != value)
                {
                    _currentPos = value;
                    OnPropertyChanged("CurrentPos");
                }
            }
        }

        public IList<FreeDocument> Seeds { get; set; }

        /// <summary>
        /// 指代任务的层级，是主任务还是附属任务
        /// </summary>
        public int Level { get; set; }

        public static TemporaryTask<T> AddTempTaskSimple<T>(string taskName, IEnumerable<T> enumable, Action<T> action,
            Action<int> continueAction = null, int count = -1, bool autoStart = true, int notifyInterval = 1,
            Func<int> delayFunc = null) where  T:IDictionarySerializable
        {
            var tempTask = new TemporaryTask<T> {Name = taskName};
            return AddTempTaskSimple<T>(tempTask, enumable, action,continueAction, count, autoStart, notifyInterval, delayFunc);
        }
        public static TemporaryTask<T> AddTempTaskSimple<T>(TemporaryTask<T> tempTask , IEnumerable<T> enumable, Action<T> action,
           Action<int> continueAction = null, int count = -1, bool autoStart = true, int notifyInterval = 1,
           Func<int> delayFunc = null) where T : IDictionarySerializable
        {
            // start a task with a means to do a hard abort (unsafe!)
            if (enumable is IList)
            {
                tempTask.Seeds = enumable.Select(d => d.DictSerialize()).ToList();
            }
            var index = 0;
            if (notifyInterval <= 0)
                notifyInterval = 1;
            tempTask.TaskAction = () =>
            {
                if (enumable is ICollection<T>)
                {
                    count = ((ICollection<T>)enumable).Count;
                }

                foreach (var r in enumable)

                {

                    tempTask.CheckWait();
                    action?.Invoke(r);

                    if (r is int)
                    {
                        index = Convert.ToInt32(r);
                    }
                    else
                    {
                        index++;
                    }

                    if (index % notifyInterval != 0) continue;
                    if (tempTask.CheckCancel())
                    {
                        tempTask.WasCanceled = true;
                        break;
                    }
                    if (delayFunc != null)
                        Thread.Sleep(delayFunc());
                    if (count > 0)
                    {
                        tempTask.Percent = tempTask.CurrentIndex * 100 / count;
                    }
                    tempTask.CurrentIndex = index;
              
                }
                if (!tempTask.WasCanceled)
                {
                    XLogSys.Print.Debug(string.Format(GlobalHelper.Get("key_338"), tempTask.Name));
                    tempTask.Percent = 100;
                }
                if (continueAction != null)
                {
                    ControlExtended.UIInvoke(() => continueAction(tempTask.CurrentIndex));
                }
            };

            if (autoStart)
            {
                tempTask.Start();
            }
            return tempTask;
        }
        public static TemporaryTask<T> AddTempTask<T>(string taskName, IEnumerable<T> source,
            Func<T, IEnumerable<T>> action,
            Action<int> continueAction = null, int count = -1, bool autoStart = true, int notifyInterval = 1,
            Func<int> delayFunc = null) where  T:IDictionarySerializable
        {
            var tempTask = new TemporaryTask<T> {Name = taskName};
            // start a task with a means to do a hard abort (unsafe!)
            if (source is IList)
            {
                tempTask.Seeds = source.Select(d => d.DictSerialize()).ToList();
            }
            var index = 0;
            if (notifyInterval <= 0)
                notifyInterval = 1;
            tempTask.TaskAction = () =>
            {
                if (source is ICollection<T>)
                {
                    count = ((ICollection<T>) source).Count;
                }

                foreach (var r in  source)
                {
                    tempTask.CheckWait();
                    if (r is int)
                    {
                        index = Convert.ToInt32(r);
                    }
                    else
                    {
                        index++;
                    }
                    foreach (var  item in  action!=null?action(r):new List<T>() {r})
                    {
                      
                        tempTask.CheckWait();
                        if (index % notifyInterval != 0) continue;
                        if (tempTask.CheckCancel())
                        {
                            tempTask.WasCanceled = true;
                            break;
                        }
                      
                        tempTask.CurrentIndex++;
                    }
                    if (delayFunc != null)
                        Thread.Sleep(delayFunc());
                    if (count > 0)
                    {
                        tempTask.Percent = tempTask.CurrentIndex*100/count;
                    }
                    tempTask.CurrentPos = index;
                }
                if (!tempTask.WasCanceled)
                {
                    XLogSys.Print.Debug(string.Format(GlobalHelper.Get("key_338"), tempTask.Name));
                    tempTask.Percent = 100;
                }
                if (continueAction != null)
                {
                    ControlExtended.UIInvoke(() => continueAction(tempTask.CurrentIndex));
                }
            };

            if (autoStart)
            {
                tempTask.Start();
            }
            return tempTask;
        }

        public override void Start()
        {
            base.Start();
            if (TaskAction != null)
            {
                IsStart = true;
                CurrentTask = new Task(() =>
                {
                    try
                    {
                        //Capture the thread
                        var thread = Thread.CurrentThread;
                        using (CancellationToken.Token.Register(() =>
                        {
                            Task.Factory.StartNew(() =>
                            {
                                for (var i = 0; i < 10; i++)
                                {
                                    Thread.Sleep(100);
                                    if (WasCanceled)
                                        break;
                                }
                                if (WasCanceled == false)
                                    thread.Abort();
                            });
                        }))
                        {
                            TaskAction();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        XLogSys.Print.Warn(GlobalHelper.Get("key_339"));
                        IsStart = false;
                        WasAborted = true;
                    }
                    catch (Exception ex)
                    {
                        XLogSys.Print.Error(GlobalHelper.Get("key_340") + ex);
                        IsStart = false;
                    }

                    IsStart = false;
                    if (AutoDelete)
                        Remove();
                }, CancellationToken.Token);
                CurrentTask.Start();
            }
        }

        public void Wait()
        {
            CurrentTask.Wait();
        }
        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var freedoc = new FreeDocument {{ "CurrentPos", CurrentPos }, {"Name  ", Name}, {"Level", Level}};
            if (Seeds == null) return freedoc;
            var seed = new FreeDocument
            {
                Children = Seeds.OfType<IFreeDocument>().Select(d => d.DictSerialize()).ToList()
            };
            freedoc.Add("Seeds", seed);
            return freedoc;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Name = docu.Set("Name", Name);
            CurrentPos = docu.Set("CurrentPos", CurrentPos);
            Level = docu.Set("Level", Level);
            var items = docu["Seeds"] as FreeDocument;

            if (items?.Children == null) return;
            foreach (var item in items.Children)
            {
                Seeds.Add(item);
            }
        }
    }
}