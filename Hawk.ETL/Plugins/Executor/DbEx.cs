using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
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
            TableSelector = new ExtendSelector<TableInfo>();
            ConnectorSelector.SelectChanged +=
                (s, e) => TableSelector.SetSource(ConnectorSelector.SelectItem.RefreshTableNames());

            ConnectorSelector.SetSource(dataManager.CurrentConnectors);
        }

        [DisplayName("操作类型")]
        [Description("选择数据对数据库的操作")]
        public EntityExecuteType ExecuteType { get; set; }

        [DisplayName("连接器")]
        [Description("选择所要连接的数据库服务")]
        [PropertyOrder(1)]
        public ExtendSelector<IDataBaseConnector> ConnectorSelector { get; set; }

        [DisplayName("表名")]
        [Description("选择所要连接的表")]
        [ PropertyOrder(1)]
        public ExtendSelector<TableInfo> TableSelector { get; set; }

        [DisplayName("新建表名")]
        [Description("如果要新建表，则填写此项，否则留空，若数据库中已经存在该表，则不执行建表操作")]
        public string NewTableName { get; set; }

        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
            var con = NewTableName;

            if (string.IsNullOrEmpty(NewTableName) == false)
            {
                if (ConnectorSelector.SelectItem.RefreshTableNames().FirstOrDefault(d => d.Name == NewTableName) == null)

                {
                    var data = documents?.FirstOrDefault() ?? new FreeDocument();
                    if (ConnectorSelector.SelectItem.CreateTable(data, NewTableName))
                    {
                        TableSelector.SelectItem =
                            ConnectorSelector.SelectItem.RefreshTableNames().FirstOrDefault(d => d.Name == NewTableName);
                    }
                    else
                    {
                        throw new Exception($"创建名字为{NewTableName}的表失败");
                    }
                }

               
            }
            if (ExecuteType == EntityExecuteType.OnlyInsert)
            {
                foreach (var document in documents)
                {
                    ConnectorSelector.SelectItem.SaveOrUpdateEntity(document, con, null, ExecuteType);
                    yield return document;
                }
            }
            else
            {
                var select = TableSelector.SelectItem;


                foreach (var document in documents)
                {
                    var v = document[Column];
                    if (v == null || @select == null) continue;
                    ConnectorSelector.SelectItem.SaveOrUpdateEntity(document, con,
                        new Dictionary<string, object> {{Column, v}}, ExecuteType);
                    yield return document;
                }
            }
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize(scenario);
            if (ConnectorSelector.SelectItem != null)
            {
                dict.Add("Connector", ConnectorSelector.SelectItem.Name);
            }
            if (TableSelector.SelectItem != null)
            {
                dict.Add("TableName", TableSelector.SelectItem.Name);
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
            TableSelector.InformPropertyChanged("");
            var connector = ConnectorSelector.SelectItem;
            if (connector == null)
                return;
            TableSelector.SelectItem =
                TableSelector.Collection.FirstOrDefault(d => d.Name == docu["TableName"].ToString());
        }
    }
}