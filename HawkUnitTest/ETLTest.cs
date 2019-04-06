using System;
using System.Collections.Generic;
using System.Linq;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using Hawk.ETL.Plugins.Filters;
using Hawk.ETL.Plugins.Generators;
using Hawk.ETL.Plugins.Transformers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HawkUnitTest
{
    [TestClass]
    public class ETLTest
    {

      
       
        public List<IColumnProcess> GetColumnProcesses()
        {
            var list = new List<IColumnProcess>();
            list.Add(new TextGE {Column = "a", Content = "a\nb\nc"});
            list.Add(new MergeTF {Column = "a", Format = "{0}123"});
            list.Add(new ToListTF());
            list.Add(new RangeGE() {Column = "a",  MaxValue = "20",MinValue = "[a]", MergeType =  MergeType.Cross});
            return list;
        }

        [TestMethod]
        public void GetDatas()
        {
            var process = GetColumnProcesses();
            ToListTF split;
            var splitPoint = process.GetParallelPoint(true, out split);
            Assert.IsNotNull(split);
            process.Execute(d=>d.Init(new List<IFreeDocument>()));
            var list = process.Aggregate(isexecute: true)().ToList();

            Assert.IsTrue(list.Count>20);
            foreach (var item in list)
            {
                Console.WriteLine(item);
            }

        }

    }
}