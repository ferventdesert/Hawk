using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;

namespace Hawk.Core.Connectors
{
    [XFrmWork("EXCEL导入导出器", "输出标准EXCEL文件，效率较低", "")]
    public class FileConnectorExcel : FileConnector
    {
        #region Properties

        public override string ExtentFileName => ".xlsx";

        #endregion

        #region Methods

        public override IEnumerable<FreeDocument> ReadFile(Action<int> alreadyGetSize = null)
        {
            XSSFWorkbook hssfworkbook;

            using (var file = new FileStream(FileName, FileMode.Open, FileAccess.Read))
            {
                hssfworkbook = new XSSFWorkbook(file);
            }



            var sheet = hssfworkbook.GetSheetAt(0);

            List<string> titles = null;
            try
            {
                titles = sheet.GetRow(0).Cells.Select(d => d.StringCellValue).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("请填写Excel的表头信息");
            }

            for (var i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                var data = new FreeDocument();
                var dict = new Dictionary<string, object>();
                for (var index = 0; index < titles.Count; index++)
                {
                    var title = titles[index];
                    var cell = row.GetCell(index);
                    string value = "";
                    if (cell != null)
                        value = cell.ToString();
                    dict.Set(title, value);
                }

                if (data != null)
                {
                    if (i == 1)
                    {
                        PropertyNames = titles.ToDictionary(d => d, d => d);
                    }
                }
                data.DictDeserialize(dict);
                yield return data;
                if (i%1000 == 0)
                    XLogSys.Print.Info($"已经导入数量{i}，总共{sheet.LastRowNum}");
            }
        }

        public override IEnumerable<IFreeDocument> WriteData(IEnumerable<IFreeDocument> datas)
        {
            var xssfWb = new XSSFWorkbook();
            var wb = new SXSSFWorkbook(xssfWb, 1000);
// IWorkbook workbook = new XSSFWorkbook();
            var sheet1 = xssfWb.CreateSheet("Sheet1");
            var sw = File.Create(FileName);
            using (var dis = new DisposeHelper(() =>
            {
                wb.Write(sw);
                sw.Close();
            }))
            {
                var rowIndex = 0;
                PropertyNames = datas.GetKeys().ToDictionary(d => d, d => d);
                foreach (FreeDocument computeable in datas)
                {
                    IDictionary<string, object> data = computeable.DictSerialize();
                    int cellIndex;

                    if (rowIndex == 0)
                    {
                        var row1 = sheet1.CreateRow(rowIndex);
                        cellIndex = 0;
                        foreach (var  o in PropertyNames)
                        {
                            row1.CreateCell(cellIndex).SetCellValue(o.Value);
                            sheet1.AutoSizeColumn(cellIndex, true);
                            cellIndex++;
                        }

                        rowIndex++;
                    }
                    cellIndex = 0;
                    var row = sheet1.CreateRow(rowIndex);

                    foreach (var value in PropertyNames.Select(name => data[name.Key]))
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
                            row.CreateCell(cellIndex).SetCellValue(value?.ToString() ?? "");
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