using System.Collections.Generic;
using Hawk.Core.Utils;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

namespace Hawk.Core.Connectors
{
    public class DataCollection : PropertyChangeNotifier, IDictionarySerializable
    {
        #region Constants and Fields

        protected List<IFreeDocument> RealData;


        private string name;

        [LocalizedDisplayName("key_5")]
        public virtual string Source => GlobalHelper.Get("key_6");

        public override string ToString()
        {
            return Name;
        }
        [Browsable(false)]

        #endregion

        #region Constructors and Destructors

        public DataCollection(IEnumerable<IFreeDocument> data)
        {
            TableInfo=new TableInfo();
            RealData = new List<IFreeDocument>(data)
               ;
        }
        #endregion

        #region Properties

        public DataCollection()
        {
            TableInfo=new TableInfo();
            RealData = new List<IFreeDocument>();
        }

        [LocalizedDisplayName("key_7")]
        public TableInfo TableInfo { get; set; }

        [Browsable(false)]
        public virtual IList<IFreeDocument> ComputeData
        {
            get { return RealData; }
        }


        [LocalizedDisplayName("key_8")]
        public int Count
        {
            get { return ComputeData.Count; }
        }

        

        [LocalizedDisplayName("key_9")]
        public string Description { get; set; }

        [LocalizedDisplayName("key_10")]
        public virtual bool IsVirtual
        {
            get { return false; }
        }

        [LocalizedDisplayName("key_11")]
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

        #endregion

        #region Public Methods

        #endregion

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = new FreeDocument();
            dict.Add("Name", Name);
            dict.Children= RealData.Select(d=>d as FreeDocument).ToList();
            return dict;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Name=docu.Set("Name", Name);

            var doc = docu as FreeDocument;
            var res= doc?.Children?.Select(d=>d as IFreeDocument).ToList();
            if (res != null)
                RealData = res;
        }

        public DataCollection Clone(bool isdeep)
        {
            var docuts = new List<IFreeDocument>();

            for (int i = 0; i < ComputeData.Count; i ++)
            {
                if (isdeep)
                {
                    var fr = new FreeDocument();
                    ComputeData[i].DictCopyTo(fr);
                    docuts.Add(fr);
                }
                else
                {
                    docuts.Add(ComputeData[i] as IFreeDocument);
                }
            }
            var collection = new DataCollection(docuts);
            collection.Name = Name + '1';

            collection.TableInfo = this.TableInfo.Clone();
            return collection;
        }
    }
}