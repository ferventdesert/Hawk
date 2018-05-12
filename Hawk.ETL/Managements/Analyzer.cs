using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hawk.Core.Utils;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Managements
{
    public class AnalyzeItem
    {
        public IColumnProcess Process { get; set; }
        public int Input { get; set; }
        public int Output { get; set; }
        public int Error { get; set; }

        public bool HasInit { get; set; }

        public bool MightError
        {
            get { return Input != 0 && Output == 0 && Process.Enabled == true && HasInit == false; }
        }

        public int EmptyInput { get; set; }
    }
    public class Analyzer
    {
        public Analyzer()
        {
            Items=new List<AnalyzeItem>(); 
        }

        public AnalyzeItem Set(IColumnProcess process)
        {
            var item= new AnalyzeItem();
            item.Process = process;
            Items.Add(item);
            return item;
        }
        public List<AnalyzeItem>  Items { get; set; }


    }

}
