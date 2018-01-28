using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("Python转换器", "执行特定的python代码或脚本，最后一行需要为值类型，作为该列的返回值")]
    public class PythonTF : TransformerBase
    {
        protected readonly ScriptEngine engine;
        protected readonly ScriptScope scope;
        private CompiledCode compiledCode;

        public PythonTF()
        {
            engine = Python.CreateEngine();
            scope = engine.CreateScope();
            Script = "value";
            ScriptWorkMode = ScriptWorkMode.NoTransform; 
        }

        [DisplayName("工作模式")]
        [Description("文档列表：[{}],转换为多个数据行构成的列表；单文档：{},将结果的键值对附加到本行；不进行转换：直接将值放入到新列")]
        public ScriptWorkMode ScriptWorkMode { get; set; }

        [DisplayName("执行脚本")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }


        [DisplayName("Python库路径")]
        [PropertyOrder(100)]
        [Description("若需要引用第三方Python库，则可指定库的路径，一行一条")]
        [PropertyEditor("CodeEditor")]
        public string LibraryPath { get; set; }


        public override bool Init(IEnumerable<IFreeDocument> docus)
        {
            OneOutput = false;
            var script = Script;
            if (!string.IsNullOrWhiteSpace(LibraryPath))
            {
                var libs = LibraryPath.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);
                var head = libs.Aggregate("import sys\n", (current, lib) => current + $@"sys.path.append(""{lib}"")");
                script = head + "\n" + script;
                XLogSys.Print.Debug(script);
            }
            var source = engine.CreateScriptSourceFromString(script);
            compiledCode = source.Compile();
            IsMultiYield = ScriptWorkMode == ScriptWorkMode.List;
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
            if (ScriptWorkMode == ScriptWorkMode.NoTransform)
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