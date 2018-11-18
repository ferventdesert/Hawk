using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using System.Net;
using System.Web.Security;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using Fiddler;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Managements;
using ContentType = Octokit.ContentType;

namespace Hawk.ETL.Market
{
    public class GitHubAPI :  PropertyChangeNotifier, IDictionarySerializable

    {
        public GitHubAPI()
        {
            client = new GitHubClient(new ProductHeaderValue("Hawk3"));
            Login = "hawkpublic@yeah.net";
            Password = "hawk1qaz2wsx";
            IsKeepPassword = true;
            MarketUrl = "https://github.com/ferventdesert/Hawk-Projects/tree/master/Hawk3";
        }

        [Browsable(false)]
        public string MarketShortUrl
        {
            get
            {
                string username = "?";
                string project = "?";
                string target = "?";
                if (!this.GetRepoInfo(MarketUrl, out username, out project, out target))
                {
                   
                    return null;
                }
                return username + "/" + project + "/"+target;


            }
        }

        [LocalizedDescription("market_url_check")]
        [LocalizedDisplayName("market_url")]
        public string MarketUrl
        {
            get { return _marketUrl; }
            set
            {
                if (_marketUrl != value)
                {
                    _marketUrl = value;
                    OnPropertyChanged("MarketUrl");
                    OnPropertyChanged("MarketShortUrl");
                }
            }
        }

        public bool GetRepoInfo(string url,out string userName,out string repoName,out string dir)
        {
            userName = null;
            repoName = null;
            dir = null;
            var github = "https://github.com/";
            if (url.StartsWith(github) == false)
            {
                return false;
            }
            url = url.Replace(github, "");
            var keys = url.Split('/');
            if (keys.Count() < 4)
                return false;
            userName = keys[0];
            repoName = keys[1];
            var branch = keys[3];
            if (keys.Length > 4)
                dir = keys[4];
            return true;

        }

        [LocalizedCategory("user_login")]   
        [LocalizedDisplayName("key_25")]
        [PropertyOrder(0)]
        public string Login { get; set; }

        [LocalizedDisplayName("key_26")]
        [LocalizedCategory("user_login")]
        [PropertyOrder(1)]
        [PropertyEditor("PasswordEditor")]
        public string Password { get; set; }

        [LocalizedDisplayName("keep_pass")]
        [LocalizedCategory("user_login")]
        [PropertyOrder(2)]
        public bool IsKeepPassword { get; set; }

        public async Task<IEnumerable<ProjectItem>>  GetProjects(string url=null)
        {
            try
            {
                if (url == null)
                {
                    url = this.MarketUrl;
                }
                string username = null;
                string project = null;
                string target = null;
                if (!this.GetRepoInfo(url, out username, out project, out target))
                {
                    XLogSys.Print.Error(GlobalHelper.Get("market_url_check"));
                    return null;
                }

                IReadOnlyList<RepositoryContent> result = null;

                result = await client.Repository.Content.GetAllContents(username, project, target);

                var items = result?.Where(d => d.Type == ContentType.File && (d.Name.EndsWith(".xml", true, null) || d.Name.EndsWith(".hproj", true, null))).Select(
                    d =>
                    {
                        var projectItem = new ProjectItem()
                        {
                            IsRemote = true,
                            SavePath = d.DownloadUrl
                        };
                        var suffix = d.Name.Split('.').Last();
                        var name = d.Name.Replace("." + suffix, "");
                        projectItem.Name = name;
                        var meta = name + ".meta";
                        var metafile = result.FirstOrDefault(d2 => d2.Name == meta);
                        if (metafile != null)
                        {
                            Task.Factory.StartNew(() =>
                            {
                                var response = WebRequest.Create(metafile.DownloadUrl).GetResponse().GetResponseStream();
                                using (StreamReader reader = new StreamReader(response, Encoding.UTF8))
                                {
                                    var item = reader.ReadToEnd();
                                    var metainfo = ParameterItem.GetParameters(item);
                                    ControlExtended.UIInvoke(() =>
                                    {
                                        projectItem.DictDeserialize(metainfo.ToDictionary(d2 => d2.Key, d2 => (object)d2.Value));
                                    });
                                  
                                }
                            });
                         
                        }
                        return projectItem;

                    });
                return items;
            }
            catch (Exception ex)
            {
                XLogSys.Print.Error(ex.Message);
                return new List<ProjectItem>();
            }
          
        } 
        private GitHubClient client;
        private bool isConnect = false;
        private string _marketUrl;

        public  void Connect()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
         
            client.Credentials=new Credentials (Login,Password);
            isConnect = true;
        }

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
           var dict=new FreeDocument();
            dict.Add("Login", Login);
            if (IsKeepPassword)
                dict.Add("Password", Password);
            dict.Add("IsKeepPassword", IsKeepPassword);
            dict.Add("MarketUrl", MarketUrl);
            return dict;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Login = docu.Set("Login", Login);
            IsKeepPassword = docu.Set("IsKeepPassword", IsKeepPassword);
            if(IsKeepPassword)
                Password = docu.Set("Password", Password);

            MarketUrl = docu.Set("MarketUrl", MarketUrl);
        }
    }
}
