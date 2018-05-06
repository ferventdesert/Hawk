using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.WpfPropertyGrid;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Transformers
{
    public abstract class ToolBase : PropertyChangeNotifier, IColumnProcess
    {
        private bool _enabled;
        private int _etlIndex;
        protected bool IsExecute = true;

        protected ToolBase()
        {
            ColumnSelector = new TextEditSelector();
            ColumnSelector.SelectChanged += (s, e) => Column = ColumnSelector.SelectItem;
        }

        [LocalizedCategory("1.基本选项"), PropertyOrder(1), DisplayName("输入列")]
        [LocalizedDescription("本模块要处理的列的名称")]
        public TextEditSelector ColumnSelector { get; set; }

      
        public virtual void Finish()
        {
        }

        public virtual bool Init(IEnumerable<IFreeDocument> docus)
        {
            return true;
        }

        public virtual void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            this.UnsafeDictDeserializePlus(docu);
        }

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = this.UnsafeDictSerializePlus();
            dict.Add("Type", GetType().Name);
            dict.Remove("ETLIndex");
            dict.Remove("ColumnSelector");
            return dict;
        }

        [Browsable(false)]
        public string Column
        {
            get { return ColumnSelector.SelectItem; }
            set
            {
                if (value != ColumnSelector.SelectItem)
                {
                    ColumnSelector.SelectItem = value;
                    OnPropertyChanged("Column");
                }
            }
        }

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

        public void SetExecute(bool value)
        {
            IsExecute = value;
        }

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

        [Browsable(false)]
        public SmartETLTool Father { get; set; }

        [Browsable(false)]
        public int ETLIndex
        {
            get { return _etlIndex; }
            set
            {
                if (_etlIndex != value)
                {
                    _etlIndex = value;
                    OnPropertyChanged("ETLIndex");
                }
            }
        }

        [LocalizedCategory("1.基本选项")]
        [LocalizedDisplayName("类型")]
        [PropertyOrder(0)]
        public string TypeName
        {
            get
            {
                var item = AttributeHelper.GetCustomAttribute(GetType());
                return item == null ? GetType().ToString() : item.Name;
            }
        }

        [Browsable(false)]
        public XFrmWorkAttribute Attribute => AttributeHelper.GetCustomAttribute(GetType());

        public override string ToString()
        {
            return TypeName + " " + Column;
        }
    }

    public abstract class TransformerBase : ToolBase, IColumnDataTransformer
    {
        #region Public Methods

        #endregion

        #region Constants and Fields

        #endregion

        #region Constructors and Destructors

        protected bool IsExecute;


        [Browsable(false)] protected readonly IProcessManager processManager;

        protected TransformerBase()
        {
            OneOutput = true;
            Column = "";
            NewColumn = "";
            Enabled = true;
            IsMultiYield = false;
            processManager = MainDescription.MainFrm.PluginDictionary["模块管理"] as IProcessManager;
        }


        protected SmartCrawler GetCrawler(string name)
        {
            var crawlers = processManager.CurrentProcessCollections.OfType<SmartCrawler>().ToList();
            if (string.IsNullOrEmpty(name))
            {
                if (crawlers.Count() == 1)
                {
                    name = crawlers[0].Name;
                }
            }
            var crawler = this.GetModule<SmartCrawler>(name);
            if (crawler != null)
            {
                IsMultiYield = crawler?.IsMultiData == ScriptWorkMode.List;
            }
            return crawler;
        }

        #endregion

        #region Properties

        [LocalizedCategory("1.基本选项")]
        [PropertyOrder(2)]
        [LocalizedDisplayName("输出列")]
        [LocalizedDescription("结果要输出到的列的名称")]
        public virtual string NewColumn
        {
            get { return _newColumn; }
            set
            {
                if (_newColumn != value)
                {
                    OnPropertyChanged("NewColumn");
                    _newColumn = value;
                }
            }
        }


        /// <summary>
        ///     是否在数据中必须包含列名
        /// </summary>
        [Browsable(false)]
        public virtual bool OneOutput { get; set; }

        #endregion

        #region Implemented Interfaces

        #region IColumnDataTransformer

        public virtual object TransformData(IFreeDocument datas)
        {
            return null;
        }

        [Browsable(false)]
        public virtual bool IsMultiYield { get; set; }


        protected void SetValue(IFreeDocument doc, object item)
        {
            if (string.IsNullOrEmpty(NewColumn))
                doc.SetValue(Column, item);
            else
                doc.SetValue(NewColumn, item);
        }

        private bool isErrorRemind = true;
        private string _newColumn;

        public virtual IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)

        {
            var olddatas = datas;
            var errorCounter = 0;
            foreach (var data in datas)
            {
                var newdatas = InternalTransformManyData(data);
                if (MainDescription.IsUIForm)
                {
                    if (((olddatas is IList) == false || !olddatas.Any()) && newdatas is IList &&
                        (!newdatas.Any()))
                    {
                        errorCounter++;
                        if (errorCounter == 5 && isErrorRemind)
                        {
                            //连续三次无值输出，表示为异常现象
                            if (ControlExtended.UIInvoke(() =>
                            {
                                var result =
                                    MessageBox.Show(
                                        $"作用在列名`{Column}`的 模块`{TypeName}` 已经连续 {5} 次没有成功获取数据，可能需要重新修改参数 \n 【是】：【进入调试模式】 \n 【否】：【取消当前任务】 \n 【取消】：【不再提示】",
                                        "参数设置可能有误",
                                        MessageBoxButton.YesNoCancel);
                                if (result == MessageBoxResult.Yes)
                                {
                                    var window = PropertyGridFactory.GetPropertyWindow(this);

                                    var list = processManager.CurrentProcessTasks.Where(
                                        task => task.Publisher == Father && task.IsPause == false).ToList();
                                    list.Execute(task => task.Remove());

// window.Closed += (s, e) => Father.ETLMount++;
                                    Father.ETLMount = Math.Max(0, Father.CurrentETLTools.IndexOf(this));
                                    window.ShowDialog();
                                    window.Topmost = true;
                                    return true;
                                }
                                if (result == MessageBoxResult.Cancel)
                                {
                                    isErrorRemind = false;
                                    return true;
                                }
                                return false;
                            }) == false)
                                yield break;
                        }
                    }
                    else
                    {
                        errorCounter = 0;
                    }
                }
                if (newdatas == null)
                    yield break;
                foreach (var newdata in newdatas)
                {
                    yield return newdata;
                }
            }
        }

        protected virtual IEnumerable<IFreeDocument> InternalTransformManyData(IFreeDocument datas)
        {
            yield break;
        }

        #endregion

        #region IColumnProcess

        #endregion

        #region IDictionarySerializable

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize();
            dict.Add("Group", "Transformer");
            return dict;
        }

        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize();
            AppHelper.ConfigVersionConverter(dict, docu);
            //向上兼容性转换
            base.DictDeserialize(docu);
        }

        #endregion

        #endregion
    }
}