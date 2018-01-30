using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Forms;
using System.Windows.Input;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

namespace XFrmWork.DataVisualization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SQLite;
    using System.Linq;
    using System.Text;

 

    [XFrmWork("SQLite数据库",  "提供SQLite交互的数据库服务", "")]
    public class SQLiteDatabase : DBConnectorBase
    {
        private string _dbName;

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
        [LocalizedDisplayName("服务器地址")]
        [LocalizedCategory("1.连接管理")]
        [PropertyOrder(2)]
        public override string Server { get; set; }
        [Browsable(false)]
        [LocalizedDisplayName("用户名")]
        [LocalizedCategory("1.连接管理")]
        [PropertyOrder(3)]
        public override string UserName { get; set; }

        [Browsable(false)]
        [LocalizedDisplayName("密码")]
        [LocalizedCategory("1.连接管理")]
        [PropertyOrder(4)]
        //  [PropertyEditor("PasswordEditor")]
        public override string Password { get; set; }

        [LocalizedCategory("1.连接管理")]
        [LocalizedDisplayName("浏览路径")]

        [PropertyOrder(3)]
        public ReadOnlyCollection<ICommand> Commands2 {
            get
            {
                return CommandBuilder.GetCommands(
                     this,
                     new[]
                     {
                     
                        new Command("加载", obj => LoadOldDB(),icon:"disk"),
                            new Command("新建", obj => CreateNewDB(),icon:"add"),
                     });
            }
        }

        private void CreateNewDB()
        {
            SaveFileDialog saveTifFileDialog = new SaveFileDialog();
            saveTifFileDialog.OverwritePrompt = true;//询问是否覆盖
            saveTifFileDialog.Filter = "*.db|*.db";
            saveTifFileDialog.DefaultExt = "db";//缺省默认后缀名
            if (saveTifFileDialog.ShowDialog() == DialogResult.OK)
            {
                DBName = saveTifFileDialog.FileName;
            }
        }

        private void LoadOldDB()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;//该值确定是否可以选择多个文件
            dialog.Title = "请选择sqlite数据库文件";
            dialog.Filter = "所有文件(*.*)|*.db";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                DBName = dialog.FileName;
            }
        }

        [LocalizedCategory("1.连接管理")]
        [LocalizedDisplayName("数据库路径")]
        [LocalizedDescription("例如d:\\test\\mydb.sqlite")]
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

        [LocalizedDisplayName("执行")]
        [PropertyOrder(20)]
        public override ReadOnlyCollection<ICommand> Commands
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
                    });
            }
        }

        protected override string GetConnectString()
        {

            return  "data source="+this.DBName;
        }
        protected override void ConnectStringToOtherInfos()
        {
            var connectstrs = ConnectionString.Split('=');
            if (connectstrs.Length > 1)
                DBName = connectstrs[1];
        }

        #endregion

        #region Public Methods

        public override void BatchInsert(IEnumerable<IFreeDocument>source, string dbTableName)
        {
            var cnn = new SQLiteConnection(this.ConnectionString);
            cnn.Open();

            try
            {


                using (SQLiteTransaction mytrans = cnn.BeginTransaction())
                {
                    foreach (var data in source.Init(d =>
                    {
                        if(this.TableNames.Collection.FirstOrDefault(d2=>d2.Name==dbTableName)==null)
                             CreateTable(d, dbTableName);
                        return true;
                    }))
                    {
                        try
                        {
                            var sql = this.Insert(data, dbTableName);
                            var mycommand = new SQLiteCommand(sql, cnn, mytrans);
                            mycommand.CommandTimeout = 180;
                            mycommand.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            XLogSys.Print.Warn($"insert sqllite database error {ex.Message}");
                        }

                    }

                    mytrans.Commit();

                    cnn.Close();
                }

            }

         

            finally
            {


                cnn.Close();
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
            var cnn = new SQLiteConnection(this.ConnectionString);

            try
            {
                cnn.Open();
                this.IsUseable = true;
                cnn.Close();
                return true;
            }
            catch (Exception ex)
            {
                this.IsUseable = false;
                XLogSys.Print.Debug($"connect database error {ex}");
                return false;
            }
        }

        

        

        /// <summary>
        ///     Allows the programmer to interact with the database for purposes other than a query.
        /// </summary>
        /// <param name="sql">The SQL to be run.</param>
        /// <returns>An Integer containing the number of rows updated.</returns>
        protected override int ExecuteNonQuery(string sql)
        {
            int col = 0;
            var cnn = new SQLiteConnection(ConnectionString);
            cnn.Open();

            using (SQLiteTransaction mytrans = cnn.BeginTransaction())
            {
                var mycommand = new SQLiteCommand(sql, cnn);

                try
                {
                    mycommand.CommandTimeout = 180;

                   col= mycommand.ExecuteNonQuery();

                    mytrans.Commit();

                 

                    cnn.Close();
                }

                catch (Exception e)
                {
                    XLogSys.Print.Warn($"sql执行错误 {e.Message}  sql= {sql} ");
                    mytrans.Rollback();
                }

                finally
                {
                    mycommand.Dispose();

                    cnn.Close();
                }
            }

            return col;
        }

        public override List<FreeDocument> QueryEntities(string querySQL,out int count, string tableName=null)
        {
            var table = GetDataTable(querySQL);
            var datas = Table2Data(table );
            count = datas.Count;
            return datas;

        }
        public bool ExecuteNonQuery(string sql, IList<SQLiteParameter> cmdparams)
        {
            bool successState = false;
            var cnn = new SQLiteConnection(this.ConnectionString);
            cnn.Open();

            using (SQLiteTransaction mytrans = cnn.BeginTransaction())
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
                var cnn = new SQLiteConnection(this.ConnectionString);

                cnn.Open();

                var mycommand = new SQLiteCommand(cnn);

                mycommand.CommandText = sql;

                mycommand.Parameters.AddRange(cmdparams.ToArray());

                mycommand.CommandTimeout = 180;

                SQLiteDataReader reader = mycommand.ExecuteReader();

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



        public override IEnumerable<FreeDocument> GetEntities(string tableName,int mount = 0, int skip = 0)
        {
            string sql = null;
            if (mount == 0)
            {
                sql = $"Select * from {tableName}";
            }
            else
            {
                sql = $"Select * from {tableName} LIMIT {mount} OFFSET {skip}";
            }


            var data = GetDataTable(sql);
            return Table2Data(data);

        }

   
 

        public override List<TableInfo> RefreshTableNames()
        {

            var items = GetDataTable("select name from sqlite_master where type='table' order by name");
            List<TableInfo> res = new List<TableInfo>();
            foreach (DataRow dr in items.Rows)
            {


                res.Add( new TableInfo(dr.ItemArray[0].ToString(),this));
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
        public bool Update(String tableName, Dictionary<String, String> data, String where)
        {
            String vals = "";

            Boolean returnCode = true;

            if (data.Count >= 1)
            {
                vals = data.Aggregate(vals, (current, val) => current + $" {val.Key} = '{val.Value}',");

                vals = vals.Substring(0, vals.Length - 1);
            }

            try
            {
                this.ExecuteNonQuery($"update {tableName} set {vals} where {@where};");
            }

            catch
            {
                returnCode = false;
            }

            return returnCode;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Allows the programmer to run a query against the Database.
        /// </summary>
        /// <param name="sql">The SQL to run</param>
        /// <returns>A DataTable containing the result set.</returns>
        protected  override  DataTable GetDataTable(string sql)
        {

            var dt = new DataTable();

            try
            {
                var cnn = new SQLiteConnection(this.ConnectionString);

                cnn.Open();

                var mycommand = new SQLiteCommand(cnn);

                mycommand.CommandText = sql;

                SQLiteDataReader reader = mycommand.ExecuteReader();

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

        #endregion
    }
}