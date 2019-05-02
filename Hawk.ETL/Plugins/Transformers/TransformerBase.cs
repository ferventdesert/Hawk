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
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using Hawk.ETL.Process;
using NPOI.OpenXml4Net.Exceptions;

namespace Hawk.ETL.Plugins.Transformers
{
    public abstract class ToolBase : PropertyChangeNotifier, IColumnProcess
    {
        private bool _enabled;
        protected bool IsExecute = true;

        public bool GetExecute()
        {
            return IsExecute;
        }

        protected ToolBase()
        {
            ColumnSelector = new TextEditSelector();
            ColumnSelector.SelectChanged += (s, e) => Column = ColumnSelector.SelectItem;
        }

        [LocalizedCategory("key_211"), PropertyOrder(1), LocalizedDisplayName("key_717")]
        [LocalizedDescription("key_566")]
        public TextEditSelector ColumnSelector { get; set; }

        public bool Assert(bool result, string name="", bool isthrow = true,string reason="")
        {
            if (result == false)
            {
                var str = GlobalHelper.FormatArgs("condition_check", this.ObjectID, name, reason);
                XLogSys.Print.Warn(str);
                if(isthrow)
                    throw  new  InvalidOperationException(str);

            }
            return result;

        }
        [Browsable(false)]
        public virtual string KeyConfig
        {
            get { return Column; }
        }

        [LocalizedDisplayName("key_567")]
        [PropertyOrder(100)]
        [PropertyEditor("MarkdownEditor")]
        public string Document => ETLHelper.GetMarkdownScript(GetType());

        public virtual void Finish()
        {
        }

        public virtual bool Init(IEnumerable<IFreeDocument> docus)
        {
            return true;
        }

        public virtual IEnumerable<IFreeDocument> CheckDatas(IEnumerable<IFreeDocument> docs)
        {
            return docs;
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

        [Browsable(false)]
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
        [Browsable(false)]
        public AnalyzeItem AnalyzeItem
        {
            get
            {
                AnalyzeItem analyzer = null;
                if (Father == null)
                    return null;
                if (Father.Analyzer == null)
                    return null;
                foreach (var _analyzer in this.Father?.Analyzer?.Items)
                {
                    if (_analyzer.Process.ObjectID == this.ObjectID)
                        analyzer = _analyzer;
                }
                return analyzer;
            }
        }
        public void SetExecute(bool value)
        {
            IsExecute = value;
        }

        [LocalizedCategory("key_211")]
        [LocalizedDisplayName("key_568")]
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

        [LocalizedCategory("key_211")]
        [LocalizedDisplayName("key_12")]
        [PropertyOrder(0)]
        public string TypeName
        {
            get
            {
                var item = AttributeHelper.GetCustomAttribute(GetType());
                return item == null ? GetType().ToString() : item.Name;
            }
        }

        [LocalizedCategory("key_211")]
        public string ObjectID { get; set; }

        [Browsable(false)]
        public XFrmWorkAttribute Attribute => AttributeHelper.GetCustomAttribute(GetType());

        [PropertyEditor("CodeEditor")]
        [PropertyOrder(99)]
        [LocalizedDisplayName("remark")]
        [LocalizedDescription("remark_desc")]
        public string Remark { get; set; }

        public virtual IEnumerable<string> InputColumns()
        {
            if (!string.IsNullOrEmpty(Column))
                yield return Column;
        }

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



        [Browsable(false)] protected readonly IProcessManager processManager;

        protected TransformerBase()
        {
            OneOutput = true;
            Column = "";
            NewColumn = "";
            Enabled = true;
            IsMultiYield = false;
            IXPlugin plugin;
            if (MainDescription.MainFrm != null)
            {
                if (MainDescription.MainFrm.PluginDictionary.TryGetValue("DataProcessManager", out plugin))
                {
                    processManager = plugin as IProcessManager;
                }
            }
        }


        protected SmartCrawler GetCrawler(string name)
        {
            if (processManager == null)
                return null;
            var crawlers = processManager.CurrentProcessCollections.OfType<SmartCrawler>().ToList();
            if (string.IsNullOrEmpty(name))
            {
                if (crawlers.Count() == 1)
                {
                    name = crawlers[0].Name;
                }
            }
            var crawler = this.GetTask<SmartCrawler>(name);
            if (crawler != null)
            {
                IsMultiYield = crawler?.IsMultiData == ScriptWorkMode.List;
            }
            return crawler;
        }

        #endregion

        #region Properties

        [LocalizedCategory("key_211")]
        [PropertyOrder(2)]
        [LocalizedDisplayName("key_433")]
        [LocalizedDescription("key_569")]
        public virtual string NewColumn
        {
            get { return _newColumn; }
            set
            {
                if (_newColumn != value)
                {
                    _newColumn = value;
                    OnPropertyChanged("NewColumn");
              
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

        public virtual IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas,
            AnalyzeItem analyzer = null)

        {
            var olddatas = datas;
            var errorCounter = 0;
            foreach (var data in datas)
            {
                IEnumerable<IFreeDocument> newdatas = null;
                try
                {
                    DateTime now= DateTime.Now;
                    newdatas = InternalTransformManyData(data);
                    if(analyzer!=null)
                        analyzer.RunningTime +=DateTime.Now-now;
                }
                catch (Exception ex)
                {
                    if(analyzer!=null)
                        analyzer.Analyzer.AddErrorLog(data, ex, this);
                    else
                    {
                        XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_208"), this.Column, this.TypeName, ex.Message));
                    }
                }

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
                                        string.Format(GlobalHelper.Get("fail_remind"), Column, TypeName),
                                        GlobalHelper.Get("key_570"),
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
                    continue;
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