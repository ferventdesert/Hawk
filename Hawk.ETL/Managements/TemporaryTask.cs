using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;

namespace Hawk.ETL.Managements
{
    public class TemporaryTask : TaskBase
    {
        public TemporaryTask()
        {
            AutoDelete = true;
        }

        private Task CurrentTask;

        public Action TaskAction { get; set; }
        public static TemporaryTask AddTempTask<T>(string taskName, IEnumerable<T> enumable, Action<T> action,
           Action<int> contineAction = null, int count = -1, bool autoStart = true, int notifyInterval = 1, Func<int>delayFunc=null )
        {
            var tempTask = new TemporaryTask { Name = taskName };

            var index = 0;
            if (notifyInterval <= 0)
                notifyInterval = 1;
            tempTask.TaskAction = () =>
            {
                if (enumable is ICollection<T>)
                {
                    count = ((ICollection<T>)enumable).Count;
                }

                var finish = true;
                foreach (var r in enumable)
                {
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
                        finish = false;
                        break;
                    }
                    if(delayFunc!=null)
                        Thread.Sleep(delayFunc());
                    if (count >0)
                    {
                        tempTask.Percent = tempTask.CurrentIndex * 100 / count;
                    }
                    tempTask.CurrentIndex = index;
                    tempTask.CheckWait();
                }
                if (finish)
                    tempTask.Percent = 100;
                if (contineAction != null)
                {
                    ControlExtended.UIInvoke(() => contineAction(tempTask.CurrentIndex));
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
                CurrentTask=new Task(() =>
                {
                    try
                    {
                        TaskAction();
                    }
                    catch (Exception ex)
                    {
                        
                        XLogSys.Print.Error("任务已经出错："+ex);
                        IsStart = false;
                    }
                  
                    IsStart = false;
                    if (AutoDelete)
                        Remove();
                });
                CurrentTask.Start();
            }
        }


        public void Wait()
        {
            CurrentTask.Wait();
        }
    }
}