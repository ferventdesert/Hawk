using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
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

    public class CommandLineContainer : IMainFrm, IDockableManager
    {
        public CommandLineContainer()
        {
            PluginManager = new PluginManager();
            MainDescription.IsUIForm = false;
            MainDescription.MainFrm = this;
            PluginManager.MainFrmUI = this;
            var MainStartUpLocation = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            ;
            PluginManager.Init(new[] {MainStartUpLocation});
            PluginManager.LoadPlugins();
        }

        public PluginManager PluginManager { get; set; }

        public void AddDockAbleContent(FrmState thisState, object thisControl, params string[] objects)
        {
        }

        public void RemoveDockableContent(object model)
        {
        }

        public void ActiveThisContent(object view)
        {
        }

        public void ActiveThisContent(string name)
        {
        }

        public event EventHandler<DockChangedEventArgs> DockManagerUserChanged;
        public List<ViewItem> ViewDictionary { get; }

        public void SetBusy(bool isBusy, string title = "系统正忙", string message = "正在处理长时间操作", int percent = 0)
        {
        }

        public ObservableCollection<IAction> CommandCollection { get; set; }
        public string MainPluginLocation { get; }
        public Dictionary<string, IXPlugin> PluginDictionary { get; set; }
        public event EventHandler<ProgramEventArgs> ProgramEvent;

        public void InvokeProgramEvent(ProgramEventArgs e)
        {
        }
    }


    internal class Program
    {
        private static void Main(string[] args)
        {
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
            Console.WriteLine("project file: {0} ...", options.ProjectFile);
            var container = new CommandLineContainer();

            var processManager = container.PluginDictionary["模块管理"] as DataProcessManager;
            var project = ProjectItem.LoadProject(options.ProjectFile);

            XmlConfigurator.Configure(new FileInfo("log4net_cmd.config"));
            processManager.CurrentProject = project;
            var task = project.Tasks.FirstOrDefault(d => d.Name == options.TaskName);
            if (task == null)
            {
                Console.WriteLine("task not in project, project task lists:");
                foreach (var _task in project.Tasks)
                {
                    Console.WriteLine(_task.Name);
                }
                Console.ReadKey();
            }
            task.Load(false);
            Console.WriteLine("projec load successful");
            var realTask =
                processManager.CurrentProcessCollections.FirstOrDefault(d => d.Name == options.TaskName) as SmartETLTool;
            var queuelists = processManager.CurrentProcessTasks as ObservableCollection<TaskBase>;
            queuelists.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (var item in e.NewItems.OfType<TaskBase>())
                    {
                        Console.WriteLine("task add: {0}", item.Name);
                        item.PropertyChanged += (s2, e2) =>
                        {
                            if (e2.PropertyName == "Percent")
                            {
                                Console.WriteLine($"task {item.Name}, percent {item.Percent}");
                            }
                        };
                    }
                }
                else
                {
                    foreach (var item in e.OldItems.OfType<TaskBase>())
                    {
                        Console.WriteLine("task finished: {0}", item.Name);
                    }
                }
                if (queuelists.Count == 0)
                {
                    Console.WriteLine("task all finished: quit");
                    Environment.Exit(0); //0代表正常退出，非0代表某种错误的退出
                }
            };
            realTask.ExecuteDatas();
            while (true)
            {
                var key = Console.ReadLine();
                if (key == "exit")
                    Environment.Exit(0);
            }
        }
    }
}