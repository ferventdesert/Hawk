using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    [XFrmWork("数据库操作", "进行数据库操作，包括写入和更新，拖入的列为表的主键")]
    public class DbEX : DataExecutorBase
    {
        private readonly IDataManager dataManager;

        public DbEX()
        {
            dataManager = MainDescription.MainFrm.PluginDictionary["数据管理"] as IDataManager;
            ConnectorSelector = new ExtendSelector<IDataBaseConnector>();
            ConnectorSelector.SetSource(dataManager.CurrentConnectors);
        }

        [LocalizedDisplayName("操作类型")]
        [LocalizedDescription("选择数据库的操作，如插入，删除，更新等")]
        public EntityExecuteType ExecuteType { get; set; }

        [LocalizedDisplayName("选择数据库")]
        [LocalizedDescription("选择所要连接的数据库服务，如果该项无法选择，请配置【模块管理】->【数据源】，并点击右键创建新的数据库连接器")]
        [PropertyOrder(1)]
        public ExtendSelector<IDataBaseConnector> ConnectorSelector { get; set; }

        [LocalizedDisplayName("表名")]
        [LocalizedDescription("必填，若数据库不存在该表，则会根据第一条数据的列自动创建表")]
        public string TableName { get; set; }

        private bool InitTable(    IFreeDocument document)
        {
            if (string.IsNullOrEmpty(TableName) == false)
            {
                if (!(ConnectorSelector.SelectItem != null).SafeCheck("数据库连接器不能为空"))
                {
                    return false;
                }
                if (ConnectorSelector.SelectItem?.RefreshTableNames().FirstOrDefault(d => d.Name == TableName) == null)

                {
                    if (!ConnectorSelector.SelectItem.CreateTable(document, TableName))
                    {
                        throw new Exception($"创建名字为{TableName}的表失败");
                    }
                    return true;
                }
            }
            return false;
        }
         
       
        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {

         


            var con = TableName;

            if (ExecuteType == EntityExecuteType.OnlyInsert)
            {
                if (ConnectorSelector.SelectItem is FileManager)
                {
                    var connector = FileConnector.SmartGetExport(con);

                    return connector.WriteData(documents);
                }
                return
                    documents.Init(InitTable).Select(
                        document =>
                        {
                            ConnectorSelector.SelectItem?.SaveOrUpdateEntity(document, con, null, ExecuteType);
                            return document;
                        });
            }
            return
                documents.Init(InitTable).Select(
                    document =>
                    {
                        var v = document[Column];
                        if (v == null || TableName == null) return document;

                        ConnectorSelector.SelectItem.SaveOrUpdateEntity(document, con,
                            new Dictionary<string, object> {{Column, v}}, ExecuteType);
                        return document;
                    });
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize(scenario);
            if (ConnectorSelector.SelectItem != null)
            {
                dict.Add("Connector", ConnectorSelector.SelectItem.Name);
            }


            return dict;
        }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {

        
            return true;
        }

        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(docu);
            ConnectorSelector.SelectItem =
                dataManager.CurrentConnectors.FirstOrDefault(d => d.Name == docu["Connector"].ToString());
            var connector = ConnectorSelector.SelectItem;
            if (connector == null)
                return;
        }
    }
}