using System;
using System.Web.Script.Serialization;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Web
{
    [XFrmWork("获取IP的坐标","获取某一ip地址的经纬度坐标", "location")]
    public class GetIPLocation : BaiduSDKBase
    {
        public override object TransformData(IFreeDocument datas)
        {
            //初始化方案信息实体类。
            var item = datas[Column];

            if (item == null)
                return null;
            try
            {
                var newlocation = buffHelper.Get(item.ToString());
                if (newlocation == null)
                {
                    //以 Get 形式请求 Api 地址


                    var apiUrl =
                        $"http://api.map.baidu.com/location/ip?ak={apikey}&ip={item}&coor=bd09ll";

                    //初始化方案信息实体类。
                    var result = HttpHelper.GetWebSourceHtml(apiUrl, "utf-8");
                    //以 Get 形式请求 Api 地址
                    //    var result = HttpHelper.DoGet(apiUrl, param);
                    dynamic info = serialier.DeserializeObject(result);
                    if (info["status"].ToInt32() == 0)
                    {
                        newlocation = new FreeDocument();
                        newlocation["ip_content"] = info["address"];
                        newlocation["pos_lat"] = info["content"]["point"]["x"];
                        newlocation["pos_lng"] = info["content"]["point"]["y"];
                    }
                    buffHelper.Set(item.ToString(), newlocation);
                }
                newlocation.DictCopyTo(datas);
            }
            catch (Exception ex)
            {
            }
            return true;
        }
    }
}