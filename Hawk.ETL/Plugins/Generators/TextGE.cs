using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Generators
{
     [XFrmWork("key_177","TextGE_desc" )]
    public class TextGE : GeneratorBase
    {
         List<string> argsList=new List<string>();

      
         [LocalizedDisplayName("key_464")]
         [LocalizedDescription("key_465")]
         [PropertyOrder(2)]
         [PropertyEditor("CodeEditor")]
         public string Content { get; set; }

         [Browsable(false)]
         public override string KeyConfig => Content?.Substring(Math.Min(100,Content.Length)); 
        public TextGE()
         {
             Column = "text";
             Content = "";
         }

     

         public override bool Init(IEnumerable<IFreeDocument> datas)
         {
             if (string.IsNullOrEmpty(Content))
                 return base.Init(datas);

             try
             {
                 argsList =  Content.Split( new []{"\r\n"},StringSplitOptions.None).ToList();
                 
             }
             catch (Exception ex)
             {
                 XLogSys.Print.Error(ex.Message);


             }
             return base.Init(datas);
         }
         public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
         {
             return argsList.Select(doc => new FreeDocument {{this.Column, doc}});
         }
    }
}
