using System.Collections.Generic;
using Hawk.Core.Connectors;

namespace Hawk.ETL.Interfaces
{
    public interface IColumnDataTransformer : IColumnProcess
    {
        string NewColumn { get; set; }

        /// <summary>
        ///     自主管理数据，不需要外部干涉
        /// </summary>
        bool OneOutput { get; }

        object TransformData(IFreeDocument datas);

        /// <summary>
        ///  是否会转换出多个数据
        /// </summary>
        bool IsMultiYield { get; }

        IEnumerable<IFreeDocument> TransformManyData(IEnumerable<IFreeDocument> datas);



    }
}