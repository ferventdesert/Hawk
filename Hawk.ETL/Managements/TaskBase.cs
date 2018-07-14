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
        private bool _isSelected;

        protected TaskBase()
        {
            autoReset = new AutoResetEvent(false);
            ProcessManager = MainDescription.MainFrm.PluginDictionary["模块管理"] as IProcessManager;

           
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
        [LocalizedCategory("遍历状态")]
        [LocalizedDisplayName("当前位置")]
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
        [LocalizedDisplayName("分组")]
        [PropertyOrder(4)]
        public string Group { get; set; }

        /// <summary>
        ///     该计算任务的介绍
        /// </summary>
        [LocalizedDisplayName("任务描述")]
        [PropertyOrder(3)]
        [PropertyEditor("CodeEditor")]
        public string Description { get; set; }

     
    

        [LocalizedDisplayName("名称")]
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