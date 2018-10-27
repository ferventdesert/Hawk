using System;
using System.Collections.Generic;
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
            ProjectName = new ExtendSelector<string>();
            TargetDir=new ExtendSelector<string>();
            client = new GitHubClient(new ProductHeaderValue("Hawk3"));
            this.PropertyChanged += async  (s, e) =>  
            {
                if(!isConnect)
                    return;
                if (e.PropertyName== "RepoUserName"&&string.IsNullOrEmpty(RepoUserName) == false)
                {
                    var result = await client.Repository.GetAllForUser(RepoUserName);
                    ProjectName.SetSource(result.Select(d=>d.Name));
                }

             

            };
            this.ProjectName.SelectChanged += async (s, e) =>
            {
                if (string.IsNullOrEmpty(ProjectName.SelectItem) == false)
                {
                    var result = await client.Repository.Content.GetAllContents(RepoUserName, ProjectName.SelectItem);
                    TargetDir.SetSource(result.Where(d => d.Type == ContentType.Dir).Select(d => d.Name));
                }

            };
            TargetDir.
        }

        [PropertyOrder(3)]
        public ExtendSelector<string> ProjectName { get; set; }

        [PropertyOrder(2)]
        public string RepoUserName
        {
            get { return _repoUserName; }
            set
            {
                if (_repoUserName != value)
                {
                    _repoUserName = value;
                    OnPropertyChanged("RepoUserName");
                }
            }
        }

        [PropertyOrder(4)]
        public ExtendSelector<string> TargetDir { get; set; }

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

        public async Task<IEnumerable<ProjectItem>>  GetProjects(string username=null, string project= null,string target=null)
        {
            if (string.IsNullOrEmpty(username))
                username = "ferventdesert";
            if (string.IsNullOrEmpty(project) )
                project = "Hawk-Projects";
            if (string.IsNullOrEmpty(target) )
                target = "Hawk3";
            IReadOnlyList<RepositoryContent> result = null;

            result = await client.Repository.Content.GetAllContents(username, project, target);
            if (result == null)
                return null;
           
            var items=  result.Where(d => d.Type == ContentType.File&&(d.Name.EndsWith(".xml",true,null)||d.Name.EndsWith(".hproj",true,null))).Select(
                  d =>
                {
                    var projectItem = new ProjectItem()
                    {
                        IsRemote = true,
                        SavePath = d.DownloadUrl
                    };
                    var suffix = d.Name.Split('.').Last();
                    var name = d.Name.Replace("."+suffix, "");
                    projectItem.Name = name;
                    var meta = name + ".meta";
                    var metafile = result.FirstOrDefault(d2 => d2.Name == meta);
                    if (metafile != null)
                    {
                        var response =  WebRequest.Create(metafile.DownloadUrl).GetResponse().GetResponseStream();
                        using (StreamReader reader = new StreamReader(response,Encoding.UTF8))
                        {
                            var item= reader.ReadToEnd();
                            var metainfo = ParameterItem.GetParameters(item);
                            projectItem.DictDeserialize(metainfo.ToDictionary(d2=>d2.Key,d2=>(object)d2.Value));
                        }
                    }
                    return projectItem;

                });
            return items;
        } 
        private GitHubClient client;
        private bool isConnect = false;
        private string _repoUserName;

        public  void Connect()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
         
            client.Credentials=new Credentials (Login,Password);
            isConnect = true;
        }

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            throw new NotImplementedException();
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            throw new NotImplementedException();
        }
    }
}
