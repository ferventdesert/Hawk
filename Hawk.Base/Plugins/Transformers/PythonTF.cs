using System;
using System.Collections.Generic;
using System.Linq;
using Hawk.Base.Managements;
using Hawk.Base.Utils;
using Hawk.Base.Utils.Plugins;

namespace Hawk.Base.Plugins.Transformers
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
        [LocalizedDescription("key_501")]
        public ScriptWorkMode ScriptWorkMode { get; set; }

        [LocalizedDisplayName("key_511")]
        [PropertyEditor("CodeEditor")]
        public string Script { get; set; }


        [LocalizedDisplayName("key_512")]
        [BrowsableAttribute.PropertyOrderAttribute(100)]
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

        public override IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas, AnalyzeItem analyzer)
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
                    analyzer.Analyzer.AddErrorLog(data, ex, this);
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