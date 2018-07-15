using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
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
                (s, e) => TableNames.SetSource(ConnectorSelector.SelectItem.RefreshTableNames().Select(d=>d.Name));
            TableNames.SelectChanged += (s, e) => { InformPropertyChanged("TableNames"); };
        }

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

        private bool InitTable(IFreeDocument document)
        {
            var tableName = TableNames.SelectItem;
            if (string.IsNullOrEmpty(tableName) == false)
            {
                if (!(ConnectorSelector.SelectItem != null).SafeCheck(GlobalHelper.Get("key_348")))
                {
                    return false;
                }
                if (ConnectorSelector.SelectItem?.RefreshTableNames().FirstOrDefault(d => d.Name == tableName) == null)

                {
                    if (!ConnectorSelector.SelectItem.CreateTable(document, tableName))
                    {
                        throw new Exception(String.Format(GlobalHelper.Get("key_349"),tableName));
                    }
                    return true;
                }
                return true;
            }
            return false;
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
                    documents.BatchDo(InitTable, list =>
                    {
                        
                        ConnectorSelector.SelectItem.BatchInsert(list, tableName);
                        XLogSys.Print.Info(string.Format(GlobalHelper.Get("key_350"),ConnectorSelector.SelectItem.Name,TableNames.SelectItem,list.Count));
                    });
            }
            return
                documents.Init(InitTable).Select(
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
            return true;
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