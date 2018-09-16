using System;
using System.Collections.Generic;
using Hawk.Base.Utils.Plugins;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;

namespace Hawk.Base.Managements
{
    public class AnalyzeItem
    {
        public AnalyzeItem()
        {
        }
        public IColumnProcess Process { get; set; }
        public int Input { get; set; }
        public int Output { get; set; }
        public int Error { get; set; }

        public TimeSpan RunningTime { get; set; }
        public Analyzer Analyzer { get; internal set; }

        /// <summary>
        /// 保存出错时的上下文信息
        /// </summary>
        public bool HasInit { get; set; }

        public bool MightError
        {
            get { return Input != 0 && Output == 0 && Process.Enabled == true && HasInit == false; }
        }

        public int EmptyInput { get; set; }
    }
    public class Analyzer
    {
        public Analyzer()
        {
            Items=new List<AnalyzeItem>(); 
        }

        public IDataManager DataManager { get; set; }
        private string errorLogName;
        public void Start(string name)
        {
            errorLogName = String.Format("{0}_{2}_{1}",name,DateTime.Now.ToString("HH_mm_ss"),GlobalHelper.Get("key_103"));
        }
        public void AddErrorLog(IFreeDocument item,Exception ex,IColumnProcess process)
        {
            var param = item.Clone() as FreeDocument;
            param["__SysObjectID"] = process.ObjectID;
            param["__SysETL"] = (process as ToolBase)?.Father.Name;
            param["__SysERROR"] = ex.Message;
            param["__SysTime"] = DateTime.Now.ToString();
            ControlExtended.UIInvoke(() =>
            {
                if(!ConfigFile.GetConfig<DataMiningConfig>().IsAddErrorCollection)
                    return;
                if (errorCollection == null)
                {
                    errorCollection = new DataCollection() { Name = errorLogName };
                    DataManager.AddDataCollection(errorCollection);
                }
                errorCollection?.ComputeData.Add(param);
                errorCollection?.OnPropertyChanged("Count");
            });
           
        }
        public IList<FreeDocument> ErrorLogs { get; set; }

        public AnalyzeItem Set(IColumnProcess process)
        {
            var item= new AnalyzeItem();
            item.Process = process;
            item.Analyzer = this;
            Items.Add(item);
            return item;
        }
        public List<AnalyzeItem>  Items { get; set; }


    }

}
