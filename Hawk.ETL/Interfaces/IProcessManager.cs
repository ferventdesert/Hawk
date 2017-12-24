using System;
using System.Collections.Generic;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Managements;


namespace Hawk.ETL.Interfaces
{
    /// <summary>
    /// 处理管理器接口
    /// </summary>
    public interface IProcessManager
    {
        /// <summary>
        /// 当前已经加入的处理方法集合
        /// </summary>
        ICollection<IDataProcess> CurrentProcessCollections { get; }


         

        /// <summary>
        /// 通过名称获取处理方法实例
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isAddToList">是否添加到列表 </param>
        /// <returns></returns>
        IDataProcess GetOneInstance(string name, bool isAddToList = true, bool newOne = false,bool addUI=false);

      
        

               /// <summary>
        /// 删除一个模块
        /// </summary>
        /// <param name="process"></param>
        void RemoveOperation(IDataProcess process);


        /// <summary>
        /// 当前在任务队列中的任务
        /// </summary>
        IList<TaskBase> CurrentProcessTasks { get; set; }

        /// <summary>
        /// 当前的工程
        /// </summary>
        Project CurrentProject { get; set; }


        /// <summary>
        /// 工程改变
        /// </summary>
        event EventHandler OnCurrentProjectChanged;

    }
}