using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Crawlers
{
    public class Proxy
    {
        public Proxy()
        {
            ProxyIp = "";
            ProxyPort = 80;
        }

        public string ProxyUserName { get; set; }
        public string ProxyPwd { get; set; }
        public string ProxyIp { get; set; }
        public int ProxyPort { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1}",ProxyIp,ProxyPort);

        }
    }

    /// <summary>
    ///     代理策略
    /// </summary>
    public enum ProxyStrategy
    {
        /// <summary>
        ///     不使用代理
        /// </summary>
        NoAgent,

        /// <summary>
        ///     当失败后更换代理
        /// </summary>
        FailChanged,

        /// <summary>
        ///     当达到一定访问次数后更换代理
        /// </summary>
        CountChange,
    }

    /// <summary>
    ///     Web代理提供器
    /// </summary>
    public class ProxyProvider : PropertyChangeNotifier, IDictionarySerializable
    {
        [LocalizedDisplayName("代理列表")]
        public ObservableCollection<Proxy> Proxies { get; set; }
        private int count;

        public ProxyProvider()
        {
            ProxyStrategy = ProxyStrategy.NoAgent;
            MaxVisitCount = 100000;
            Proxies=new ObservableCollection<Proxy>();
            ParaGeneratorSelector =
                new ExtendSelector<XFrmWorkAttribute>(PluginProvider.GetPluginCollection(typeof (IColumnGenerator)));
            ParaGeneratorSelector.SelectChanged += (s, e) =>
            {
                ParaGenerator = PluginProvider.GetObjectInstance<IColumnGenerator>(ParaGeneratorSelector.SelectItem.Name);
                ParaGenerator.Column = "ProxyIp";
                OnPropertyChanged("ParaGenerator");
            };
            ParaGeneratorSelector.SelectItem = ParaGeneratorSelector.Collection.FirstOrDefault(d => d.Name == "从文本生成");
        }

        [LocalizedDisplayName(("生成器"))]
        [TypeConverter(typeof (ExpandableObjectConverter))]
        public IColumnGenerator ParaGenerator { get; set; }

        [LocalizedDisplayName("代理策略")]
        public ProxyStrategy ProxyStrategy { get; set; }


        [LocalizedDisplayName("最大访问次数")]
        public int MaxVisitCount { get; set; }


        [PropertyOrder(4)]
        [LocalizedDisplayName("生成器类型")]
        public ExtendSelector<XFrmWorkAttribute> ParaGeneratorSelector { get; set; }


        [LocalizedDisplayName("当前代理索引")]
        public int CurrentAgentIndex { get; set; }

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            FreeDocument dict = this.UnsafeDictSerialize();

            if (ParaGenerator != null)
            {
                dict.SetValue("ParaGenerator", ParaGenerator.DictSerialize());
            }
            return dict;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            this.UnsafeDictDeserialize(docu);
            if (docu.ContainsKey("ParaGenerator"))
            {
                List<XFrmWorkAttribute> coll = PluginProvider.GetPluginCollection(typeof (IColumnGenerator));
                object doc2 = docu["ParaGenerator"];
                var p = doc2 as IDictionary<string, object>;
                object name = p["Type"];
                if (name != null)
                {
                    ParaGeneratorSelector.SelectItem =
                        coll.FirstOrDefault(d => d.Name == name.ToString());

                    ParaGenerator?.DictDeserialize(p);
                }
            }
        }

        public void Init()
        {
           
            foreach (var result in ParaGenerator.Generate())
            {
                var p = new Proxy();
                p.UnsafeDictDeserialize(result);
                Proxies.Add(p);
            };

        }

        public Proxy GetProxy(Func<bool> isSuccess)
        {
            switch (ProxyStrategy)
            {
                case ProxyStrategy.NoAgent:
                    return new Proxy();
                case ProxyStrategy.FailChanged:
                    if (isSuccess() == false)
                    {
                        CurrentAgentIndex++;
                    }

                    break;
                case ProxyStrategy.CountChange:
                    count++;
                    if (count > MaxVisitCount)
                        CurrentAgentIndex++;
                    break;
            }


            return getCurrentProxy();
        }

        private Proxy getCurrentProxy()
        {
            if (CurrentAgentIndex >= Proxies.Count)
            {
                CurrentAgentIndex = 0;
            }
            if (Proxies.Count == 0)
                return new Proxy();
            if (CurrentAgentIndex >= Proxies.Count)
                return new Proxy();
            return Proxies[CurrentAgentIndex];
        }
    }
}