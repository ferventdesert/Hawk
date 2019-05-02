using System.Collections.Generic;
using Hawk.Core.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Plugins.Transformers;
using EncodingType = Hawk.Core.Utils.EncodingType;

namespace Hawk.ETL.Plugins.Generators
{
    [XFrmWork("ReadFileTextGE", "ReadFileTextGE_desc","clipboard_file")]
    public class ReadFileTextGE : TransformerBase 
    {
        private readonly BuffHelper<string> buffHelper = new BuffHelper<string>(20);
        protected string _fileName = "";

        [LocalizedDisplayName("key_163")]
        [LocalizedDescription("key_87")]
        [PropertyOrder(2)]
        public virtual string FileName
        {
            get { return _fileName; }
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged("FileName");
                }
            }
        }
        [Browsable(false)]
        public override string KeyConfig => FileName;
        [LocalizedDisplayName("key_34")]
        [PropertyOrder(3)]
        public ReadOnlyCollection<ICommand> Commands2
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_437"), obj => Open(), icon: "disk")
                    });
            }
        }

        [LocalizedDisplayName("key_438")]
        [PropertyOrder(4)]
        public virtual EncodingType EncodingType { get; set; }

        private void Open()
        {
            var dialog = new OpenFileDialog();
            dialog.Multiselect = false; //该值确定是否可以选择多个文件
            dialog.Title = GlobalHelper.Get("key_439");
            dialog.Filter = "所有文件(*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FileName = dialog.FileName;
            }
        }

        public override object TransformData(IFreeDocument document)
        {
            var path = FileName;
            var result = document?.Query(FileName);
            if (result != null)
                path = result;
            var res = buffHelper.Get(path);
            if (res != null)
            {
                return res;
            }
           
            res = File.ReadAllText(path, AttributeHelper.GetEncoding(EncodingType));
            buffHelper.Set(path, res);
            return res;
        }
      
    }


    [XFrmWork("ReadFileGe", "ReadFileGe_desc","clipboard_file")]
    public class ReadFileGe : GeneratorBase
    {
        private readonly BuffHelper<List<FreeDocument>> buffHelper =new BuffHelper<List<FreeDocument>>(50);
        public ReadFileGe()
        {
            ConnectorSelector =
                new ExtendSelector<XFrmWorkAttribute>(PluginProvider.GetPluginCollection(typeof (IFileConnector)));

            ConnectorSelector.SelectChanged += (s, e) =>
            {
                if (ConnectorSelector.SelectItem == null)
                    return;
                Connector = PluginProvider.GetObjectInstance<IFileConnector>(ConnectorSelector.SelectItem.Name);
                OnPropertyChanged("Connector");
            };
            FileName = "";
        }
        protected string _fileName = "";

        [Browsable(false)]
        public override string KeyConfig => FileName; 
        [LocalizedDisplayName("key_163")]
        [LocalizedDescription("key_87")]
        [PropertyOrder(2)]
        public  string FileName
        {
            get { return _fileName; }
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged("FileName");
                    var connector = FileConnector.SmartGetExpotStr(FileName);
                    if (connector != null && ConnectorSelector != null)
                    {
                        ConnectorSelector.SelectItem =
                            ConnectorSelector.Collection.FirstOrDefault(d => d.Name == connector);
                        if (Connector == null)
                        {
                            Connector = PluginProvider.GetObjectInstance<IFileConnector>(ConnectorSelector.SelectItem.Name);
                            OnPropertyChanged("Connector");
                        }
                    }
                
            }
            }
        }
        [LocalizedDisplayName("key_34")]
        [PropertyOrder(3)]
        public ReadOnlyCollection<ICommand> Commands2
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_437"), obj => Open(), icon: "disk")
                    });
            }
        }
        private void Open()
        {
            var dialog = new OpenFileDialog();
            dialog.Multiselect = false; //该值确定是否可以选择多个文件
            dialog.Title = GlobalHelper.Get("key_439");
            dialog.Filter = "所有文件(*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FileName = dialog.FileName;
            }
        }

        [LocalizedDisplayName("key_442")]
        [PropertyOrder(0)]
        public ExtendSelector<XFrmWorkAttribute> ConnectorSelector { get; set; }

        [Browsable(false)]
        public  EncodingType EncodingType { get; set; }

        [LocalizedDisplayName(("key_443"))]
        [PropertyOrder(1)]
        [TypeConverter(typeof (ExpandableObjectConverter))]
        public IFileConnector Connector { get; set; }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = base.DictSerialize();
            if (Connector != null)
            {
                dict.SetValue("Connector", Connector.DictSerialize());
            }
            return dict;
        }

        public override void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            this.UnsafeDictDeserialize(docu);
            if (docu.ContainsKey("Connector"))
            {
                var coll = PluginProvider.GetPluginCollection(typeof (IFileConnector));
                var doc2 = docu["Connector"];
                var p = doc2 as IDictionary<string, object>;
                var name = p["Type"];
                if (name != null)
                {
                    var result =
                        coll.FirstOrDefault(d => d.MyType.Name == name.ToString());
                    ConnectorSelector.SelectItem = result;

                    Connector?.DictDeserialize(p);
                }
            }
        }

        public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
            if (Connector == null)
               return new List<IFreeDocument>();
            var path = FileName;
            var result = document?.Query(FileName);
            if (result != null)
                path = result;

            Connector.FileName = path;

            if (!IsExecute)
                return Connector.ReadFile();
            else
                return Connector.ReadFile().CacheDo(buffHelper.GetOrCreate(path,new List<FreeDocument>()),this.Father.SampleMount*2);
         
        }

     
    }


    [XFrmWork("WriteFileTextTF", "WriteFileTextTF_desc")]
    public class WriteFileTextTF : DataExecutorBase
    {
        private string _fileName = "";

        [LocalizedDisplayName("key_163")]
        [LocalizedDescription("key_87")]
        [PropertyOrder(2)]
        public string FileName
        {
            get { return _fileName; }
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged("FileName");
                }
            }
        }

        [Browsable(false)]
        public override string KeyConfig => FileName; 
        [LocalizedDisplayName("key_34")]
        [PropertyOrder(3)]
        public ReadOnlyCollection<ICommand> Commands2
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_437"), obj => Open(), icon: "disk")
                    });
            }
        }

        [LocalizedDisplayName("key_438")]
        [PropertyOrder(4)]
        public virtual EncodingType EncodingType { get; set; }

        private void Open()
        {
            var saveTifFileDialog = new SaveFileDialog();
            saveTifFileDialog.OverwritePrompt = true; //询问是否覆盖
            saveTifFileDialog.Filter = "*.txt|*.txt";
            saveTifFileDialog.DefaultExt = "txt"; //缺省默认后缀名
            if (saveTifFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileName = saveTifFileDialog.FileName;
            }
        }

        public override IEnumerable<IFreeDocument> Execute(IEnumerable<IFreeDocument> documents)
        {
            foreach (var document in documents)
            {
                var item = document[Column].ToString();
                var path = document.Query(FileName);
                var directoryInfo = new DirectoryInfo(path);
                var folder = directoryInfo.Parent;
                if (folder == null)
                {
                    yield return document;
                    continue;
                }
                if (!folder.Exists)
                {
                    folder.Create();
                }
                var url = document[Column].ToString();
                if (string.IsNullOrEmpty(url))
                {
                    yield return document;
                    continue;
                }
               File.AppendAllText(path, item+"\n", AttributeHelper.GetEncoding(EncodingType));
                yield return document;
            }
        }
    }
}