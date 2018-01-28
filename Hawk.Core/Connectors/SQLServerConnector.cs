using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    // [XFrmWork("SQLServer数据库",  "提供SQLServer交互的数据库服务", "")]
    [XFrmWorkIgnore]
    public class SQLServerConnector : DBConnectorBase
    {
        //用于连接sql server

        //执行语句

        #region Constants and Fields

        #endregion

        #region Properties

        #endregion

        // Public Methods (18) 

        #region Public Methods

        public override void BatchInsert(IEnumerable<IFreeDocument> insertItems, string tableName)
        {
            var sqlConn = new SqlConnection(
                ConnectionString); //连接数据库

            var sqlComm = new SqlCommand();
            sqlComm.CommandType = CommandType.Text;
            sqlComm.Connection = sqlConn;
            sqlConn.Open();
            try
            {
                foreach (var dictionarySerializable in insertItems)
                {
                    sqlComm.CommandText = Insert(dictionarySerializable, tableName);
                    sqlComm.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                sqlConn.Close();
            }
        }

        /// <summary>
        ///     关闭连接
        /// </summary>
        /// <returns></returns>
        public override bool CloseDB() //关闭连接
        {
            if (IsUseable)
            {
                IsUseable = false;
                return true;
            }
            return false;
        }

        public override bool ConnectDB() //数据库初始化
        {
            using (var sqlCon = new SqlConnection(ConnectionString))
            {
                try
                {
                    sqlCon.Open();
                    IsUseable = true;
                }
                catch (Exception)
                {
                    IsUseable = false;
                }
            }


            return IsUseable;
        }


        ///// <summary>
        ///// 删除表中满足特定条件的一行数据
        ///// </summary>
        ///// <param name="utablename">表名称</param>
        ///// <param name="Keyitem">主键名称</param>
        ///// <param name="itemtext">列中的值</param>
        //public void DeleteRow(string utablename, object Keyitem, object itemtext)
        //{
        //    string state = this.sqlCon.State.ToString();
        //    if (state == "Closed")
        //    {
        //        this.IsUseable = true;
        //        this.sqlCon.Open();
        //    }
        //    this.strexce = string.Format("delete from dbo.{0}  where {1}='{2}'", utablename, Keyitem, itemtext);
        //    var cmd = new SqlCommand(this.strexce, this.sqlCon);
        //    cmd.ExecuteNonQuery();
        //    this.sqlCon.Close();
        //}


        public bool IsChina(object obj)
        {
            if (!(obj is string))
            {
                return false;
            }
            var cString = (string) obj;
            for (var i = 0; i < cString.Length; i++)
            {
                if (Convert.ToInt32(Convert.ToChar(cString.Substring(i, 1))) < Convert.ToInt32(Convert.ToChar(128)))
                {
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public override List<TableInfo> RefreshTableNames()
        {
            var table = GetDataTable("select name from sysobjects where xtype='u'");
            var names =
                (from DataRow dr1 in table.Rows select new TableInfo(dr1.ItemArray[0].ToString(), this)).ToList();
            TableNames.SetSource(names);
            return names;
        }

        #endregion

        #region Methods

        protected override DataTable GetDataTable(string sql)
        {
            if (IsUseable == false) return new DataTable();
            using (var sqlCon = new SqlConnection(ConnectionString))
            {
                var dataAda = new SqlDataAdapter(sql, sqlCon);
                var table = new DataTable();
                dataAda.Fill(table);
                return table;
            }
        }

        protected override string GetTableName(string tableName)
        {
            return "dbo." + tableName;
        }

        /*    private void GetPrimaryKey(DataSet daset, string tablename, string key)
        {
            if (this.sqlCon.State == ConnectionState.Closed)
            {
                this.sqlCon.Open();
            }
            this.strexce = "select distinct " + key + " from dbo." + tablename;
            this.dataAda = new SqlDataAdapter(this.strexce, this.sqlCon);
            //data = new DataSet();
            this.dataAda.Fill(daset, key);
            this.sqlCon.Close();
        }
*/
        // Private Methods (1) 

        ///// <summary>
        ///// 向表格中插入一行新数据
        ///// </summary>
        ///// <param name="utablename">表名称</param>
        ///// <param name="itemtext1">第一项数据</param>
        ///// <param name="itemtext2">第二项数据</param>
        //private void InsertTable(object[] items)
        //{
        //    this.state = this.sqlCon.State.ToString();
        //    if (this.state == "Closed")
        //    {
        //        this.sqlCon.Open();
        //    }
        //    int mount = items.Count();

        //    string temp = "values(";
        //    for (int i = 1; i < mount - 1; i++)
        //    {
        //        if (this.IsChina(items[i]))
        //        {
        //            temp += "N'{" + i.ToString() + "}' ,";
        //        }
        //        else
        //        {
        //            temp += "'{" + i.ToString() + "}' ,";
        //        }
        //    }

        //    temp += "'{" + (mount - 1).ToString() + "}')";
        //    this.strexce = string.Format("insert into dbo.{0}  " + temp, items);
        //    var cmd = new SqlCommand(this.strexce, this.sqlCon);
        //    cmd.ExecuteNonQuery();
        //}

        #endregion
    }
}