using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Managements
{
    public abstract class TaskBase : PropertyChangeNotifier
    {
        private bool _isPause;
        private bool _isStart;
        private AutoResetEvent autoReset;
        private int currentIndex;
        private string name;
        private int _percent;

        protected TaskBase()
        {
            autoReset = new AutoResetEvent(false);
            ProcessManager = MainDescription.MainFrm.PluginDictionary["模块管理"] as IProcessManager;

           
        }
        /// <summary>
        /// 任务发布者
        /// </summary>
        [Browsable(false)]
        public object Publisher { get; set; }
        /// <summary>
        /// 当任务结束后，自动取消任务
        /// </summary>
        [Browsable(false)]
        public bool AutoDelete { get; set; }

        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }
        public bool CheckWait()
        {
            if (!IsPause) return false;
            autoReset.WaitOne();
            return true;
        }
        [Category("遍历状态")]
        [DisplayName("当前位置")]
        public int CurrentIndex
        {
            get { return currentIndex; }
            set
            {
                if (currentIndex == value) return;
                currentIndex = value;
                OnPropertyChanged("CurrentIndex");
            }
        }
        [DisplayName("分组")]
        [PropertyOrder(4)]
        public string Group { get; set; }

        /// <summary>
        ///     该计算任务的介绍
        /// </summary>
        [DisplayName("任务描述")]
        [PropertyOrder(3)]
        public string Description { get; set; }

      

        [DisplayName("名称")]
        [PropertyOrder(1)]
        public string Name
        {
            get { return name; }
            set
            {
                if (name == value) return;
                if (IsStart)
                    return;
                name = value;
                OnPropertyChanged("Name");
            }
        }

        [Browsable(false)]
        public bool IsStart
        {
            get { return _isStart; }
            set
            {
                if (_isStart == value) return;
                _isStart = value;
                OnPropertyChanged("IsStart");
            }
        }
      

       [Browsable(false)]
        public DateTime StarTime { get; set; }

        public virtual void Start()
        {
            CancellationToken = new CancellationTokenSource();
            CancellationToken.Token.Register(
                () =>
                {
                //    ProcessManager.InvokeTaskFinished(this, new ProcessEventArgs(ProcessEventType.Cancel));
                    if (AutoDelete == true)
                        Remove();
                });
            IsStart = true;
            StarTime = DateTime.Now;
            OnPropertyChanged("StarTime");

        }

        public virtual void Remove()
        {
            ControlExtended.UIInvoke(() => ProcessManager.CurrentProcessTasks.Remove(this));
            CancellationToken?.Cancel();
            autoReset.Close();
            
        }
        [Browsable(false)]
        public IProcessManager ProcessManager { get; set; }

        public void Cancel()
        {
            IsStart = false;
            IsCanceled = true;
            CancellationToken.Cancel();
          
        }

        [Browsable(false)]
        public bool IsCanceled { get; private set; }
        public bool CheckCancel()
        {
            return CancellationToken.IsCancellationRequested;
        }
        [Browsable(false)]
        public bool IsPause
        {
            get { return _isPause; }
            set
            {
                if (_isPause != value)
                {
                    _isPause = value;
                    if (value)
                    {
                        autoReset.Reset();
                    }
                    else
                    {
                        autoReset.Set();
                    }

                    OnPropertyChanged("IsPause");
                }
            }
        }

        /// <summary>
        ///     完成度
        /// </summary>
        [Browsable(false)]
        public int Percent
        {
            get { return _percent; }
            set
            {
                _percent = value;
                OnPropertyChanged("Percent");
            }
        }

        /// <summary>
        ///     是否需要取消任务
        /// </summary>
        protected CancellationTokenSource CancellationToken { get; set; }

    }
}