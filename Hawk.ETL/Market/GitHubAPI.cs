using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using System.Net;
using System.Web.Security;
using Fiddler;
using Hawk.ETL.Managements;

namespace Hawk.ETL.Market
{
    public class GitHubAPI

    {

        public string ProjectName { get; set; }

        public string UserName { get; set; }

        public string TargetDir { get; set; }

        public async Task<IEnumerable<ProjectItem>>  GetProjects(string username=null, string project= null,string target=null)
        {
            if (string.IsNullOrEmpty(username))
                username = "ferventdesert";
            if (string.IsNullOrEmpty(project) )
                project = "Hawk-Projects";
            if (string.IsNullOrEmpty(target) )
                target = "链家";

            var result = await client.Repository.Content.GetAllContents(username, project, target);
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
        public GitHubClient client;
        public  void Connect()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
             client = new GitHubClient(new ProductHeaderValue("my-Hawk-app"));
            return;
            
            try
            {
                var clientId = "some-id-here";
                var clientSecret = "some-id-here";

                // NOTE: this is not required, but highly recommended!
                // ask the ASP.NET Membership provider to generate a random value 
                // and store it in the current user's session
                string csrf = Membership.GeneratePassword(24, 1);

                var request = new OauthLoginRequest(clientId)
                {
                    Scopes = { "user", "notifications" },
                    State = csrf
                };

                // NOTE: user must be navigated to this URL
                var oauthLoginUrl = client.Oauth.GetGitHubLoginUrl(request);
                Console.WriteLine(oauthLoginUrl);

                var miscellaneousRateLimit =  client.Miscellaneous.GetRateLimits().Result;

                //  The "core" object provides your rate limit status except for the Search API.
                var coreRateLimit = miscellaneousRateLimit.Resources.Core;

                var howManyCoreRequestsCanIMakePerHour = coreRateLimit.Limit;
                var howManyCoreRequestsDoIHaveLeft = coreRateLimit.Remaining;
                var whenDoesTheCoreLimitReset = coreRateLimit.Reset; // UTC time

                // the "search" object provides your rate limit status for the Search API.
                var searchRateLimit = miscellaneousRateLimit.Resources.Search;

                var howManySearchRequestsCanIMakePerMinute = searchRateLimit.Limit;
                var howManySearchRequestsDoIHaveLeft = searchRateLimit.Remaining;
                var whenDoesTheSearchLimitReset = searchRateLimit.Reset; // UTC time


                var content =  client.Repository.Content.GetAllContents("ferventdesert", "Hawk-Projects","链家").Result;
                var first=content.FirstOrDefault(d => d.Name.Contains("2020")).DownloadUrl;
                var proj=    Hawk.ETL.Managements.Project.LoadFromUrl(first); 
                Console.WriteLine(proj);

            }
            catch (Exception ex)
            {
                
                throw;
            }
        
        }

    }
}
