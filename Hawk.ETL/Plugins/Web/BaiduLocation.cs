using System;
using System.ComponentModel;
using System.Web.Script.Serialization;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Plugins.Transformers;

namespace Hawk.ETL.Plugins.Web
{
    public class BaiduSDKBase : TransformerBase
    {
        protected readonly BuffHelper<FreeDocument> buffHelper = new BuffHelper<FreeDocument>(50);
        protected string apikey = "84675e2004a16456ad3ccf23a408439c"; //
        protected string format = "json";
        protected JavaScriptSerializer serialier;

        public BaiduSDKBase()
        {
            OneOutput = false;
            serialier = new JavaScriptSerializer();
        }
    }

    [XFrmWork("搜索位置","通过百度API获取当前地标的经纬度坐标，需要拖入代表地名的列","location")]
    public class BaiduLocation : BaiduSDKBase
    {
        public BaiduLocation()
        {
            Region = "北京";
        }

        [LocalizedDisplayName("所属地市")]
        [LocalizedDescription("通过地市进行信息检索")]
        public string Region { get; set; }


        [LocalizedDisplayName("标签")]
        [LocalizedDescription("如医院，美食等")]
        public string Tag { get; set; }

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


                    var r = datas.Query(Region);
                    var tag = datas.Query(Tag);
                    var apiUrl =
                        $"http://api.map.baidu.com/place/v2/search?q={item}&region={r}&tag={tag}&output={format}&ak={apikey}";


                    //初始化方案信息实体类。
                    var result = HttpHelper.GetWebSourceHtml(apiUrl, "utf-8");
                    //以 Get 形式请求 Api 地址
                    //    var result = HttpHelper.DoGet(apiUrl, param);
                    dynamic info =  serialier.DeserializeObject(result);
                  //  if (info[0]["status"].ToInt32() == 0)
                    {
                        newlocation = Parse(info);
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

        protected virtual FreeDocument Parse(dynamic info)
        {
            var first = info["results"][0];
            var newlocation = new FreeDocument();
            foreach (var item in first)
            {
                newlocation[item.Key] = item.Value;
            }
            newlocation["pos_lat"] = first["location"]["lat"];
            newlocation["pos_lng"] = first["location"]["lng"];

            return newlocation;
        }
    }
}