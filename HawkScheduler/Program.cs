using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using CommandLine;
using CommandLine.Text;
using Hawk.Core.Utils;
using Hawk.Core.Utils.Logs;
using Hawk.ETL.Crawlers;
using Hawk.ETL.Interfaces;
using Hawk.ETL.Managements;
using Hawk.ETL.Process;
using log4net.Config;

namespace HawkScheduler
{
    public enum WorkMode
    {
        serial,
        parallel
    }

    public sealed class Options
    {
        [Option('p', "read", MetaValue = "FILE", Required = true, HelpText = "Input Hawk project xml file name.")]
        public string ProjectFile { get; set; }

        [Option('t', "read", Required = true, MetaValue = "STRING",
            HelpText = "etl task name in project")]
        public string TaskName { get; set; }

        [Option("mode", HelpText = "work mode| serial,parallel")]
        public WorkMode WorkMode { get; set; }

        [Option("i", HelpText = "If file has errors don't stop processing.")]
        public bool IgnoreErrors { get; set; }

        // Marking a property of type IParserState with ParserStateAttribute allows you to
        // receive an instance of ParserState (that contains a IList<ParsingError>).
        // This is equivalent from inheriting from CommandLineOptionsBase (of previous versions)
        // with the advantage to not propagating a type of the library.
        //
        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }


    internal class Program
    {
        public static string url = "http://www.cnblogs.com/";

        private static void unitTest()
        {
            var doc = XPathAnalyzer.GetHtmlDocument(url);

            var datas = XPathAnalyzer.GetDataFromURL(url);

            var properties = doc.DocumentNode.SearchPropertiesSmartList();


            var firstOrDefault = properties.FirstOrDefault();
            datas = doc.DocumentNode.GetDataFromXPath(firstOrDefault.CrawItems).ToList();
        }

        private static void Main(string[] args)
        {
            //unitTest();
            var options = new Options();
            var parser = new Parser(with => with.HelpWriter = Console.Error);

            if (parser.ParseArgumentsStrict(args, options, () => Environment.Exit(-2)))
            {
                Run(options);
            }
        }

        private static void Run(Options options)
        {
            Console.WriteLine();
            XmlConfigurator.Configure(new FileInfo("log4net_cmd.config"));

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval=TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => { Console.WriteLine("fuck");};
            timer.Start();

            try
            {
                XLogSys.Print.Info($"Project file: {options.ProjectFile} ...");
                var container = new CommandLineContainer();

                var processManager = container.PluginDictionary["DataProcessManager"] as DataProcessManager;
                if (processManager == null)
                {
                    throw new Exception("Plugin System ERROR, DataProcessManager not Found, Reinstall Hawk");
                }
            
                var dataManager = container.PluginDictionary["DataManager"] as DataManager;

                if (dataManager == null)
                {
                    throw new Exception("Plugin System ERROR, dataManager not Found, Reinstall Hawk");
                }

                AppHelper.LoadLanguage();

                var project = Project.Load(options.ProjectFile);
                if (project == null)
                {
                    throw new Exception($"Project {options.ProjectFile} is not exists or format error");
                }

                project.DataCollections?.Execute(d => dataManager.AddDataCollection(d));
                processManager.CurrentProject = project;
                var task = project.Tasks.FirstOrDefault(d => d.Name == options.TaskName);

                if (task == null)
                {
                    foreach (var _task in project.Tasks)
                    {
                        Console.WriteLine(_task.Name);
                    }
                    throw  new Exception($"task {options.TaskName} not in project");
                  
                }
                task.Load(false);
                XLogSys.Print.Info("project load successful");
                var realTask =
                    processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == options.TaskName) as
                        SmartETLTool;
                var queueList = processManager.CurrentProcessTasks as ObservableCollection<TaskBase>;
                queueList.CollectionChanged += (s, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        foreach (var item in e.NewItems.OfType<TaskBase>())
                        {
                            XLogSys.Print.Info($"task add: {item.Name}");
                            item.PropertyChanged += (s2, e2) =>
                            {
                                if (e2.PropertyName == "Percent")
                                {
                                    XLogSys.Print.Info($"task {item.Name}, percent {item.Percent}");
                                }
                            };
                        }
                    }
                    else
                    {
                        foreach (var item in e.OldItems.OfType<TaskBase>())
                        {
                            XLogSys.Print.Info($"task finished: {item.Name}");
                        }
                    }
                    if (queueList.Count == 0)
                    {
                        Console.WriteLine("task all finished: quit");
                        // Environment.Exit(0); //0代表正常退出，非0代表某种错误的退出
                    }
                };


                realTask.ExecuteDatas();
            }
            catch (Exception e)
            {
                XLogSys.Print.Error(e.Message, e);
                Console.ReadKey();
                Environment.Exit(1);
            }
            while (true)
            {
                var key = Console.ReadLine();
                if (key == "exit")
                    Environment.Exit(0);
            }
        }
    }
}