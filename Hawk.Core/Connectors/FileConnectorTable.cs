using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using EncodingType = Hawk.Core.Utils.EncodingType;

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
            var dict = base.DictSerialize(scenario);
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

        [LocalizedDisplayName("列分割符")]
        public string SplitString { get; set; }


        [LocalizedDisplayName("包含头信息")]
        public bool ContainHeader { get; set; }

        protected virtual string SplitChar => SplitString;

        public override string ExtentFileName => ".txt";

        public static string ReplaceSplitString(string input, string splitchar)
        {
            if (input == null)
                return "";
                input = input.Replace("\"", "\"\"");
                input = input.Replace("\n", "\\n");
            if (input.Contains(splitchar))
            {
                input = "\"" + input + "\"";
            }
            return input;
        }

        public static string ReplaceSplitString2(string input, string splitchar)
        {
            if (input == null)
                return "";
            input = input.Replace("\"\"", "\"");
            return input.Trim('"');
        }

        public void Save()
        {
            streamWriter.Close();

            streamWriter.Close();
        }

        public void Start(ICollection<string> titles)
        {
            fileStream = new FileStream(FileName, FileMode.OpenOrCreate);
            streamWriter = new StreamWriter(new BufferedStream(fileStream), AttributeHelper.GetEncoding(EncodingType));
            //var title =  titles.Aggregate("", (current, title1) => current + (title1 + SplitChar));
            var title = SplitChar.Join(titles.Select(d => ReplaceSplitString(d, SplitChar))) + "\n";
            //title = title.Substring(0, title.Length - 1) + "\n";

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
            var line = SplitChar.Join(datas.Select(d => ReplaceSplitString(d?.ToString(), SplitChar))) + "\n";
            streamWriter.Write(line);
        }

        //这段代码像屎一样又臭又长
        //English Edition: This code like shit. 
        //不要怪我，我就是懒
        public override IEnumerable<FreeDocument> ReadFile(Action<int> alreadyGetSize = null)
        {
            var titles = new List<string>();

            var intColCount = 0;
            var blnFlag = true;
            var builder = new StringBuilder();
            foreach (var strline in FileEx.LineRead(FileName, AttributeHelper.GetEncoding(EncodingType)))
            {
                if (string.IsNullOrWhiteSpace(strline))
                    continue;


                builder.Clear();
                var comma = false;
                var array = strline.ToCharArray();
                var values = new List<string>();
                var length = array.Length;
                var index = 0;
                while (index < length)
                {
                    var item = array[index++];
                    if (item.ToString() == SplitChar)
                        if (comma)
                        {
                            builder.Append(item);
                        }
                        else
                        {
                            values.Add(builder.ToString());
                            builder.Clear();
                        }
                    else if (item == '"')
                    {
                        comma = !comma;
                    }

                    else
                    {
                        builder.Append(item);
                    }
                }
                if (builder.Length > 0)
                    values.Add(builder.ToString());
                var count = values.Count;
                if (count == 0) continue;

                //给datatable加上列名
                if (blnFlag)
                {
                    blnFlag = false;
                    intColCount = values.Count;
                    if (ContainHeader)
                    {
                        titles.AddRange(values);
                        continue;
                    }
                    for (var i = 0; i < intColCount; i++)
                    {
                        titles.Add("属性" + i);
                    }
                }
                var data = new FreeDocument();
                var dict = new Dictionary<string, object>();
                for (index = 0; index < Math.Min(titles.Count, values.Count); index++)
                {
                    if (index == 0 && PropertyNames.Any() == false)
                    {
                        PropertyNames = titles.ToDictionary(d => d, d => d);
                    }
                    var title = titles[index];
                    var key = PropertyNames.FirstOrDefault(d => d.Value == title).Key;
                    if (key != null)
                    {
                        dict.Add(key, values[index]);
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
                if (PropertyNames == null || PropertyNames.Count == 0)
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