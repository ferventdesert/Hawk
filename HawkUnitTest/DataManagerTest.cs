using Hawk.Core.Utils;
using Hawk.ETL.Managements;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HawkUnitTest
{
    [TestClass]
    public class DataManagerTest
    {
        private CommandLineContainer container;
        private DataManager dataManager;
        private DataProcessManager processManager;
        private Project project;


        [TestInitialize]
        public void Initialize()
        {
            container = new CommandLineContainer();
            Assert.IsTrue(container.PluginDictionary.Count>0);

            processManager = container.PluginDictionary["DataProcessManager"] as DataProcessManager;
            processManager.CreateNewProject();
            project = processManager.CurrentProject;
            dataManager = container.PluginDictionary["DataManager"] as DataManager;
            project.DataCollections.Execute(d => dataManager.AddDataCollection(d));
            processManager.CurrentProject = project;
        }


        [TestMethod]
        public void TestMethod1()
        {

            Assert.IsTrue(processManager != null);
            Assert.IsTrue(processManager != null);
            Assert.IsTrue(project != null);

        }
        
    }
}