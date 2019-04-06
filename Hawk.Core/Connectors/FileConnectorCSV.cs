using System.Collections.Generic;
using System.IO;
using System.Text;
using Hawk.Core.Utils.Plugins;
using Microsoft.Win32;

namespace Hawk.Core.Connectors
{
    [XFrmWork("FileConnectorCSV", "FileConnectorCSV_desc", "")]
    public class FileConnectorCSV : FileConnectorTable
    {
        public static void CSVToDataTable(List<string> title, List<string[]> datas, string fileName, char split = ',')
        {
            var strpath = fileName; //csv文件的路径

            var intColCount = 0;
            var blnFlag = true;

            string strline;

            var mysr = new StreamReader(strpath, Encoding.Default);

            while ((strline = mysr.ReadLine()) != null)
            {
                var aryline = strline.Split(split);

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
                    for (var i = 0; i < intColCount; i++)
                    {
                        objs[i] = aryline[i].Trim();
                    }
                    datas.Add(objs);
                }
            }
        }

        public static bool DataTableToCSV(ICollection<string> titles, IEnumerable<object[]> datas, char split = ',')
        {
            var ofd = new SaveFileDialog {DefaultExt = ".csv", Filter = "Excel格式文件(*.csv)|*.csv"};

            string fileName = null;
            if (ofd.ShowDialog() == true)
            {
                fileName = ofd.FileName;
            }
            if (fileName == null)
            {
                return false;
            }

            DataTableToCSV(titles, datas, fileName, split);
            return true;
        }

        #region Properties

        public FileConnectorCSV()
        {
            SplitString = ",";
        }

        public override string ExtentFileName => ".csv";

        protected override string SplitChar => ",";

        #endregion

        #region Methods

        #endregion
    }
}