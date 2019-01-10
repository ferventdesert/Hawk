using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Octokit;

namespace HawkUnitTest
{
    internal class GitHubAPITest

    {
        [TestMethod]
        public async static  void Hello()
        {
            var client = new GitHubClient(new ProductHeaderValue("my-cool-app"));
          var content=  await  client.Repository.Content.GetAllContents("ferventdesert", "Hawk-Projects", "master");
           Console.WriteLine(content);
        }

     
    }
}
