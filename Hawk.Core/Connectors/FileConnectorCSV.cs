using System.Collections.Generic;
using System.IO;
using System.Text;
using Hawk.Core.Utils.Plugins;
using Microsoft.Win32;

namespace Hawk.Core.Connectors
{
    [XFrmWork("CSV导入导出器",  "输出文本型CSV逗号分隔文件", "")]
    public class FileConnectorCSV : FileConnectorTable
    {
        #region Properties

          public FileConnectorCSV()
          {
              SplitString = ",";
          }

          public override string ExtentFileName
        {
            get
            {
                return ".csv";
            }
        }
        protected  override  string SplitChar
        {
            get
            {
                return ",";
            }
        }
        #endregion

        public static void CSVToDataTable(List<string> title, List<string[]> datas, string fileName, char split = ',')
        {

            string strpath = fileName; //csv文件的路径

            int intColCount = 0;
            bool blnFlag = true;

            string strline;

            var mysr = new StreamReader(strpath, Encoding.Default);

            while ((strline = mysr.ReadLine()) != null)
            {
                string[] aryline = strline.Split(new[] { split });

                //给datatable加上列名
                if (blnFlag)
                {
                    blnFlag = false;
                    intColCount = aryline.Length;

                    title.AddRange(aryline);
                }
                else
                {
                    var objs = new string[intColCount];
                    for (int i = 0; i < intColCount; i++)
                    {
                        objs[i] = aryline[i].Trim();
                    }
                    datas.Add(objs);
                }
            }
        }

        public static bool DataTableToCSV(ICollection<string> titles, IEnumerable<object[]> datas, char split = ',')
        {
            var ofd = new SaveFileDialog { DefaultExt = ".csv", Filter = "Excel格式文件(*.csv)|*.csv" };

            string fileName = null;
            if (ofd.ShowDialog() == true)
            {
                fileName = ofd.FileName;
            }
            if (fileName == null)
            {
                return false;
            }

            FileConnectorTable.DataTableToCSV(titles, datas, fileName, split);
            return true;

        }




        #region Methods

      

       
        #endregion
    }
}