using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
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
            ScriptWorkMode = ScriptWorkMode.不进行转换;
        }

        [DisplayName("工作模式")]
        [Description("文档列表：[{}],转换为多个数据行构成的列表；单文档：{},将结果的键值对附加到本行；不进行转换：直接将值放入到新列")]
        public ScriptWorkMode ScriptWorkMode { get; set; }

        [DisplayName("执行脚本")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            OneOutput = false;
            var source = engine.CreateScriptSourceFromString(Script);
            compiledCode = source.Compile();
            IsMultiYield = ScriptWorkMode == ScriptWorkMode.文档列表;
            return true;
        }

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas)
        {
            foreach (var data in datas)
            {
                var d = eval(data);
                foreach (var item2 in ScriptHelper.ToDocuments(d))
                {
                    var item3 = item2;
                    yield return item3.MergeQuery(data, NewColumn);
                }
            }
        }

        private object eval(IFreeDocument doc)
        {
            var value = doc[Column];

            var dictionary = new PythonDictionary();
            foreach (var data1 in doc)
            {
                dictionary.Add(data1.Key, data1.Value);
            }
            scope.SetVariable("data", dictionary);
            scope.SetVariable("value", value);
            foreach (var data in doc)
            {
                scope.SetVariable(data.Key, data.Value);
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
            return d;
        }

        public override object TransformData(IFreeDocument doc)
        {
            var d = eval(doc);
            if (ScriptWorkMode == ScriptWorkMode.不进行转换)
            {
                SetValue(doc, d);
            }
            else
            {
                var newdoc = ScriptHelper.ToDocument(d);

                newdoc.DictCopyTo(doc);
            }
            return d;
        }
    }
}