using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;


namespace Hawk.ETL.Plugins.Transformers

{
  //  [XFrmWork("文本正则化", "对文本进行转换")]
    public    class TnTF : TransformerBase
    {
        protected readonly ScriptEngine engine;
        protected readonly ScriptScope scope;
        private CompiledCode compiledCode;

        public TnTF()
        {
            engine = Python.CreateEngine();
            scope = engine.CreateScope();
            ScriptWorkMode = Core.Connectors.ScriptWorkMode.List;
        }
        [DisplayName("工作模式")]
        [Description("文档列表：[{}],转换为多个数据行构成的列表；单文档：{},将结果的键值对附加到本行；不进行转换：直接将值放入到新列")]
        public ScriptWorkMode ScriptWorkMode { get; set; }
        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            var text = File.ReadAllText("tn/tnpy.py",Encoding.UTF8);
            var source = engine.CreateScriptSourceFromString(text);

            compiledCode = source.Compile();
            IsMultiYield = ScriptWorkMode == ScriptWorkMode.List;
            return true;
        }
    }
}
