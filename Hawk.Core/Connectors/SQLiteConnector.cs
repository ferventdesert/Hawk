using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Forms;
using System.Windows.Input;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    [XFrmWork("SQLiteDatabase", "SQLiteDatabase_desc", "")]
    public class SQLiteDatabase : DBConnectorBase
    {
        private string _dbName;

        #region Methods

        /// <summary>
        ///     Allows the programmer to run a query against the Database.
        /// </summary>
        /// <param name="sql">The SQL to run</param>
        /// <returns>A DataTable containing the result set.</returns>
        protected override DataTable GetDataTable(string sql)
        {
            var dt = new DataTable();

            try
            {
                var cnn = new SQLiteConnection(ConnectionString);

                cnn.Open();

                var mycommand = new SQLiteCommand(cnn);

                mycommand.CommandText = sql;

                var reader = mycommand.ExecuteReader();

                dt.Load(reader);

                reader.Close();

                cnn.Close();
            }

            catch (Exception e)
            {
                XLogSys.Print.Error(GlobalHelper.FormatArgs("key_81", e.Message));
            }

            return dt;
        }

        #endregion

        #region Constants and Fields

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Default Constructor for SQLiteDatabase Class.
        /// </summary>
        public SQLiteDatabase()
        {
            _dbName = "hawk.db";
        }

        [Browsable(false)]
        [LocalizedDisplayName("key_23")]
        [LocalizedCategory("key_24")]
        [PropertyOrder(2)]
        public override string Server { get; set; }

        [Browsable(false)]
        [LocalizedDisplayName("key_25")]
        [LocalizedCategory("key_24")]
        [PropertyOrder(3)]
        public override string UserName { get; set; }

        [Browsable(false)]
        [LocalizedDisplayName("key_26")]
        [LocalizedCategory("key_24")]
        [PropertyOrder(4)]
        //  [PropertyEditor("PasswordEditor")]
        public override string Password { get; set; }

        [LocalizedCategory("key_24")]
        [LocalizedDisplayName("key_82")]
        [PropertyOrder(3)]
        public ReadOnlyCollection<ICommand> Commands2
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_83"), obj => LoadOldDB(), icon: "disk"),
                        new Command(GlobalHelper.Get("key_84"), obj => CreateNewDB(), icon: "add")
                    });
            }
        }

        private void CreateNewDB()
        {
            var saveTifFileDialog = new SaveFileDialog();
            saveTifFileDialog.OverwritePrompt = true; //询问是否覆盖
            saveTifFileDialog.Filter = "*.db|*.db";
            saveTifFileDialog.DefaultExt = "db"; //缺省默认后缀名
            if (saveTifFileDialog.ShowDialog() == DialogResult.OK)
                DBName = saveTifFileDialog.FileName;
            SafeConnectDB();
        }

        private void LoadOldDB()
        {
            var dialog = new OpenFileDialog();
            dialog.Multiselect = false; //该值确定是否可以选择多个文件
            dialog.Title = GlobalHelper.Get("key_85");
            dialog.Filter = "所有文件(*.*)|*.db";
            if (dialog.ShowDialog() == DialogResult.OK)
                DBName = dialog.FileName;
            SafeConnectDB();
        }

        [LocalizedCategory("key_24")]
        [LocalizedDisplayName("key_86")]
        [LocalizedDescription("key_87")]
        [PropertyOrder(2)]
        public override string DBName
        {
            get { return _dbName; }
            set
            {
                if (_dbName != value)
                {
                    _dbName = value;
                    OnPropertyChanged("DBName");
                }
            }
        }

        public override void DropTable(string tableName)
        {
            var sql = $"Drop TABLE {GetTableName(tableName)}";
            GetDataTable(sql);
        }

        [LocalizedDisplayName("key_34")]
        [PropertyOrder(20)]
        public override ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("connect_db"), obj => { SafeConnectDB(); }, obj => IsUseable == false, "connect"),
                        new Command(GlobalHelper.Get("key_36"), obj => CloseDB(), obj => IsUseable, "close")
                    });
            }
        }

        private void SafeConnectDB()
        {
            ControlExtended.SafeInvoke(() => ConnectDB(), LogType.Important, GlobalHelper.Get("connect_db"));
            if (IsUseable)
                RefreshTableNames();
        }

        protected override string GetConnectString()
        {
            return "data source=" + DBName;
        }

        protected override void ConnectStringToOtherInfos()
        {
            var connectstrs = ConnectionString.Split('=');
            if (connectstrs.Length > 1)
                DBName = connectstrs[1];
        }

        #endregion

        #region Public Methods

        public override void BatchInsert(IEnumerable<IFreeDocument> source,List<string>keys, string dbTableName)
        {
            using (var cnn = new SQLiteConnection(ConnectionString))
            {
                cnn.Open();

                using (var mytrans = cnn.BeginTransaction())
                {
                    foreach (var data in source)
                    {
                        try
                        {
                            var sql = Insert(data,keys, dbTableName);
                            var mycommand = new SQLiteCommand(sql, cnn, mytrans);
                            mycommand.CommandTimeout = 180;
                            mycommand.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            XLogSys.Print.Warn($"insert sqlite database error {ex.Message}");
                        }

                        mytrans.Commit();

                        cnn.Close();
                    }
                }
            }
        }


        public override bool CloseDB()
        {
            IsUseable = false;

            return true;
        }

        public override bool ConnectDB()
        {
            ConnectionString = GetConnectString();
            var cnn = new SQLiteConnection(ConnectionString);

            try
            {
                cnn.Open();
                IsUseable = true;
                cnn.Close();
                return true;
            }
            catch (Exception ex)
            {
                IsUseable = false;
                XLogSys.Print.Debug($"connect database error {ex}");
                return false;
            }
        }

        public override bool CreateTable(IFreeDocument example, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new Exception(GlobalHelper.Get("key_29"));
            var txt = example.DictSerialize(Scenario.Database);
            var sb = string.Join(",", txt.Select(d => $"{d.Key} {DataTypeConverter.ToType(d.Value)}"));
            var sql = $"CREATE TABLE {GetTableName(name)} ({sb})";
            ExecuteNonQuery(sql);
            RefreshTableNames();
            return true;
        }


        /// <summary>
        ///     Allows the programmer to interact with the database for purposes other than a query.
        /// </summary>
        /// <param name="sql">The SQL to be run.</param>
        /// <returns>An Integer containing the number of rows updated.</returns>
        protected override int ExecuteNonQuery(string sql)
        {
            using (var cnn = new SQLiteConnection(ConnectionString))
            {
                cnn.Open();

                using (var mytrans = cnn.BeginTransaction())
                {
                    return ExecuteNonQuery(sql, cnn );
                }
            }

        }


        protected int ExecuteNonQuery(string sql, SQLiteConnection cnn)
        {
            var col = 0;


            var mycommand = new SQLiteCommand(sql, cnn);

            try
            {
                mycommand.CommandTimeout = 180;

                col = mycommand.ExecuteNonQuery();

            }

            catch (Exception e)
            {
                XLogSys.Print.Warn(GlobalHelper.Get("key_88")+ e.Message+ GlobalHelper.Get("key_89") + sql );
            }

            finally
            {
                mycommand.Dispose();
            }
            return col;
        }


        public override List<FreeDocument> QueryEntities(string querySQL, out int count, string tableName = null)
        {
            var table = GetDataTable(querySQL);
            var datas = Table2Data(table);
            count = datas.Count;
            return datas;
        }

        public bool ExecuteNonQuery(string sql, IList<SQLiteParameter> cmdparams)
        {
            var successState = false;
            var cnn = new SQLiteConnection(ConnectionString);
            cnn.Open();

            using (var mytrans = cnn.BeginTransaction())
            {
                var mycommand = new SQLiteCommand(sql, cnn, mytrans);

                try
                {
                    mycommand.Parameters.AddRange(cmdparams.ToArray());

                    mycommand.CommandTimeout = 180;

                    mycommand.ExecuteNonQuery();

                    mytrans.Commit();

                    successState = true;

                    cnn.Close();
                }

                catch (Exception e)
                {
                    mytrans.Rollback();

                    throw e;
                }

                finally
                {
                    mycommand.Dispose();

                    cnn.Close();
                }
            }

            return successState;
        }

        /// <summary>
        ///     暂时用不到
        ///     Allows the programmer to retrieve single items from the DB.
        /// </summary>
        /// <param name="sql">The query to run.</param>
        /// <returns>A string.</returns>
        public string ExecuteScalar(string sql)
        {
            /* this.cnn.Open();

             var mycommand = new SQLiteCommand(this.cnn);

             mycommand.CommandText = sql;

             object value = mycommand.ExecuteScalar();

             this.cnn.Close();

             if (value != null)
             {
                 return value.ToString();
             }

             return "";*/
            return "";
        }

        protected DataTable GetDataTable(string sql, IList<SQLiteParameter> cmdparams)
        {
            var dt = new DataTable();

            try
            {
                var cnn = new SQLiteConnection(ConnectionString);

                cnn.Open();

                var mycommand = new SQLiteCommand(cnn);

                mycommand.CommandText = sql;

                mycommand.Parameters.AddRange(cmdparams.ToArray());

                mycommand.CommandTimeout = 180;

                var reader = mycommand.ExecuteReader();

                dt.Load(reader);

                reader.Close();

                cnn.Close();
            }

            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            return dt;
        }


        public override IEnumerable<FreeDocument> GetEntities(string tableName, int mount = 0, int skip = 0)
        {
            string sql = null;
            if (mount == 0)
                sql = $"Select * from {tableName}";
            else
                sql = $"Select * from {tableName} LIMIT {mount} OFFSET {skip}";


            var data = GetDataTable(sql);
            return Table2Data(data);
        }


        public override List<TableInfo> RefreshTableNames()
        {
            var items = GetDataTable("select name from sqlite_master where type='table' order by name");
            var res = new List<TableInfo>();
            foreach (DataRow dr in items.Rows)
            {
                var name = dr.ItemArray[0].ToString();
                var size = GetDataTable($"select count(*) from  {name}").Rows[0].ItemArray[0];
                res.Add(new TableInfo(name, this) {Size = int.Parse(size.ToString())});
            }
            TableNames.SetSource(res);
            return res;
        }


        /// <summary>
        ///     Allows the programmer to easily update rows in the DB.
        /// </summary>
        /// <param name="tableName">The table to update.</param>
        /// <param name="data">A dictionary containing Column names and their new values.</param>
        /// <param name="where">The where clause for the update statement.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Update(string tableName, Dictionary<string, string> data, string where)
        {
            var vals = "";

            var returnCode = true;

            if (data.Count >= 1)
            {
                vals = data.Aggregate(vals, (current, val) => current + $" {val.Key} = '{val.Value}',");

                vals = vals.Substring(0, vals.Length - 1);
            }

            try
            {
                ExecuteNonQuery($"update {tableName} set {vals} where {where};");
            }

            catch
            {
                returnCode = false;
            }

            return returnCode;
        }

        #endregion
    }
}