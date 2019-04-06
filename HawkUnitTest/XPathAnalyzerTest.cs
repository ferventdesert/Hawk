using System.Linq;
using Hawk.ETL.Crawlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HawkUnitTest
{
    [TestClass]
    public class XPathAnalyzerTest
    {
        public static string url = "http://www.cnblogs.com/";

        [TestMethod]
        public void XPathTest()
        {
            var doc = XPathAnalyzer.GetHtmlDocument(url);
            Assert.IsTrue(doc != null);
            var datas = XPathAnalyzer.GetDataFromURL(url);
            Assert.IsTrue(datas != null && datas.Count > 10);
            var properties = doc.DocumentNode.SearchPropertiesSmartList();

            Assert.IsTrue(properties.Any());
            var firstOrDefault = properties.FirstOrDefault();
            datas = doc.DocumentNode.GetDataFromXPath(firstOrDefault.CrawItems).ToList();
            Assert.IsTrue(datas != null && datas.Count > 10);
        }
    }
}