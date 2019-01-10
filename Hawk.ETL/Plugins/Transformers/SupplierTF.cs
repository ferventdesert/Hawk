using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Process;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("SupplierTF", "SupplierTF_desc")]
    public class SupplierTF : TransformerBase
    {
        public SupplierTF()
        {
            IsMultiYield = true;
            InnerExecute = false;
        }
        [PropertyOrder(2)]
        [LocalizedDisplayName("exe_inner")]
        public bool InnerExecute { get; set; }
        private string[] checkKeys = "__SysObjectID __SysETL".Split(' ');
        protected override IEnumerable<IFreeDocument> InternalTransformManyData(IFreeDocument datas)
        {
        
            if(!checkKeys.All(checkKey => datas.Keys.Contains(checkKey)))
                yield return datas;
            var etlName=datas["__SysETL"].ToString();
            var etl =  this.GetTask<SmartETLTool>(etlName);
            if(etl==null)
                throw new Exception(String.Format("task not exists {0}",etlName));
            var process = etl.CurrentETLTools.FirstOrDefault(d => d.ObjectID == datas["__SysObjectID"].ToString());
            if(process==null)
                throw new Exception(String.Format("module not exists {0}",etlName));
            var index = etl.CurrentETLTools.IndexOf(process);
            foreach (var doc in etl.CurrentETLTools.Skip(index).Generate(this.IsExecute&&InnerExecute, new List<IFreeDocument>() {datas},this.Father.Analyzer))
            {
                yield return doc;
            }
        }
    }
}
