using System;
using System.Globalization;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Web
{
    [XFrmWork("身份证信息转换","提取身份证中的基本信息")]
    public class IDCardTF : TransformerBase
    {

        public IDCardTF()
        {
            OneOutput = false;
        }

        public override object TransformData(IFreeDocument datas)
        {
            object item = datas[Column];
            if (item == null) return null;
            string str = item.ToString();
            if (str.Length != 18) return null;
            datas.Add("省ID", str.Substring(0, 2));
            datas.Add("城市ID", str.Substring(2, 2));

            DateTime time;
            if (DateTime.TryParseExact(str.Substring(6, 8), "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None,
                out time))
            {
                datas.Add("出生日期", time);
            }
            int sex;
            if (int.TryParse(str[16].ToString(), out sex))
            {
                datas.Add("性别", sex%2 == 1
                    ? "男"
                    : "女");
            }

            return datas;
        }
    }
}