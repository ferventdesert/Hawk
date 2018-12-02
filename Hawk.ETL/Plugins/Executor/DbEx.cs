using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Executor
{
    [XFrmWork("DbEX", "DbEX_desc")]
    public class DbEX : DataExecutorBase
    {
        private readonly IDataManager dataManager;

        public DbEX()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["DataManager"] as IDataManager;


            ConnectorSelector = new ExtendSelector<IDataBaseConnector>();
            ConnectorSelector.GetItems = () => dataManager.CurrentConnectors.ToList();
            TableNames = new TextEditSelector();
            ConnectorSelector.SelectChanged +=
                (s, e) =>
                {
                    var text=  TableNames.SelectItem;
                    TableNames.SetSource(ConnectorSelector.SelectItem.RefreshTableNames().Select(d => d.Name));
                    if (string.IsNullOrEmpty(text) == false)
                    {
                        TableNames._SelectItem = text;
                        TableNames.SelectItem = text;
                    }
                };
            TableNames.SelectChanged += (s, e) => { InformPropertyChanged("TableNames"); };
        }
        [Browsable(false)]
        public override string KeyConfig => ExecuteType+":"+TableNames?.SelectItem;
        [LocalizedDisplayName("key_343")]
        [PropertyOrder(3)]
        [LocalizedDescription("key_344")]
        public EntityExecuteType ExecuteType { get; set; }

        [LocalizedDisplayName("key_345")]
        [LocalizedDescription("key_346")]
        [PropertyOrder(1)]
        public ExtendSelector<IDataBaseConnector> ConnectorSelector { get; set; }

        [LocalizedDisplayName("key_22")]
        [PropertyOrder(2)]
        [LocalizedDescription("key_347")]
        public TextEditSelector TableNames { get; set; }


        private List<string> columns =new List<string>(); 

        private List<string> InitTable(IEnumerable<IFreeDocument> documents)
        {
       
            var tableName = AppHelper.Query(TableNames.SelectItem,documents.FirstOrDefault());
            if (string.IsNullOrEmpty(tableName) == false)
            {
                if (!(ConnectorSelector.SelectItem != null).SafeCheck(GlobalHelper.Get("key_348")))
                {
                    return columns;
                }
                Monitor.Enter(this);
                if (ConnectorSelector.SelectItem?.RefreshTableNames().FirstOrDefault(d => d.Name.ToLower() == tableName.ToLower()) == null)
                {
                    var document = documents.MergeToDocument();
                    columns = document.GetKeys();
                    if (!ConnectorSelector.SelectItem.CreateTable(document, tableName))
                    {
                        Monitor.Exit(this);
                        throw new Exception(String.Format(GlobalHelper.Get("key_349"),tableName));
                    }
                  
                    ConnectorSelector.SelectItem?.RefreshTableNames();
                    Monitor.Exit(this);
                    return columns;
                }
                if (columns.Count == 0)
                {
                    columns = documents.GetKeys().ToList();
                }
                Monitor.Exit(this);
                return columns;
            }
            return columns;
        }

        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
            var tableName = TableNames.SelectItem;

            if (ExecuteType == EntityExecuteType.OnlyInsert)
            {
                if (ConnectorSelector.SelectItem is FileManager)
                {
                    var connector = FileConnector.SmartGetExport(tableName);

                    return connector.WriteData(documents);
                }
                return
                    documents.BatchDo(InitTable, (list,columns) =>
                    {
                        var first = list.FirstOrDefault();
                        if(first==null)
                            return;
                        tableName = first.Query(tableName);
                        ConnectorSelector.SelectItem.BatchInsert(list, (List<string>)columns, tableName);
                        XLogSys.Print.Debug(string.Format(GlobalHelper.Get("key_350"),ConnectorSelector.SelectItem.Name, tableName, list.Count));
                    });
            }
            return
                documents.Init(d =>
                {
                    var result= InitTable(new List<IFreeDocument>() {d});
                    return result.Count > 0;
                }).Select(
                    document =>
                    {
                        var v = document[Column];
                        if (v == null || tableName == null) return document;

                        ConnectorSelector.SelectItem.SaveOrUpdateEntity(document, tableName,
                            new Dictionary<string, object> {{Column, v}}, ExecuteType);
                        return document;
                    });
        }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
             columns = new List<string>();
            return Assert(ConnectorSelector.SelectItem!=null, GlobalHelper.Get("key_345")) &&Assert(string.IsNullOrEmpty(TableNames.SelectItem)==false, GlobalHelper.Get("key_22"));
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize(scenario);
            if (ConnectorSelector.SelectItem != null)
            {
                dict.Add("Connector", ConnectorSelector.SelectItem.Name);
            }
            if (TableNames.SelectItem != null)
            {
                dict.Add("Table", TableNames.SelectItem);
            }

            return dict;
        }

        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(docu);
            ConnectorSelector.SetSource(dataManager.CurrentConnectors);
            ConnectorSelector.SelectItem =
                dataManager.CurrentConnectors.FirstOrDefault(d => d.Name == docu["Connector"].ToString());

            TableNames.SelectItem = docu["Table"].ToString();
               // ConnectorSelector.SelectItem?.RefreshTableNames().FirstOrDefault(d => d.Name == docu["Table"].ToString())?.Name;
        }
    }
}