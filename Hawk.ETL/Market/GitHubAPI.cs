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
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Managements;
using ContentType = Octokit.ContentType;

namespace Hawk.ETL.Market
{
    public class GitHubAPI :  PropertyChangeNotifier 

    {
        public GitHubAPI()
        {
            client = new GitHubClient(new ProductHeaderValue("Hawk3"));
            DataMiningConfig.GetConfig().PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "MarketUrl")
                {
                    OnPropertyChanged("MarketShortUrl");
                }
            };

        }

        [Browsable(false)]
        public string MarketShortUrl
        {
            get
            {
                string username = "?";
                string project = "?";
                string target = "?";
                var MarketUrl = ConfigFile.GetConfig().Get<string>("MarketUrl");
                if (!this.GetRepoInfo(MarketUrl, out username, out project, out target))
                {

                    return null;
                }
                return username + "/" + project + "/" + target;


            }
        }



        public bool GetRepoInfo(string url, out string userName, out string repoName, out string dir)
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
            if (keys.Length > 4)
                dir = keys[4];
            return true;

        }


        public async Task<IEnumerable<ProjectItem>>  GetProjects(string url=null)
        {
            try
            {
              
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

        public  void Connect(string login,string password)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
         
            client.Credentials=new Credentials (login,password);
            isConnect = true;
        }

     
    }
}
