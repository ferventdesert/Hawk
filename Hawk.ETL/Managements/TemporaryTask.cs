using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Process;

namespace Hawk.ETL.Managements
{
    public class TemporaryTask<T> : TaskBase, IDictionarySerializable
    {
        private int _mapperIndex1;
        private int _mapperIndex2;
        private Task CurrentTask;

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
        [Browsable(false)]
        public Action TaskAction { get; set; }

        public int MapperIndex1
        {
            get { return _mapperIndex1; }
            set
            {
                if (_mapperIndex1 != value)
                {
                    _mapperIndex1 = value;
                    OnPropertyChanged("MapperIndex1");
                }
            }
        }
        public int MapperIndex2
        {
            get { return _mapperIndex2; }
            set
            {
                if (_mapperIndex2 != value)
                {
                    _mapperIndex2 = value;
                    OnPropertyChanged("MapperIndex2");
                }
            }
        }


        [Browsable(false)]
        public IList<IFreeDocument> Seeds { get; set; }

        /// <summary>
        ///     指代任务的层级，是主任务还是附属任务
        /// </summary>
        public int Level { get; set; }

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var freedoc = new FreeDocument {{"MapperIndex1", MapperIndex1}, { "MapperIndex2", MapperIndex2 } ,{ "OutputIndex", OutputIndex }, { "Name", Name}, {"Level", Level}};
            var tool = Publisher as SmartETLTool;
            if (tool != null)
            {
                freedoc.Add("Publisher", tool.Name);
                freedoc.Add("GenerateMode", tool.GenerateMode);
            }

            if (Seeds == null) return freedoc;
            var seed = new FreeDocument
            {
                Children = Seeds.Select(d => d.DictSerialize()).ToList()
            };
            freedoc.Add("Seeds", seed);
            return freedoc;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Name = docu.Set("Name", Name);
            MapperIndex1 = docu.Set("MapperIndex1", MapperIndex1);
            MapperIndex2 = docu.Set("MapperIndex2", MapperIndex2);
            OutputIndex = docu.Set("OutputIndex", OutputIndex);
            Level = docu.Set("Level", Level);
            Publisher = docu.Set("Publisher", Publisher);
            var items = docu["Seeds"] as FreeDocument;

            if (items?.Children == null) return;
            if(Seeds==null)
                Seeds=new List<IFreeDocument>();
            foreach (var item in items.Children)
                Seeds.Add(item);
        }

        public static TemporaryTask<T> AddTempTaskSimple<T>(string taskName, IEnumerable<T> enumable, Action<T> action,
            Action<int> continueAction = null, int count = -1, bool autoStart = true, int notifyInterval = 1,
            Func<int> delayFunc = null) where T : IDictionarySerializable
        {
            var tempTask = new TemporaryTask<T> {Name = taskName};
            return AddTempTaskSimple(tempTask, enumable, action, continueAction, count, autoStart, notifyInterval,
                delayFunc);
        }

        public static TemporaryTask<T> AddTempTaskSimple<T>(TemporaryTask<T> tempTask, IEnumerable<T> enumable,
            Action<T> action,
            Action<int> continueAction = null, int count = -1, bool autoStart = true, int notifyInterval = 1,
            Func<int> delayFunc = null) where T : IDictionarySerializable
        {
            return AddTempTask<T>(tempTask, enumable, d =>
            {
                action?.Invoke(d);
                return new List<T>() {d};
            }, continueAction, count, autoStart, notifyInterval, delayFunc);
        }

        public static TemporaryTask<T> AddTempTask<T>(TemporaryTask<T> tempTask, IEnumerable<T> source,
            Func<T, IEnumerable<T>> func,
            Action<int> continueAction = null, int count = -1, bool autoStart = true, int notifyInterval = 1,
            Func<int> delayFunc = null) where T : IDictionarySerializable

        {
          
            if (notifyInterval <= 0)
                notifyInterval = 1;
            tempTask.Total = count;
            tempTask.TaskAction = () =>
            {
                if (source is ICollection<T>)
                    count = ((ICollection<T>)source).Count;
                tempTask.CheckWait();
                foreach (var r in source)
                {
                    if (tempTask.CheckCancel())
                    {
                        tempTask.WasCanceled = true;
                        break;
                    }
                    foreach (var item in func != null ? func(r) : new List<T> { r })
                    {
                        tempTask.CheckWait();
                   
                        if (tempTask.CheckCancel())
                        {
                            tempTask.WasCanceled = true;
                            break;
                        }
                        if (tempTask.OutputIndex % notifyInterval != 0) continue;
                        tempTask.OutputIndex++;
                  
                    }
                    if (delayFunc != null)
                        Thread.Sleep(delayFunc());
                    if (tempTask.OutputIndex % notifyInterval != 0) continue;
                    if (count > 0)
                        tempTask.Percent = tempTask.OutputIndex * 100 / count;
               
                }
                if (!tempTask.WasCanceled)
                {
                    XLogSys.Print.Debug(string.Format(GlobalHelper.Get("key_338"), tempTask.Name));
                    tempTask.Percent = 100;
                }
                if (continueAction != null)
                    ControlExtended.UIInvoke(() => continueAction(tempTask.OutputIndex));
            };

            if (autoStart)
                tempTask.Start();
            return tempTask;
        }
        public static TemporaryTask<T> AddTempTask<T>(string taskName, IEnumerable<T> source,
            Func<T, IEnumerable<T>> action,
            Action<int> continueAction = null, int count = -1, bool autoStart = true, int notifyInterval = 1,
            Func<int> delayFunc = null) where T : IDictionarySerializable
        {
            var tempTask = new TemporaryTask<T> {Name = taskName};
            // start a task with a means to do a hard abort (unsafe!)
            return AddTempTask<T>(tempTask, source, action, continueAction, count, autoStart, notifyInterval,
                delayFunc);
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
                        //ControlExtended.UIInvoke(()=>this.ContinueAction?.Invoke(this.CurrentIndex));
                        WasAborted = true;
                    
                    }
                    catch (Exception ex)
                    {
                        XLogSys.Print.Error(GlobalHelper.Get("key_340") + ex.Message);
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
    }
}