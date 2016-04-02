using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hawk.ETL.Crawlers
{
    public class XPath : List<string>
    {
        private static readonly Regex boxRegex = new Regex(@"\[\d{1,3}\]");

        public XPath()
        {
        }

        public XPath(string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
                return;
            xpath = xpath.Trim();
            string[] items = xpath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            AddRange(items);
        }

        public XPath(IEnumerable<string> xpath)
        {
            AddRange(xpath);
        }

        

        public static XPath Translate(string item)
        {
            return new XPath(item);
        }

        public static XPath Translate(IEnumerable<string> item)
        {
            return new XPath(item);
        }

        /// <summary>
        ///     获取从头开始的最大公共子串
        /// </summary>
        /// <returns></returns>
        public static XPath GetMaxCompareXPath(IList<XPath> items)
        {
            int minlen = items.Min(d => d.Count);

            string c = null;
            int i = 0;
            for (i = 0; i < minlen; i++)
            {
                for (int index = 0; index < items.Count; index++)
                {
                    XPath path = items[index];
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
            XPath first = items.First().SubXPath(i + 1);

            first.RemoveFinalNum();
            return first;
        }

        public override string ToString()
        {
            if (!this.Any())
            {
                return "/";
            }
            return "/" + this.Aggregate((a, b) => a + '/' + b);
        }

        public static string GetAttributeName(string path)
        {
            return path.Replace("@", "").Replace("[1]", "");
        }

        public XPath SubXPath(int t)
        {
            List<string> items = this.Take(t).ToList();
            return new XPath(items);
        }

        public XPath SubXPath(int start, int end)
        {
            List<string> items = this.Skip(start).Take(end).ToList();
            return new XPath(items);
        }

        public XPath TakeOff(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return this;
            var temp = new XPath(fullPath);
            return SubXPath(temp.Count, Count - temp.Count);
        }

        public XPath RemoveFinalNum()
        {
            string v = this.Last();
            string v2 = boxRegex.Replace(v, "");
            this[Count - 1] = v2;
            return this;
        }
    }
}