using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Transformers
{

    public abstract class TransformerBase : PropertyChangeNotifier, IColumnDataTransformer
    {
        #region Constants and Fields

        #endregion

        #region Constructors and Destructors
        protected bool IsExecute;

        public void SetExecute(bool value)
        {
            IsExecute = value;
        }

        protected readonly IProcessManager processManager;
        protected TransformerBase()
        {
            this.OneOutput = true;
            this.Column = "";
            this.NewColumn = "";
            this.Enabled = true;
            IsMultiYield = false;
            processManager = MainDescription.MainFrm.PluginDictionary["模块管理"] as IProcessManager;

        }


        protected SmartCrawler GetCrawler(string name)
        {
          var   crawler =
             processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == name) as SmartCrawler;
            if (crawler != null)
            {
                IsMultiYield = crawler?.IsMultiData == ListType.List;
            }
            else
            {
                var task = processManager.CurrentProject.Tasks.FirstOrDefault(d => d.Name == name);
                if (task != null)
                {

                    ControlExtended.UIInvoke(() => { task.Load(false); });
                    crawler =
                        processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == name) as
                            SmartCrawler;
                }
                if (crawler == null)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                
                    XLogSys.Print.Error($"您没有填写“从爬虫转换”的“爬虫选择”。需要填写要调用的网页采集器的名称");
                    }
                    else
                    {
                        
                    XLogSys.Print.Error($"没有找到名称为'{name}'的网页采集器，请检查“从爬虫转换”的“爬虫选择”是否填写错误");
                    }
                }

            }
            return crawler;
        }
        #endregion

        #region Properties



        [LocalizedCategory("1.基本选项"), PropertyOrder(1), DisplayName("输入列")]
        [LocalizedDescription("本模块要处理的列的名称")]
        public string Column { get; set; }

        [LocalizedDisplayName("介绍")]
        [PropertyOrder(100)]
        [PropertyEditor("CodeEditor")]
        public string Description
        {
            get
            {
                var item = AttributeHelper.GetCustomAttribute(GetType());
                if (item == null)
                    return GetType().ToString();
                return item.Description;
            }
        }



        [LocalizedCategory("1.基本选项")]
        [PropertyOrder(2)]
        [LocalizedDisplayName("输出列")]
        [LocalizedDescription("结果要输出到的列的名称")]
        public virtual string NewColumn { get; set; }

  
        private bool _enabled;

        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("启用")]
        [PropertyOrder(5)]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                OnPropertyChanged("Enabled");
            }
        }


        /// <summary>
        ///     是否在数据中必须包含列名
        /// </summary>
        [Browsable(false)]
        public virtual bool OneOutput { get;  set; }




        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("类型")]
        [PropertyOrder(0)]
        public string TypeName
        {
            get
            {
                XFrmWorkAttribute item = AttributeHelper.GetCustomAttribute(this.GetType());
                return item == null ? this.GetType().ToString() : item.Name;
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return this.TypeName + " " + this.Column;
        }

        #endregion

        #region Implemented Interfaces

        #region IColumnDataTransformer

        public virtual object TransformData(IFreeDocument datas)
        {
            return null;
        }

        [Browsable(false)]
        public virtual bool IsMultiYield { get; set; }


        protected void SetValue(IFreeDocument doc,object item)
        {
            if(string.IsNullOrEmpty(NewColumn))
                doc.SetValue(Column,item);
            else
                doc.SetValue(NewColumn,item);
        }
     

        public virtual IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            yield break;
        }

       
        #endregion

        #region IColumnProcess

        public virtual void Finish()
        {

        }

        public virtual bool Init(IEnumerable<IFreeDocument> docus)
        {
            return true;
        }

        #endregion

        #region IDictionarySerializable

        public virtual void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            this.UnsafeDictDeserialize(docu);
           
        }

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = this.UnsafeDictSerialize();
            dict.Add("Type", this.GetType().Name);
            dict.Add("Group","Transformer");
            return dict;
        }

        #endregion

        #endregion
    }
}