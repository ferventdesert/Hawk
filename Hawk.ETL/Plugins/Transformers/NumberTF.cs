using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("提取数字","提取当前列中出现的数值" )]
    public class NumberTF : RegexTF
    {
        public NumberTF()
        {
            Script = @"(-?\d+)(\.\d+)?";
            Index = 0;
        }

                
    }
}