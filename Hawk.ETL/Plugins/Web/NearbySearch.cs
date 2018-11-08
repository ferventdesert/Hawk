using System;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.Core.Utils.Logs;
using Hawk.ETL.Crawlers;

namespace Hawk.ETL.Plugins.Web
{
    [XFrmWork("NearbySearch","NearbySearch_desc","map")]
    public class NearbySearch : BaiduSDKBase
    {
        public NearbySearch()
        {
            Lng = "pos_lng";
            Radius = 2000;
            this.PropertyChanged += (s, e) =>
            {
                buffHelper.Clear();
            };
            OneOutput = true;

        }

        [LocalizedDisplayName("key_592")]
        [LocalizedDescription("key_593")]
        public string Query { get; set; }

        [LocalizedDisplayName("key_594")]
        [LocalizedDescription("key_595")]
        public string Lng { get; set; }

        [LocalizedDisplayName("key_596")]
        public int Radius { get; set; }


        [LocalizedDisplayName("key_597")]
        public bool AllResult { get; set; }

        public override object TransformData(IFreeDocument datas)
        {
            //初始化方案信息实体类。
            var item = datas[Column];

            if (item == null)
                return null;
            try
            {
                var lat = item.ToString();
                var lng = datas[Lng].ToString();
                var bufkey = $"{lat},{lng}";
                var newlocation = buffHelper.Get(bufkey);
                if (newlocation == null)
                {
                    var apiUrl =
                        $"http://api.map.baidu.com/place/v2/search?ak={apikey}&output={format}&query={Query}&page_size=10&page_num=0&scope=2&location={lat},{lng}&radius={Radius}";
                   
                    var result = HttpHelper.GetWebSourceHtml(apiUrl, "utf-8");
                    if (AllResult)
                    {
                        return result;
                    }
                    //以 Get 形式请求 Api 地址
                    //    var result = HttpHelper.DoGet(apiUrl, param);
                    dynamic info = serialier.DeserializeObject(result); 
                    if (info["status"].ToInt32() == 0)
                    {
                        var first = info["results"][0];
                        newlocation = new FreeDocument();

                        newlocation[Query] = first["name"];
                        newlocation[Query+"_lat"] = first["location"]["lat"];
                        newlocation[Query+"_lng"] = first["location"]["lng"];
                        newlocation[Query+"_distance"] = first["detail_info"]["distance"];
                    }
                    buffHelper.Set(bufkey, newlocation);
                }
                newlocation.DictCopyTo(datas);
            }
            catch (Exception ex)
            {
                XLogSys.Print.Warn(ex);
            }
            return true;
        }
    }
}