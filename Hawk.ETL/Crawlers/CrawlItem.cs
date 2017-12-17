using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Crawlers
{
    public class CrawlItem : PropertyChangeNotifier, IDictionarySerializable
    {
        private string name;

        private string xpath;

        public CrawlItem()
        {
            SampleData1 = "";
            IsEnabled = true;
        }

        /// <summary>
        ///     属性名称
        /// </summary>
        [LocalizedDisplayName("属性名称")]
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }
        [LocalizedDisplayName("是否保存HTML")]
        public bool IsHTML { get; set; }

        [LocalizedDisplayName("XPath")]
        [PropertyOrder(1)]
        public string XPath
        {
            get { return xpath; }
            set
            {
                if (xpath != value)
                {
                    xpath = value;
                    OnPropertyChanged("XPath");
                }
            }
        }

        [LocalizedDisplayName("可用")]
        [PropertyOrder(2)]
        public bool IsEnabled { get; set; }



        /// <summary>
        ///     样例数据
        /// </summary>
        [LocalizedDisplayName("样例1")]
        [PropertyOrder(3)]
        public string SampleData1 { get; set; }

    

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var doc = new FreeDocument { { "Name", Name }, { "XPath", XPath },{"IsHtml", IsHTML}, { "IsEnabled",IsEnabled } };
            return doc;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Name = docu.Set("Name", Name);
            XPath = docu.Set("XPath", XPath);
            IsHTML = docu.Set("IsHtml", IsHTML);
            IsEnabled = docu.Set("IsEnabled", IsEnabled);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}