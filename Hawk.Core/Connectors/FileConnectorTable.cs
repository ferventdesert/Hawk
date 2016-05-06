using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Hawk.Core.Connectors
{
    [XFrmWork("文本导入导出器", "输出制表符文本文件", "对基本文本文件进行导入和导出的工具")]
    public class FileConnectorTable : FileConnector
    {
        #region Properties

        private FileStream fileStream;

        private StreamWriter streamWriter;


      
        public FileConnectorTable()
        {
            EncodingType = EncodingType.UTF8;
            SplitString = "\t";

            ContainHeader = true;
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)

        {
            var dict= base.DictSerialize(scenario);
            dict.Add("ContainHeader", ContainHeader);
            dict.Add("SplitString", SplitString);
            return dict;
        }

        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(docu, scenario);
            ContainHeader = docu.Set("ContainHeader", ContainHeader);
            SplitString = docu.Set("SplitString", SplitString);
        }

        [DisplayName("列分割符")]
        public string SplitString { get; set; }


        [DisplayName("包含头信息")]
        public bool ContainHeader { get; set; }

        protected virtual string SplitChar => SplitString;

        public override string ExtentFileName => ".txt";


        public void Save()
        {
            streamWriter.Close();

            streamWriter.Close();
        }

        public void Start(ICollection<string> titles)
        {
            fileStream = new FileStream(FileName, FileMode.OpenOrCreate);
            streamWriter = new StreamWriter(new BufferedStream(fileStream), AttributeHelper.GetEncoding(EncodingType));
            var title = titles.Aggregate("", (current, title1) => current + (title1 + SplitChar));

            title = title.Substring(0, title.Length - 1) + "\n";

            streamWriter.Write(title);
        }

        public void WriteData(IEnumerable<object[]> datas)
        {
            foreach (var rows in datas)
            {
                WriteData(rows);
            }
        }


        /// <summary>
        ///     导出到CSV文件
        /// </summary>
        /// <param name="titles">文件标题</param>
        /// <param name="datas">数据</param>
        /// <param name="fileName">要保存的文件名</param>
        public static void DataTableToCSV(ICollection<string> titles, IEnumerable<object[]> datas, string fileName,
            char split = ',', Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            var fs = new FileStream(fileName, FileMode.OpenOrCreate);

            var sw = new StreamWriter(new BufferedStream(fs), encoding);

            var title = titles.Aggregate("", (current, title1) => current + (title1 + split));

            title = title.Substring(0, title.Length - 1) + "\n";

            sw.Write(title);

            foreach (var rows in datas)
            {
                var line = new StringBuilder();
                foreach (var row in rows)
                {
                    if (row != null)
                    {
                        line.Append(row.ToString().Trim());
                    }
                    else
                    {
                        line.Append(" ");
                    }
                    line.Append(split);
                }
                var result = line.ToString().Substring(0, line.Length - 1) + "\n";

                sw.Write(result);
            }

            sw.Close();

            fs.Close();
        }

        public void WriteData(object[] datas)
        {
            var line = datas.Aggregate("",
                (current, row) => current + ((row == null ? " " : row.ToString().Trim()) + SplitChar));
            line = line.Substring(0, line.Length - 1) + "\n";

            streamWriter.Write(line);
        }


        public override IEnumerable<IFreeDocument> ReadFile(Action<int> alreadyGetSize = null)
        {
            var titles = new List<string>();

            var intColCount = 0;
            var blnFlag = true;

            foreach (var strline in FileEx.LineRead(FileName, AttributeHelper.GetEncoding(EncodingType)))
            {
                if (string.IsNullOrWhiteSpace(strline))
                    continue;

                var aryline = strline.Split(new[] {SplitChar}, StringSplitOptions.None);

                string[] objs = null;

                //给datatable加上列名
                if (blnFlag)
                {
                    blnFlag = false;
                    intColCount = aryline.Length;
                    objs = new string[intColCount];
                    if (ContainHeader)
                    {
                        titles.AddRange(aryline);
                        continue;
                    }
                    for (var i = 0; i < intColCount; i++)
                    {
                        titles.Add("属性" + i);
                    }


                    for (var i = 0; i < intColCount; i++)
                    {
                        objs[i] = aryline[i].Trim();
                    }
                }
                else
                {
                    var min = Math.Min(intColCount, aryline.Count());
                    objs = new string[min];
                    for (var i = 0; i < min; i++)
                    {
                        objs[i] = aryline[i].Trim();
                    }
                }
                var data = PluginProvider.GetObjectInstance(DataType) as IFreeDocument;
                var dict = new Dictionary<string, object>();
                for (var index = 0; index < Math.Min(titles.Count, objs.Length); index++)
                {
                    var freeDocument = data as IFreeDocument;
                    if (freeDocument != null)
                    {
                        if (index == 0 && PropertyNames.Any() == false)
                        {
                            PropertyNames = titles.ToDictionary(d => d, d => d);
                        }
                    }
                    var title = titles[index];
                    var key = PropertyNames.FirstOrDefault(d => d.Value == title).Key;
                    if (key != null)
                    {
                        dict.Add(key, objs[index]);
                    }
                }
                data.DictDeserialize(dict);
                yield return data;
            }
        }

        public override bool ShouldConfig => true;

        public override IEnumerable<IFreeDocument> WriteData(IEnumerable<IFreeDocument> datas)
        {
            if (datas.Any() == false) yield break;
            using (var dis = new DisposeHelper(Save))
            {
                if (PropertyNames == null||PropertyNames.Count==0)
                {
                    PropertyNames = datas.GetKeys().ToDictionary(d => d, d => d);
                }

                Start(PropertyNames.Values);

                var i = 0;

                foreach (var computeable in datas)
                {
                    IDictionary<string, object> data = computeable.DictSerialize(Scenario.Report);

                    var objects = PropertyNames.Select(name => data[name.Key]).ToArray();
                    WriteData(objects);
                    i++;
                    yield return computeable;
                }
            }
        }

        #endregion
    }
}