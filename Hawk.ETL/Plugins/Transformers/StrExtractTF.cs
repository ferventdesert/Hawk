using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("StrExtractTF", "StrExtractTF_desc")]
    public  class StrExtractTF : TransformerBase
    {
        private string _former;
        private string _end;

        public StrExtractTF()
        {
            Former = End =
                "";
          
        }

        [LocalizedDisplayName("key_549")]
        public string Former
        {
            get { return _former; }
            set
            {
                if (_former != value)
                {
                    _former = value;
                    OnPropertyChanged("Former");
                }
            }
        }

        [LocalizedDisplayName("key_550")]
        public string End
        {
            get { return _end; }
            set
            {
                if (_end != value)
                {
                    _end = value;
                    OnPropertyChanged("End");
                }
            }
        }


        [LocalizedDisplayName("key_551")]
        [LocalizedDescription("key_552")]
        public bool HaveStartEnd { get; set; }
        public override object TransformData(IFreeDocument datas)
        {
            object item = datas[Column];
            if (item == null)
                return null;
            var str = item.ToString();
            if (Former == "" || End == "")
                return item;
            var start = str.IndexOf(Former);
            if (start == -1)
                return "";
            if (HaveStartEnd==false)
                start += Former.Length;
            var end = str.IndexOf(End,start);
            if (end == -1)
                return "";
            if (HaveStartEnd)
                end += End.Length;
            return str.Substring(start, end - start);



        }
    }
}
