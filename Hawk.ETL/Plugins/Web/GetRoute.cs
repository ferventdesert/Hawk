using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Web
{
    [XFrmWork("GetRoute","GetRoute_desc","map")]
    public  class GetRoute : BaiduSDKBase
    {

        public GetRoute()
        {
            ModeSelector=new ExtendSelector<string>(map.Keys);
            SourceCity = GlobalHelper.Get("key_577");
            DestCity = GlobalHelper.Get("key_577");

        }
        [LocalizedDisplayName("key_586")]
        [LocalizedDescription("key_579")]
        public string Dest { get; set; }

        [LocalizedDisplayName("key_587")]
        [LocalizedDescription("key_579")]
        public string SourceCity { get; set; }


        [LocalizedDisplayName("key_588")]
        [LocalizedDescription("key_579")]
        public string DestCity { get; set; }

        private Dictionary<string,string> map=new  Dictionary<string, string>() { {"公交","transit"}, {"驾车","driving"}, {"步行","walking"} };

        [LocalizedDisplayName("key_589")]
        [PropertyOrder(1)]
        public ExtendSelector<string> ModeSelector { get; set; }

        public override object TransformData(IFreeDocument datas)
        {
            //初始化方案信息实体类。
            var item = datas[Column];

            if (item == null)
                return null;
            try
            {
                var source = item.ToString();
                var dest = datas.Query( Dest);
                var sourcecity = datas.Query( SourceCity);
                var destcity = datas.Query(DestCity);
                var mode = map[ModeSelector.SelectItem];
                var key = $"{source},{dest},{sourcecity},{destcity},{mode}";
                var newlocation = buffHelper.Get(key);
                if (newlocation == null)
                {
                    //以 Get 形式请求 Api 地址
                    var region = "";
                    if (mode == "transit" || mode == "walking")
                    {
                        region = $"region={sourcecity}";
                    }
                    else
                    {
                        region = $"origin_region={sourcecity}&destination_region={destcity}";

                    }

                    var apiUrl =
                        $"http://api.map.baidu.com/direction/v1?mode={mode}&origin={source}&destination={dest}&{region}&output={format}&ak={apikey}";


                    //初始化方案信息实体类。
                    var result = HttpHelper.GetWebSourceHtml(apiUrl, "utf-8");
                    //以 Get 形式请求 Api 地址
                    //    var result = HttpHelper.DoGet(apiUrl, param);
                    dynamic info = serialier.DeserializeObject(result);
                    if (info["status"].ToInt32() == 0&& info["type"].ToInt32()==2)
                    {
                        var first= info["result"];
                        newlocation=new FreeDocument();
                    

                        if (mode == "transit")
                        {
                            newlocation["distance"] = first["routes"]["scheme"]["distance"];
                            newlocation["duration"] = first["routes"]["scheme"]["duration"];

                            newlocation["price"] = first["routes"]["scheme"]["price"];
                        }
                        else if (mode == "walking")
                        {
                            newlocation["distance"] = first["routes"][0]["distance"];
                            newlocation["duration"] = first["routes"][0]["duration"];

                        }
                        else
                        {
                            newlocation["distance"] = first["routes"][0]["distance"];
                            newlocation["duration"] = first["routes"][0]["duration"];
                           
                            newlocation["traffic_condition"] = first["traffic_condition"];
                            newlocation["toll"] = first["routes"]["toll"];
                        }

                    }
                    buffHelper.Set(item.ToString(), newlocation);
                }
                newlocation.DictCopyTo(datas);
            }
            catch (Exception )
            {
            }
            return true;
        }
    }
}
