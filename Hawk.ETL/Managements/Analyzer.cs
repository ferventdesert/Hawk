using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Plugins.Transformers;
using Hawk.Core.Utils.MVVM;
using Hawk.ETL.Process;

namespace Hawk.ETL.Managements
{
    public class AnalyzeItem
    {
        public AnalyzeItem()
        {
        }
        public IColumnProcess Process { get; set; }
        private int output;
        public int Input { get; set; }

        public int Output { 
        get { return output; }
        set
        {
            if (value != output)
            {
                if (value > 0 && output == 0)
                {
                    output = value;
                    (Process as PropertyChangeNotifier).OnPropertyChanged("AnalyzeItem");
                }
                output = value;
                   
            }
        }
        }
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
        private DataCollection errorCollection;
        public void Start(string name)
        {
            errorLogName = String.Format("{0}_{2}_{1}",name,DateTime.Now.ToString("HH_mm_ss"),GlobalHelper.Get("error_message"));
            errorCollection = null;
        }
        public void AddErrorLog(IFreeDocument item,Exception ex,IColumnProcess process)
        {
            if (!(process as ToolBase).GetExecute())
            {

               XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_208"), process.Column, process.TypeName, ex.Message));
                return;
            }
            if (string.IsNullOrEmpty(errorLogName))
                return;
            var param = item.Clone() as FreeDocument;
            param["__SysObjectID"] = process.ObjectID;
            param["__SysETL"] = (process as ToolBase)?.Father.Name;
            param["__SysERROR"] = ex.Message;
            param["__SysTime"] = DateTime.Now.ToString();
            ControlExtended.UIInvoke(() =>
            {
                if (ConfigFile.GetConfig<DataMiningConfig>().IsAddErrorCollection)
                {
                    if (errorCollection == null)
                    {
                        errorCollection = new DataCollection() {Name = errorLogName};
                        DataManager.AddDataCollection(errorCollection);
                    }
                    errorCollection?.ComputeData.Add(param);
                    errorCollection?.OnPropertyChanged("Count");
                }
                else
                {
                    XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_208"), process.Column, process.TypeName, ex.Message));
                }
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
        public SmartETLTool Container { get; set; }
    }

}
