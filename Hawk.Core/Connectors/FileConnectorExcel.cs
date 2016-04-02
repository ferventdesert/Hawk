using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Hawk.Core.Connectors
{
    [XFrmWork("EXCEL导入导出器",  "输出标准EXCEL文件，效率较低", "")]
    public class FileConnectorExcel : FileConnector
    {
        #region Properties

        public override string ExtentFileName => ".xlsx";

        #endregion

        #region Methods

        public override IEnumerable<IDictionarySerializable> ReadFile(Action<int> alreadyGetSize = null)
        {
            XSSFWorkbook hssfworkbook;

            using (var file = new FileStream(FileName, FileMode.Open, FileAccess.Read))
            {
                hssfworkbook = new XSSFWorkbook(file);
            }

          
            ISheet sheet = hssfworkbook.GetSheetAt(0);


            List<string> titles = null;
            try
            {
               titles= sheet.GetRow(0).Cells.Select(d => d.StringCellValue).ToList();

            }
            catch (Exception ex)
            {
                
                 throw  new Exception("请填写Excel的表头信息");
            }
          
            for (int i = 1; i < sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                var data = PluginProvider.GetObjectInstance(DataType) as IDictionarySerializable;
                var dict = new Dictionary<string, object>();
                for (int index = 0; index < titles.Count; index++)
                {
                    string title = titles[index];
                    dict.Add(title, row.GetCell(index).ToString());
                }
                var freeDocument = data as IFreeDocument;
                if (freeDocument != null)
                {
                    if (i == 1)
                    {
                        PropertyNames = titles.ToDictionary(d => d, d => d);
                    }
                }
                data.DictDeserialize(dict);
                yield return data;
                if(i%1000==0)
                    XLogSys.Print.Info($"已经导入数量{i}，总共{sheet.LastRowNum}");
            }
        }

        public override IEnumerable<IDictionarySerializable> WriteData(IEnumerable<IDictionarySerializable> datas)
        {
           
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet1 = workbook.CreateSheet("Sheet1");
            FileStream sw = File.Create(FileName);
            using (var dis = new DisposeHelper(() =>
            {
                workbook.Write(sw);
                sw.Close();
            }))
            {

                int rowIndex = 0;
                foreach (IDictionarySerializable computeable in datas)
                {
                    IDictionary<string, object> data = computeable.DictSerialize();
                    int cellIndex;

                    if (rowIndex == 0)
                    {
                        IRow row1 = sheet1.CreateRow(rowIndex);
                        cellIndex = 0;
                        foreach (var  o in this.PropertyNames)
                        {


                            row1.CreateCell(cellIndex).SetCellValue(o.Value);
                            cellIndex++;
                        }

                         rowIndex++;
                    }
                    cellIndex = 0;
                    IRow row = sheet1.CreateRow(rowIndex);
                    foreach (object value in this.PropertyNames.Select(name => data[name.Key]))
                    {
                        if (value is DateTime)
                        {
                            row.CreateCell(cellIndex).SetCellValue((value.ToString()));
                        }
                        else if (value is int || value is long)
                        {
                            row.CreateCell(cellIndex).SetCellValue(value.ToString());
                        }
                        else if (value is double)
                        {
                            row.CreateCell(cellIndex).SetCellValue((double) value);
                        }
                        else if (value is string)
                        {
                            row.CreateCell(cellIndex).SetCellValue((string) value);
                        }
                        else
                        {
                            row.CreateCell(cellIndex).SetCellValue(value != null ? value.ToString() : "");
                        }
                        cellIndex++;
                    }
                    rowIndex++;
                    yield return computeable;
                
                
                }
            }

           
        }

        #endregion
    }
}