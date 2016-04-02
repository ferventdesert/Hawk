using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Utils
{
    /// <summary>
    /// 文件操作管理器
    /// </summary>
    public static class FileEx
    {
        /// <summary>
        /// 获取当前目录下固定后缀的文件名称列表 
        /// </summary>
        /// <param name="thisFolderLocation">文件夹位置</param>
        /// <param name="thisExtensionName">后缀名</param>
        /// <returns></returns>
        public static List<string> GetFileNameInFolder(string thisFolderLocation, string thisExtensionName)
        {
            var temp = new List<string>();
            if (Directory.Exists(thisFolderLocation))
            {
                string[] paths = Directory.GetFiles(thisFolderLocation);
                temp.AddRange(from str in paths where Path.GetExtension(str) == thisExtensionName select Path.GetFileNameWithoutExtension(str));
            }
            return temp;
        }
        public static void LineSplit(string filename, string split, Action<string[]> act)
        {
            LineRead(filename, d =>
            {
                if (act != null)
                {
                    var s = d.Split(new string[] { split }, StringSplitOptions.RemoveEmptyEntries);
                    act(s);
                }
            });
        }

        /// <summary>
        /// 获取当前目录下的文件名称列表 
        /// </summary>
        /// <param name="thisFolderLocation">文件夹位置</param>
        /// <returns></returns>
        public static List<string> GetFileNameInFolder(string thisFolderLocation)
        {
            var temp = new List<string>();
            if (Directory.Exists(thisFolderLocation))
            {
                string[] paths = Directory.GetFiles(thisFolderLocation);
                temp.AddRange(paths.Select(Path.GetFileName));
            }
            return temp;
        }

        public static string GetFileLocation(string filename, string location, string extension)
        {
            return location + filename + extension;
        }

        public static void WriteAll(this IFileConnector connector,IEnumerable<IDictionarySerializable>data )
        {

           

                var r = connector.WriteData(data).LastOrDefault();

        }

        public static void LineRead(string filename, Action<string> act,Encoding code= null)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs,  code ?? new UTF8Encoding());


            string ts = sr.ReadLine();

            do
            {
                string t = ts;
                act(t);
                ts = sr.ReadLine();
            } while (ts != null);
            sr.Close();
            fs.Close();
        }
        public static IEnumerable<string> LineRead(string filename, Encoding code = null)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);

            StreamReader sr;
            sr = code == null ? new StreamReader(fs,   true) : new StreamReader(fs, code);


            using (var dis = new DisposeHelper(() =>
            {
                sr.Close();
                fs.Close();
            }))
            {

                string ts = sr.ReadLine();
                do
                {
                    string t = ts;
                    yield return t;
                    ts = sr.ReadLine();
                } while (ts != null);
            }
        }
        public static void FolderRead(string inputPath, Action<string, int> act)
        {
            var files = Directory.GetFiles(inputPath);
            int index = 0;
            foreach (var filename in files)
            {
                FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs, new UTF8Encoding());


                string ts = sr.ReadLine();

                do
                {
                    string t = ts;
                    act(t, index);
                    ts = sr.ReadLine();
                    index++;
                } while (ts != null);
                sr.Close();
                fs.Close();
            }




        }

        public static void Transform(string input, string output, Func<string, string> trans, int limit = -1)
        {
            FileStream fs = new FileStream(input, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, new UTF8Encoding());
            int count = 0;
            List<string> result = new List<string>();
            string ts = sr.ReadLine();

            do
            {
                string t = ts;
                var r = trans(t);
                if (r != null)
                    result.Add(r);
                ts = sr.ReadLine();
                count++;
                if (limit != -1 && count >= limit)
                    break;
            } while (ts != null);
            sr.Close();
            fs.Close();

            System.IO.File.WriteAllLines(output, result, new UTF8Encoding());
        }

        public static void TransformFolder(string inputPath, string outputPath, Func<string, string> trans)
        {
            var files = Directory.GetFiles(inputPath);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                Transform(file, outputPath + name, trans);
            }
        }
    }
}
