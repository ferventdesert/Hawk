using System;
using System.ComponentModel;
using System.Web.Script.Serialization;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Plugins.Web
{
    [XFrmWork("检索附近","获取当前经纬度某一半径范围内的所有地物，需要拖入的为代表经度的列","map")]
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

        [LocalizedDisplayName("查询地物")]
        [LocalizedDescription("如公园，车站等")]
        public string Query { get; set; }

        [LocalizedDisplayName("纬度列")]
        [LocalizedDescription("代表纬度所在的列")]
        public string Lng { get; set; }

        [LocalizedDisplayName("搜索半径")]
        public int Radius { get; set; }


        [LocalizedDisplayName("所有结果")]
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
            }
            return true;
        }
    }
}