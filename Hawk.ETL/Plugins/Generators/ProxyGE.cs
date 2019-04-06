using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;

namespace Hawk.ETL.Plugins.Generators
{
   

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

 //   [XFrmWork("proxyge", "proxyge_desc", "database")]
    public class ProxyGE : GeneratorBase
    {
        public ProxyGE()
        {
            ProxyStrategy = ProxyStrategy.NoAgent;
            Column = "__Proxy";
            MergeType = MergeType.Merge;
        }


        [PropertyOrder(4)]
        [LocalizedCategory("http_header")]
        [LocalizedDisplayName("key_176")]
        [LocalizedDescription("proxy_setting_desc")]
        [PropertyEditor("CodeEditor")]
        public string ProxySetting { get; set; }


        List<string> argsList = new List<string>();
        [LocalizedDisplayName("key_179")]
        public ProxyStrategy ProxyStrategy { get; set; }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            var proxy = "";
            proxy = ProxySetting;
            if (string.IsNullOrEmpty(proxy))
                proxy = ConfigFile.GetConfig<DataMiningConfig>().ProxySetting;

            if (string.IsNullOrEmpty(proxy))
                return base.Init(datas);

                try
            {
                argsList = proxy.Split(new[] { "\r\n" }, StringSplitOptions.None).ToList();

            }
            catch (Exception ex)
            {
                XLogSys.Print.Error(ex.Message);


            }
            return base.Init(datas);
        }
        public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
            return argsList.Select(doc => new FreeDocument { { this.Column, doc } });
        }



    }
}