using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Crawlers
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
        [PropertyOrder(0)]
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
        [LocalizedDisplayName("target_value")]
        public CrawlType CrawlType { get; set; }

        
        [PropertyOrder(2)]
        [LocalizedDisplayName("key_162")]
        public SelectorFormat Format { get; set; }

        [LocalizedDisplayName("key_163")]
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

        [LocalizedDisplayName("key_164")]
        [PropertyOrder(2)]
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
        [PropertyOrder(3)]
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