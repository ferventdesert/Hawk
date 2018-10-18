namespace Hawk.Standard.Plugins.Transformers
{
    [XFrmWork("NumberTF","NumberTF_desc" )]
    public class NumberTF : RegexTF
    {
        public NumberTF()
        {
            Script = @"(-?\d+)(\.\d+)?";
            Index = 0;
        }

                
    }
}