using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hawk.ETL.Crawlers
{
    public static class XPath 
    {
        public static readonly Regex boxRegex = new Regex(@"\[\d{1,3}\]");


        

        /// <summary>
        ///     获取从头开始的最大公共子串
        /// </summary>
        /// <returns></returns>
        public static string GetMaxCompareXPath(IEnumerable<string> items2)
        {
            var items = items2.Select(Split).ToList();
            int minlen = items.Min(d => d.Count());

            string c = null;
            int i = 0;
            for (i = 0; i < minlen; i++)
            {
                for (int index = 0; index < items.Count; index++)
                {
                    var  path = items[index];
                    if (index == 0)
                    {
                        c = path[i];
                    }
                    else
                    {
                        if (c != path[i])
                        {
                            goto OVER;
                        }
                    }
                }
            }
            OVER:
            var  first = SubXPath( items2.First(),i + 1);
            first=RemoveFinalNum(first);
            return first;
        }

        public static  string ToString(IEnumerable<string> items )
        {
            if (!items.Any())
                return "/";
            return "/" + items.Aggregate((a, b) => a + '/' + b);
        }

        public static List<string> Split(string path)
        {
            return path.Split('/').Select(d=>d.Trim()).Where(d=>d!="").ToList();
        } 
        public static string GetAttributeName(string path)
        {
            return path.Replace("@", "").Replace("#","").Replace("[1]", "");
        }

        public static string  SubXPath(string path,int t)
        {
            var items = Split(path);
            if (t > 0)
                return ToString(items.Take(t));
            else
            {
                return ToString(items.Take(items.Count + t));
            }
        }

        public static string SubXPath(string path,int start, int end)
        {
            return ToString(Split(path).Skip(start).Take(end).ToList());
        }

        public static string TakeOff(string path,string root)
        {
            if (string.IsNullOrEmpty(root))
                return path;
            var start = Split(root).Count;
            var total = Split(path).Count;
            return SubXPath(path,start,total-start);
        }

        /// <summary>
        /// 相比于takeoff，要去掉父xpath，同时还要自动下降一层
        /// </summary>
        /// <param name="path"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static string TakeOffPlus(string path, string root)
        {
            if (string.IsNullOrEmpty(root))
                return path;
            var start = Split(root).Count;
            var total = Split(path).Count;
            return SubXPath(path, start+1, total - start);
        }

        public static string RemoveFinalNum(string path)
        {
            var items = Split(path);
            if (items.Count == 0)
                return path;
            string v = items.Last();
            string v2 = boxRegex.Replace(v, "");
            items[items.Count - 1] = v2;
            return ToString(items);
        }
    }
}