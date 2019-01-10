using System;
using System.Collections.Generic;

namespace Hawk.Core.Utils.Plugins
{

    public class  ViewItem
    {
        public ViewItem(object view, string name)
        {
            this.View = view;
            this.Name = name;
        }

        public ViewItem(object view, string name, FrmState frmState)
        {
            this.View = view;
            this.Name = name;
            this.FrmState = frmState;
        }

        public object Container { get; set; }

        public object Model { get; set; }

        public string Group { get; set; }

        public object View { get; set; }

        public string Name { get; set; }

        public FrmState FrmState  { get; set; }
    }
    public enum DockChangedType
    {
        Remove,
        Change,
    }
    public class DockChangedEventArgs : EventArgs
    {
        public DockChangedType DockChangedType { get; set; }
        public object Control { get; set; }

        public string Name { get; set; }

        public DockChangedEventArgs(DockChangedType theDockChangedType, object thisControl,string name=null)
            : base()
        {
            DockChangedType = theDockChangedType;
            Control = thisControl;
            this.Name = name;
        }
    }
    /// <summary>
    /// 实现可动态调配界面的接口
    /// </summary>
    public interface IDockableManager
    {
        /// <summary>
        /// 添加一个用户控件
        /// </summary>
        /// <param name="thisState"></param>
        /// <param name="thisControl"></param>
        /// <param name="objects"></param>
        void AddDockAbleContent(FrmState thisState, object thisControl, params string[] objects);
        /// <summary>
        /// 删除控件
        /// </summary>
        /// <param name="rc"></param>
        void RemoveDockableContent(object model);
        /// <summary>
        /// 激活控件
        /// </summary>
        /// <param name="view"></param>
        void ActiveThisContent(object view);

       
      
        void ActiveThisContent(string name);
  
 
        /// <summary>
        /// 当用户改变窗体结构时激活
        /// </summary>
        event EventHandler<DockChangedEventArgs> DockManagerUserChanged;

        /// <summary>
        /// 可视化UI字典
        /// </summary>
        List<ViewItem> ViewDictionary { get; }

        /// <summary>
        /// 设置忙时提醒
        /// </summary>
        /// <param name="isBusy"></param>
        /// <param name="title"></param>
         void SetBusy(ProgressBarState state = ProgressBarState.Normal, string title = "系统正忙",
            string message = "正在处理长时间操作", int percent = 0);



    }

    
    //
    // 摘要:
    //     Represents the thumbnail progress bar state.
    public enum ProgressBarState
    {
        //
        // 摘要:
        //     No progress is displayed.
        NoProgress = 0,
        //
        // 摘要:
        //     The progress is indeterminate (marquee).
        Indeterminate = 1,
        //
        // 摘要:
        //     Normal progress is displayed.
        Normal = 2,
        //
        // 摘要:
        //     An error occurred (red).
        Error = 4,
        //
        // 摘要:
        //     The operation is paused (yellow).
        Paused = 8
    }


}