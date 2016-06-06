using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
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
        [LocalizedDescription("选择数据对数据库的操作")]
        public EntityExecuteType ExecuteType { get; set; }

        [LocalizedDisplayName("连接器")]
        [LocalizedDescription("选择所要连接的数据库服务")]
        [PropertyOrder(1)]
        public ExtendSelector<IDataBaseConnector> ConnectorSelector { get; set; }

        [LocalizedDisplayName("表名")]
        [LocalizedDescription("如果要新建表，则填写此项，若数据库中已经存在该表，则不执行建表操作")]
        public string TableName { get; set; }

       
        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
            var con = TableName;

            if (ExecuteType == EntityExecuteType.OnlyInsert)
            {
                if (ConnectorSelector.SelectItem is FileManager)
                {
                    var connector = FileConnector.SmartGetExport(con);

                    return connector.WriteData(documents.Select(d=>d as IFreeDocument)).Select(d=>d as IFreeDocument);
                }
                return
                    documents.Select(
                        document =>
                        {
                            ConnectorSelector.SelectItem.SaveOrUpdateEntity(document, con, null, ExecuteType);
                            return document;
                        });
            }
            return
                documents.Select(
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

            if (string.IsNullOrEmpty(TableName) == false)
            {
                if (ConnectorSelector.SelectItem.RefreshTableNames().FirstOrDefault(d => d.Name == TableName) == null)

                {
                    var data = datas?.FirstOrDefault() ?? new FreeDocument();
                    if (!ConnectorSelector.SelectItem.CreateTable(data, TableName))
                    {
                        throw new Exception($"创建名字为{TableName}的表失败");
                    }
                }
            }
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