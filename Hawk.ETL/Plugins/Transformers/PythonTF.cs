using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;

namespace Hawk.ETL.Plugins.Transformers
{
   

   

    [XFrmWork("Python转换器", "执行特定的python代码")]
    public class PythonTF : TransformerBase
    {
        private readonly ScriptEngine engine;
        private readonly ScriptScope scope;
        private CompiledCode compiledCode;

        public PythonTF()
        {
            engine = Python.CreateEngine();
            scope = engine.CreateScope();
            Script = "value";
        }

      

        [DisplayName("执行脚本")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            OneOutput = false;
            var source = engine.CreateScriptSourceFromString(Script);
            compiledCode = source.Compile();
            return true;
        }

        public override object TransformData(IFreeDocument datas)
        {
            var value = datas[Column];

            var dictionary = new PythonDictionary();
            foreach (var data1 in datas)
            {
                dictionary.Add(data1.Key, data1.Value);
            }
            scope.SetVariable("data", dictionary);
            scope.SetVariable("value", value);
            foreach (var data in datas)
            {
               scope.SetVariable(data.Key,data.Value);
            }
            dynamic d;
            try
            {
                d = compiledCode.Execute(scope);
            }
            catch (Exception ex)
            {
                d = ex.Message;
            }
         
            if (d != null)
            {
                var column = Column;
                if (!string.IsNullOrEmpty(NewColumn))
                {
                    column = NewColumn;
                }
                datas[column] = d;

            }


         
            return d;
        }

      
             
    }
}