using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Transformers
{
    [XFrmWork("NumberTF","NumberTF_desc" )]
    public class NumberTF : RegexTF
    {
        public NumberTF()
        {
            Script = @"(-?\d+)(\.\d+)?";
            Index = "0";
        }

                
    }
}