using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using MySql.Data.MySqlClient;

namespace XFrmWork.DataBase
{
    using System.Data;
    using System.Data.Common;




    [XFrmWork("MySQL数据库", "IDataBaseConnector", "提供MySQL交互的数据库服务", "")]
    public class MySQLConnector : DBConnectorBase
    {

        public override bool CloseDB()
        {
            dbConn.Close();
            return true;

        }
        
        
        public override void BatchInsert(IEnumerable<IFreeDocument> source, string dbTableName)
        {

            var sqlStringList = source.Select(d => this.Insert(d, dbTableName)).ToList();
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = dbConn;
            MySqlTransaction tx = dbConn.BeginTransaction();
            cmd.Transaction = tx;
            try
            {
                for (int n = 0; n < sqlStringList.Count; n++)
                {
                    string strsql = sqlStringList[n];
                    if (strsql.Trim().Length > 1)
                    {
                        cmd.CommandText = strsql;
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            XLogSys.Print.Debug($"batch insert database error {ex.Message}");
                        }
                    
                    }
                    //后来加上的  
                    if (n > 0 && (n % 500 == 0 || n == sqlStringList.Count - 1))
                    {
                        tx.Commit();
                        tx = dbConn.BeginTransaction();
                    }
                }
                //tx.Commit();//原来一次性提交  
            }
            catch (System.Data.SqlClient.SqlException e)
            {
                XLogSys.Print.Debug($"insert mysql database error {e.Message}");
                tx.Rollback();
                
            }
        }
        public override bool CreateTable(IFreeDocument example, string name)
        {
           //  
            var txt = example.DictSerialize(Scenario.Database);
            var sb = new StringBuilder();
            foreach (var o in txt)
            {

                sb.Append(o.Key);
                sb.AppendFormat(" {0},", DataTypeConverter.ToType(o.Value));

            }
            sb.Remove(sb.Length - 1, 1);
            var sql = string.Format("CREATE TABLE {0} ({1}) default charset utf8 COLLATE utf8_general_ci", name, sb.ToString());
            ExecuteNonQuery(sql);
            this.RefreshTableNames();
            return true;
        
        }
        private MySqlConnection dbConn;
        public override bool ConnectDB()
        {
            if (this.dbConn != null && this.dbConn.State == ConnectionState.Open)
            {
                this.dbConn.Close();
            }
            var sql = GetConnectString();
            this.dbConn = new MySqlConnection(sql);
            try
            {
                this.dbConn.Open();

                IsUseable = true;
            }
            catch (Exception ex)
            {

                IsUseable = false;

            }
            return IsUseable;
        }
        protected override int ExecuteNonQuery(string sql)
        {
            var dbComm = new MySqlCommand(sql, dbConn);

            return dbComm.ExecuteNonQuery();
        
        
        
        
        
        
        }

        private void Execute(string sql, Action<DbDataRecord> recordAction)
        {
              var dbComm = new MySqlCommand("show tables", dbConn);

         
            using (var sdr = dbComm.ExecuteReader())
            {
                foreach (var dr in sdr)
                {
                    var item = dr as
                    DbDataRecord;
                    recordAction(item);
                }
            }
        }

        private void GetTableInfo(TableInfo table)
        {
            table.ColumnInfos.Clear();
            Execute($"desc {table.Name}", d => table.ColumnInfos.Add(new ColumnInfo(d.GetString(0))
            {
                DataType = GetType(d.GetString(1)).ToString(),
                CanNull=  d.GetString(2)=="YES"?true:false,
                IsKey = d.GetString(3).Contains("PRI"),

            }));
        }

        private SimpleDataType GetType(string name)
        {
            if(name.Contains("int")||name.Contains("year"))
                return SimpleDataType.INT;
            if (name.Contains("float") ||name.Contains("double")  )
                return SimpleDataType.DOUBLE;
            if (name.Contains("varchar") || name.Contains("nvarchar"))
                return SimpleDataType.STRING;

            if (name.Contains("enum")  )
                return SimpleDataType.ENUMS;
            if (name.Contains("timestamp"))
                return SimpleDataType.TIMESPAN;
            return SimpleDataType.STRING;
        }
        public override List<TableInfo> RefreshTableNames()
        {
            if (IsUseable == false) return new List<TableInfo>();
            var dbComm = new MySqlCommand("show tables", dbConn);

            List<TableInfo> res = new List<TableInfo>();
            Execute("show tables", d => res.Add(new TableInfo(d.GetValue(0).ToString(), this) ));
            TableNames.SetSource(res);
        //    res.Execute(GetTableInfo);
        
          
            return res;
        }


        protected override DataTable GetDataTable(string sql)
        {
            MySqlCommand dbComm = new MySqlCommand(sql, dbConn);

            DataTable dt = new DataTable();
            MySqlDataReader sdr = dbComm.ExecuteReader();
            if (sdr.HasRows)
                dt.Load(sdr);
            sdr.Close();
            return dt;
        }
    }
}
