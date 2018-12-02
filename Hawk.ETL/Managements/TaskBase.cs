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
        private int _outputIndex;
        private string name;
        private int _percent;
        private bool _isSelected;
        private bool _isCanceled;
        private bool _shouldPause;

        public int Total { get; set; }

        protected TaskBase()
        {
            autoReset = new AutoResetEvent(false);
            ProcessManager = MainDescription.MainFrm.PluginDictionary["DataProcessManager"] as IProcessManager;

           
        }

        
        public string PubliserName
        {
            get
            {
                if (Publisher == null)
                    return null;
                var pub = Publisher as IDataProcess;
                if (pub == null)
                    return null;
                return pub.Name;
            }
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

        public bool CheckWait()
        {
            if (!IsPause) return false;
            autoReset.WaitOne();
            return true;
        }
        [LocalizedCategory("key_335")]
        [LocalizedDisplayName("key_336")]
        public int OutputIndex
        {
            get { return _outputIndex; }
            set
            {
                if (_outputIndex == value) return;
                _outputIndex = value;
                OnPropertyChanged("OutputIndex");
            }
        }
        [LocalizedDisplayName("key_337")]
        [PropertyOrder(4)]
        public string Group { get; set; }

        /// <summary>
        ///     该计算任务的介绍
        /// </summary>
        [LocalizedDisplayName("key_314")]
        [PropertyOrder(3)]
        [PropertyEditor("CodeEditor")]
        public string Description { get; set; }

        [LocalizedDisplayName("key_567")]
        [PropertyOrder(100)]
        [PropertyEditor("MarkdownEditor")]
        public string Document
        {
            get { return Description; }
        }


        [LocalizedDisplayName("key_18")]
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
            IsStart = true;
            StarTime = DateTime.Now;
            OnPropertyChanged("StarTime");

        }

        public virtual void Remove()
        {
            if(ProcessManager.CurrentProcessTasks.Contains(this))
                 ControlExtended.UIInvoke(() => ProcessManager.CurrentProcessTasks.Remove(this));
            CancellationToken?.Cancel();
            autoReset.Close();
            IsStart = false;
            IsCanceled = true;

        }
        [Browsable(false)]
        public IProcessManager ProcessManager { get; set; }


        [Browsable(false)]
        public bool IsCanceled
        {
            get { return _isCanceled; }
            private set
            {
                if (_isCanceled != value)
                {
                    _isCanceled = value;
                    OnPropertyChanged("IsCanceled");
                }
            }
        }

        public bool CheckCancel()
        {
            return CancellationToken.IsCancellationRequested;
        }

        /// <summary>
        /// 指示用户意愿
        /// </summary>
        public bool ShouldPause
        {
            get { return _shouldPause; }
            set
            {
                IsPause = value;
                _shouldPause = value;
            }
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