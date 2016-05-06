using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Hawk.Core.Connectors.Vitural;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Microsoft.Win32;

namespace Hawk.Core.Connectors
{
    [XFrmWork("文件管理",  "提供与历史文件交互的数据库服务", "")]
    public class FileManager : VirtualConnectorBase, IEnumerableProvider<IDictionarySerializable>
    {
        public string LastFileName { get; set; }

        public FileManager()
        {
            if (CurrentTables.FirstOrDefault(d => d.Name == "打开新文件") == null)
            {
                CurrentTables.Insert(0, new TableInfo("打开新文件", this));
            }
        }
        public IEnumerable<IDictionarySerializable> GetEnumerable(string tableName, Type type = null)
        {
            return GetEntities2(tableName, type);
        }

        public bool CanSkip(string tableName)
        {
            return false;
        }

        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(docu, scenario);
            if (CurrentTables.FirstOrDefault(d => d.Name == "打开新文件") == null)
            {
                CurrentTables.Insert(0, new TableInfo("打开新文件", this));
            }
        }

        public override IEnumerable<IFreeDocument> GetEntities(
            string tableName, Type type, int mount = -1, int skip = 0)
        {
            return GetEntities2(tableName, type, mount, skip).ToList();
        }

        [Category("参数设置")]
        [DisplayName("编码方式")]
        public EncodingType EncodingType { get; set; }

        private IEnumerable<IFreeDocument> GetEntities2(
            string tableName, Type type, int mount = -1, int skip = 0)
        {
            TableInfo table = null;
            LastFileName = tableName;
            List<string> fileNames = new List<string>();
            if (tableName == "打开新文件" && MainDescription.IsUIForm)
            {
                var ofd2 = new OpenFileDialog();
                ofd2.DefaultExt = "*";
                ofd2.Filter = FileConnector.GetDataFilter();
                ofd2.Multiselect = true;
                if (ofd2.ShowDialog() == true)
                {
                    fileNames.AddRange(ofd2.FileNames);


                }


            }
            else
            {
                table = TableNames.Collection.FirstOrDefault(d => d.Name == tableName);
                if (table != null)
                {
                    if ((File.Exists(table.Description) == false))
                    {
                        CurrentTables.Remove(table);
                        TableNames.InformPropertyChanged("Collection");
                        return new List<IFreeDocument>();
                    }
                    fileNames.Add(table.Description);
                }
            }

            if (fileNames.Count() == 1)
            {
                var filename = fileNames.FirstOrDefault();
                LastFileName = Path.GetFileName(filename);
                table = AddTable(LastFileName, filename);
            }




            return fileNames.SelectMany(d =>
              {
                  var connector = FileConnector.SmartGetExport(d);
                  var tableConnector2 = connector as FileConnectorTable;
                  if (tableConnector2 != null)
                  {
                      tableConnector2.EncodingType = this.EncodingType;
                  }
                  if (mount > 0)

                      return connector.ReadFile().Skip(skip).Take(mount);

                  return connector.ReadFile().Skip(skip);
              });




        }


        public override TableInfo AddTable(string tableName, string desc = null, bool shouldDescOnly = false)
        {
            TableInfo table = base.AddTable(tableName, desc, true);
            if (table == null)
                return null;
            table.Size = int.MaxValue;
            return table;
        }

        #region Methods

        #endregion
    }
}