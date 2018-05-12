using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using System.Windows.Input;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    /// <summary>
    ///     列信息
    /// </summary>
    /// 

    public class ColumnInfo : PropertyChangeNotifier, IDictionarySerializable
    {
        #region Constructors and Destructors

        public ColumnInfo(string name)
        {
            Name = name;
            Importance = 1;
        }

        public ColumnInfo()
        {
        }

        #endregion

        #region Properties

        private string dataType;

        [LocalizedDisplayName("类型")]
        [Xceed.Wpf.Toolkit.PropertyGrid.Attributes.PropertyOrder(1)]
        [LocalizedDescription("该数据的类型")]
        public string DataType
        {
            get { return dataType; }
            set
            {
                if (dataType != value)
                {
                    dataType = value;
                    OnPropertyChanged("DataType");
                }
            }
        }

        [LocalizedDisplayName("权重")]
        [PropertyOrder(3)]
        public double Importance { get; set; }

        [LocalizedDisplayName("主键")]
        [PropertyOrder(2)]
        public bool IsKey { get; set; }


        [LocalizedDisplayName("描述")]
        [PropertyOrder(4)]
        public string Description { get; set; }

        /// <summary>
        ///     启用虚拟化，则该值在需要时被动态计算
        /// </summary>
        [LocalizedDisplayName("虚拟值")]
        public bool IsVirtual { get; set; }

        [LocalizedDisplayName("名称")]
        [PropertyOrder(0)]
        public string Name { get; set; }
        [LocalizedDisplayName("可空")]
        [PropertyOrder(0)]
        public bool CanNull { get; set; }

        #endregion

        #region Implemented Interfaces

        #region IDictionarySerializable

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Importance = docu.Set("Importance", Importance);
            Name = docu.Set("Name", Name);
            DataType = docu.Set("DataType", DataType);
            IsKey = docu.Set("IsKey", IsKey);
            IsVirtual = docu.Set("IsVirtual", IsVirtual);
            IsVirtual = docu.Set("CanNull", CanNull);
            Description = docu.Set("Desc", Description);
        }

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = new FreeDocument();
            dict.Add("Importance", Importance);
            dict.Add("Name", Name);
            dict.Add("DataType", DataType);
            dict.Add("IsKey", IsKey);
            dict.Add("IsVirtual", IsVirtual);
            dict.Add("Desc", Description);
            return dict;
        }

        #endregion

        #endregion
    }

    /// <summary>
    ///     数据表信息
    /// </summary>
    public class TableInfo : IDictionarySerializable
    {
        public TableInfo(string name, IDataBaseConnector connector)
        {
            Name = name;
            Connector = connector;
            ColumnInfos=new List<ColumnInfo>();
        }


        public TableInfo()
        {
            ColumnInfos=new List<ColumnInfo>();
        }

        [Browsable(false)]
        public List<ColumnInfo> ColumnInfos { get; set; }
        [PropertyOrder(1)]
        [LocalizedDisplayName("表大小")]
        public int Size { get; set; }

        [PropertyOrder(0)]
        [LocalizedDisplayName("名称")]
        public string Name { get; set; }
        [PropertyOrder(2)]
        [LocalizedDisplayName("描述")]
        public string Description { get; set; }

        [Browsable(false)]
        public IDataBaseConnector Connector { get; set; }

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = new FreeDocument();
            dict.Add("Name", Name);
            dict.Add("Size", Size);
            dict.Add("Description", Description);
            dict.Children = new List<FreeDocument>();
            dict.Children = ColumnInfos.Select(d => d.DictSerialize()).ToList();
            return dict;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Name = docu.Set("Name", Name);
            Size = docu.Set("Size", Size);

            Description = docu.Set("Description", Description);
            var doc = docu as FreeDocument;
            if (doc != null && doc.Children != null)
            {


                foreach (FreeDocument item in doc.Children)
                {
                    var Column = new ColumnInfo();
                    Column.DictDeserialize(item);
                    ColumnInfos.Add(Column);
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public interface IEnumerableProvider<T>
    {
        IEnumerable<T> GetEnumerable(string tableName);

        bool CanSkip(string tableName);
    }

    public enum DBSearchStrategy
    {
     
        Contains,
        Match,
        Like,
        /// <summary>
        /// 首字母匹配
        /// </summary>
        Initials, 
    }
    public abstract class DBConnectorBase : PropertyChangeNotifier, IDataBaseConnector, IDictionarySerializable
    {
        #region Constructors and Destructors

        protected DBConnectorBase()
        {
            IsUseable = false;


            TableNames = new ExtendSelector<TableInfo>();
            TableNames.SetSource(new List<TableInfo>());
            AutoConnect = false;
        }

        protected virtual string Insert(IFreeDocument data, string dbTableName)
        {
            FreeDocument item = data.DictSerialize(Scenario.Database);
            var sb = new StringBuilder();
            foreach (var o in item)
            {
                string value;
                if (o.Value is DateTime)
                {
                    value = ((DateTime) o.Value).ToString("s");
                }
                else
                {
                    if (o.Value == null)
                    {
                        value = "null";
                    }
                    else
                    {
                        value = o.Value.ToString();
                    }
                }
                value = value.Replace("'", "''");
                sb.Append($"'{value}',");
            }
            sb.Remove(sb.Length - 1, 1);
            string sql = $"INSERT INTO {dbTableName} VALUES({sb})";
            return sql;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return TypeName;
            return Name;
        }


        public void SetObjects(IFreeDocument item, object[] value, string[] titles = null)
        {
            int index = 0;
            var values = new Dictionary<string, object>();
            if (item is IFreeDocument)
            {
                if (titles == null)
                {
                    foreach (object o in value)
                    {
                        values.Add(string.Format("Row{0}", index), value[index]);
                        index++;
                    }
                }
                else
                {
                    foreach (object o in value)
                    {
                        values.Add(titles[index], value[index]);
                        index++;
                    }
                }
            }
            else
            {
                foreach (string o in item.DictSerialize().Keys.OrderBy(d => d))
                {
                    values.Add(o, value[index]);
                    index++;
                }
            }

            item.DictDeserialize(values);
            ;
        }

        #endregion

        #region Properties

        private bool _IsUseable;
        private string name;

        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("表名")]
        public ExtendSelector<TableInfo> TableNames { get; set; }

        [LocalizedDisplayName("服务器地址")]
        [LocalizedCategory("1.连接管理")]
        [PropertyOrder(2)]
        public virtual  string Server { get; set; }

        [LocalizedDisplayName("用户名")]
        [LocalizedCategory("1.连接管理")]
        [PropertyOrder(3)]
        public virtual string UserName { get; set; }

        [LocalizedDisplayName("密码")]
        [LocalizedCategory("1.连接管理")]
        [PropertyOrder(4)]
      //  [PropertyEditor("PasswordEditor")]
        public virtual string Password { get; set; }

        [LocalizedCategory("参数设置")]
        [LocalizedDisplayName("数据库类型")]
        public string TypeName
        {
            get
            {
                XFrmWorkAttribute item = AttributeHelper.GetCustomAttribute(GetType());
                if (item == null)
                {
                    return GetType().ToString();
                }
                return item.Name;
            }
        }

        public virtual void CreateDataBase(string dbname)
        {
            ExecuteNonQuery($"CREATE DATABASE {dbname}");
        }

        public virtual List<FreeDocument> QueryEntities(string querySQL, out int count,
            string tablename = null)
        {
            count = 0;
            return new List<FreeDocument>();
        }

        [Browsable(false)]
        public virtual string ConnectionString { get; set; }


        [LocalizedCategory("1.连接管理")]
        [LocalizedDisplayName("数据库名称")]
        [PropertyOrder(2)]
        public  virtual string DBName { get; set; }

       

        public virtual bool CreateTable(IFreeDocument example, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new Exception("数据库表名不能为空");
            FreeDocument txt = example.DictSerialize(Scenario.Database);
            var sb = string.Join(",", txt.Select(d => $"{ ScriptHelper.RemoveSpecialCharacter(d.Key)} {DataTypeConverter.ToType(d.Value)}"));
            string sql = $"CREATE TABLE {GetTableName(name)} ({sb})";
            ExecuteNonQuery(sql);
            RefreshTableNames();
            return true;
        }


        [LocalizedCategory("1.连接管理")]
        [PropertyOrder(10)]
        [LocalizedDisplayName("连接状态")]
        public bool IsUseable
        {
            get { return _IsUseable; }
            protected set
            {
                if (_IsUseable != value)
                {
                    _IsUseable = value;
                    OnPropertyChanged("IsUseable");
                }
            }
        }


        public virtual List<FreeDocument> TryFindEntities(string tableName, IDictionary<string, object> search
           , List<string>keys,  int count = -1, DBSearchStrategy searchStrategy = DBSearchStrategy.Contains)
        {
         
            return new List<FreeDocument>();
        }

        [LocalizedCategory("1.连接管理")]
        [PropertyOrder(1)]
        [LocalizedDisplayName("连接名称")]
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        #endregion

        #region Implemented Interfaces

        #region IDataBaseConnector

        [LocalizedCategory("1.连接管理")]
        [PropertyOrder(5)]
        [LocalizedDisplayName("自动连接")]
        public bool AutoConnect { get; set; }

        public virtual void BatchInsert(IEnumerable<IFreeDocument> source, string dbTableName)
        {
            throw new NotImplementedException();
        }

        public virtual bool CloseDB()
        {
            IsUseable = false;
            return true;
        }

        public virtual bool ConnectDB()
        {
            IsUseable = true;
            return true;
        }

        public virtual void DropTable(string tableName)
        {
            try
            {
                string sql = $"DROP TABLE   {GetTableName(tableName)}";
                ExecuteNonQuery(sql);
                RefreshTableNames();
            }
            catch (Exception ex)
            {
                XLogSys.Print.Error($"数据库删除失败: {ex}" );
            }
        }


        public virtual IEnumerable<FreeDocument> GetEntities(string tableName, int mount = -1,
            int skip = 0)
        {
            string sql = null;
            if (mount == 0)
            {
                sql = $"Select * from {GetTableName(tableName)}";
            }
            else
            {
                sql = $"Select * from {tableName} LIMIT {mount} OFFSET {skip}";
            }


            DataTable data = GetDataTable(sql);
            return Table2Data(data);
        }


        public virtual List<TableInfo> RefreshTableNames()
        {
            DataTable items = GetDataTable("show tables");
            List<TableInfo> res =
                (from DataRow dr in items.Rows select new TableInfo(dr.ItemArray[0].ToString(), this)).ToList();
            TableNames.SetSource(res);
            return res;
        }

        public virtual void SaveOrUpdateEntity(
            IFreeDocument updateItem, string tableName,  IDictionary<string, object> keys,EntityExecuteType executeType=EntityExecuteType.InsertOrUpdate)
        {
            FreeDocument data = updateItem.DictSerialize(Scenario.Database);
            foreach (var key in data.Keys.ToList())
            {
                var value = "";
                if (data[key] != null)
                    value = data[key].ToString();
                value = value.Replace("'","''" );
                data[key] = value;
            }
            var str =",".Join( data.Select(d => $"{d.Key}='{d.Value}'"));            
           
            try
            {
                ExecuteNonQuery($"update {GetTableName(tableName)} set {str} ");
            }

            catch (Exception e)
            {
                XLogSys.Print.Debug($"insert database error {e.Message}");
            }
        }

        protected virtual void ConnectStringToOtherInfos()
        {
            try
            {
                var sqlConnBuilder = new SqlConnectionStringBuilder(ConnectionString);
                UserName = sqlConnBuilder.UserID;
                Password = sqlConnBuilder.Password;
                Server = sqlConnBuilder.DataSource;
            }
            catch (Exception)
            {
            }
        }

        protected virtual string GetConnectString()
        {
            var sqlConnBuilder = new SqlConnectionStringBuilder
            {
                DataSource = Server,
                UserID = UserName,
                Password = Password,
                InitialCatalog = DBName
            };
            return sqlConnBuilder.ConnectionString;
        }

        protected List<FreeDocument> Table2Data(DataTable data)
        {
            var result = new List<FreeDocument>();
            string[] titles = (from object column in data.Columns select column.ToString()).ToArray();
            foreach (DataRow dr in data.Rows)
            {
                var data2  =new FreeDocument();


                SetObjects(data2, dr.ItemArray, titles);
                result.Add(data2);
            }
            return result;
        }

        protected virtual string GetTableName(string tableName)
        {
            return  ScriptHelper.RemoveSpecialCharacter(tableName);
        }

        protected virtual DataTable GetDataTable(string sql)
        {
            return new DataTable();
        }

        protected virtual int ExecuteNonQuery(string sql)
        {
            return 0;
        }

        #endregion

        #endregion

        [LocalizedDisplayName("执行")]
        [PropertyOrder(20)]
        public virtual  ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("连接数据库", obj =>
                        {

                            ControlExtended.SafeInvoke(() => ConnectDB(), LogType.Important, "连接数据库");
                            if (IsUseable)
                            {
                                RefreshTableNames();
                            }
                        }, obj => IsUseable == false,"connect"),
                        new Command("关闭连接", obj => CloseDB(), obj => IsUseable,"close"),
                        new Command("创建新库", obj => CreateDataBase(DBName), obj => string.IsNullOrEmpty(DBName) == false,"add")
                    });
            }
        }

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = new FreeDocument();
            dict.Add("DBName", DBName);
            dict.Add("Name", Name);

            dict.Add("TypeName",this.GetType().Name );
            dict.Add("ConnectString", ConnectionString);
            dict.Add("AutoConnect", AutoConnect);
            return dict;
        }


        public virtual void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            DBName = docu.Set("DBName", DBName);
            Name = docu.Set("Name", Name);
            AutoConnect = docu.Set("AutoConnect", AutoConnect);
            ConnectionString = docu.Set("ConnectString", ConnectionString);

            ConnectStringToOtherInfos();
        }
    }
}