using System.Collections.Generic;
using Hawk.Base.Utils;
using Hawk.Base.Utils.MVVM;
using Hawk.Base.Utils.Plugins;

namespace Hawk.Base.Crawlers
{

  
 
    public class CrawlItem : PropertyChangeNotifier, IDictionarySerializable
    {
        private string name;

        private string xpath;
        private bool _isSelected;

        public CrawlItem()
        {
            SampleData1 = "";
            IsEnabled = true;
        }

        /// <summary>
        ///     属性名称
        /// </summary>
        [LocalizedDisplayName("key_160")]
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
        [LocalizedDisplayName("key_161")]
        public CrawlType CrawlType { get; set; }

        

        [LocalizedDisplayName("key_162")]
        public SelectorFormat Format { get; set; }

        [LocalizedDisplayName("key_163")]
        [BrowsableAttribute.PropertyOrderAttribute(1)]
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

        [LocalizedDisplayName("key_164")]
        [BrowsableAttribute.PropertyOrderAttribute(2)]
        public bool IsEnabled { get; set; }

        [Browsable(false)]
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }


        /// <summary>
        ///     样例数据
        /// </summary>
        [LocalizedDisplayName("key_165")]
        [BrowsableAttribute.PropertyOrderAttribute(3)]
        public string SampleData1 { get; set; }

    

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var doc = new FreeDocument { { "Name", Name }, { "XPath", XPath },{ "CrawlType", CrawlType }, { "IsEnabled",IsEnabled },{ "Format", Format} };
            return doc;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Name = docu.Set("Name", Name);
            XPath = docu.Set("XPath", XPath);
            CrawlType = docu.Set("CrawlType", CrawlType);
            IsEnabled = docu.Set("IsEnabled", IsEnabled);
            Format = docu.Set("Format", Format);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}