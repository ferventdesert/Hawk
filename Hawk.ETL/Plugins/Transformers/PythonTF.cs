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
using Hawk.ETL.Managements;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("PythonTF", "PythonTF_desc")]
    public class PythonTF : TransformerBase
    {
        protected readonly ScriptEngine engine;
        protected readonly ScriptScope scope;
        private CompiledCode compiledCode;
        [Browsable(false)]
        public override string KeyConfig => Script.Substring(Math.Min(100, Script.Length));
        public PythonTF()
        {
            engine = Python.CreateEngine();
            scope = engine.CreateScope();
            Script = "value";
            ScriptWorkMode = ScriptWorkMode.NoTransform; 
        }

        [LocalizedDisplayName("key_188")]
        [LocalizedDescription("etl_script_mode")]
        public ScriptWorkMode ScriptWorkMode { get; set; }

        [LocalizedDisplayName("key_511")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }


        [LocalizedDisplayName("key_512")]
        [PropertyOrder(100)]
        [LocalizedDescription("key_513")]
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

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas, AnalyzeItem analyzer = null)
        {
            foreach (var data in datas)
            {
                object d;
                try
                {
                    d = eval(data);
                    
                }
                catch (Exception ex)
                {
                    if(analyzer!=null)
                    analyzer.Analyzer.AddErrorLog(data, ex, this);
                    else
                    {
                       XLogSys.Print.Error(string.Format(GlobalHelper.Get("key_208"), this.Column, this.TypeName, ex.Message));
                    }
                   continue; 
                }
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