using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using System.Windows.Input;
using Hawk.Core.Connectors.Vitural;
using Hawk.Core.Utils.Plugins;
using Microsoft.Win32;
using EncodingType = Hawk.Core.Utils.EncodingType;

namespace Hawk.Core.Connectors
{
    [XFrmWork("文件管理",  "提供与历史文件交互的数据库服务", "")]
    public class FileManager : VirtualConnectorBase, IEnumerableProvider<IDictionarySerializable>
    {
        [Browsable(false)]
        public string LastFileName { get; set; }

        public FileManager()
        {
            if (CurrentTables.FirstOrDefault(d => d.Name == openfile) == null)
            {
                CurrentTables.Insert(0, new TableInfo(openfile, this));
            }
            AutoConnect = true;
            ConnectDB();
        }
        public IEnumerable<IDictionarySerializable> GetEnumerable(string tableName)
        {
            return GetEntities2(tableName);
        }

        public bool CanSkip(string tableName)
        {
            return false;
        }

        [Browsable(false)]

        public override ReadOnlyCollection<ICommand> Commands => null;

        [Browsable(false)] 
        public override string Server { get; set; }


        [Browsable(false)]
        public override string DBName { get; set; }

        [Browsable(false)]
        public override string UserName { get; set; }
        [Browsable(false)]
        //  [PropertyEditor("PasswordEditor")]
        public override string Password { get; set; }


        public override IEnumerable<FreeDocument> GetEntities(
            string tableName,  int mount = -1, int skip = 0)
        {
            return GetEntities2(tableName, mount, skip);
        }

        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("编码方式")]
        public EncodingType EncodingType { get; set; }

        private IEnumerable<FreeDocument> GetEntities2(
            string tableName,  int mount = -1, int skip = 0)
        {
            TableInfo table = null;
            LastFileName = tableName;
            List<string> fileNames = new List<string>();
            if (tableName == openfile && MainDescription.IsUIForm)
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
                        return new List<FreeDocument>();
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

                  return connector.ReadFile().Skip(skip) ;
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