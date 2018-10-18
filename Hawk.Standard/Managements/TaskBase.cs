using System;
using System.Threading;
using Hawk.Standard.Utils.MVVM;
using Hawk.Standard.Utils.Plugins;

namespace Hawk.Standard.Managements
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

        protected TaskBase()
        {
            autoReset = new AutoResetEvent(false);

           
        }

        [Browsable(false)]
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                    
                }
                
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
        [BrowsableAttribute.LocalizedCategoryAttribute("key_335")]
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
        [BrowsableAttribute.PropertyOrderAttribute(4)]
        public string Group { get; set; }

        /// <summary>
        ///     该计算任务的介绍
        /// </summary>
        [LocalizedDisplayName("key_314")]
        [BrowsableAttribute.PropertyOrderAttribute(3)]
        public string Description { get; set; }

        [LocalizedDisplayName("key_567")]
        [BrowsableAttribute.PropertyOrderAttribute(100)]
        public string Document
        {
            get { return Description; }
        }


        [LocalizedDisplayName("key_18")]
        [BrowsableAttribute.PropertyOrderAttribute(1)]
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
            ControlExtended.UIInvoke(() => ProcessManager.CurrentProcessTasks.Remove(this));
            CancellationToken?.Cancel();
            autoReset.Close();
            IsStart = false;
            IsCanceled = true;

        }
        [Browsable(false)]
        public IProcessManager ProcessManager { get; set; }

       

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