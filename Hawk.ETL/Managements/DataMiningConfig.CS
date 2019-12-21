using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Input;
using Hawk.Core.Connectors;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Managements
{
    /// <summary>
    ///     基础操作文件
    /// </summary>
    [XFrmWork("Hawk系统级配置" )]
    public class DataMiningConfig : ConfigFile
    {
        #region Constants and Fields

        public DataMiningConfig()
        {
            Projects = new List<ProjectItem>();
            MaxForbidCount = 10;
            IsAddErrorCollection = true;
            AutoSaveTime = 300;
            FiddlerPort = 8888;
            AutoStartStopFiddler = true;
            Login = "hawkpublic@yeah.net";
            Password = "hawk1qaz2wsx";
            IsKeepPassword = true;
            MarketUrl = "https://github.com/ferventdesert/Hawk-Projects/tree/master/Hawk3";
        }


      



        #endregion

        public override string SavePath => GetXMLFileLocation("MainConfig");

        [PropertyOrder(0)]
        [LocalizedCategory("http_header")]
        [LocalizedDisplayName("key_126")]
        public int RequestCount
        {
            get { return _requestCount; }
            set
            {
                if (_requestCount != value)
                {
                    _requestCount = value;
                    OnPropertyChanged("RequestCount");
                }
            }
        }

        [PropertyOrder(1)]
        [LocalizedCategory("smartetl_name")]
        [LocalizedDisplayName("auto_save_time")]
        [LocalizedDescription("auto_save_time_desc")]
        public int AutoSaveTime
        {
            get { return this.Get<int>("AutoSaveTime"); }
            set

            {
                if (value < 60&&value>0)
                    value = 60;
                this.Set<int>("AutoSaveTime", value);
            }
        }
      //[Browsable(false)] 
        public string  Language
        {
            get { return this.Get<string>("Language"); }
            set
            {
                this.Set<string>("Language", value);


            }
        }




        [PropertyOrder(1)]
        [LocalizedCategory("smartetl_name")]
        [LocalizedDisplayName("is_generate_error_collection")]
        [LocalizedDescription("is_generate_error_collection_desc")]
        public bool IsAddErrorCollection
        {
            get { return this.Get<bool>("IsAddErrorCollection"); }
            set
            {
                this.Set<bool>("IsAddErrorCollection", value);


            }
        }


        [PropertyOrder(2)]
        [LocalizedCategory("smartetl_name")]
        [LocalizedDisplayName("is_display_desc")]
        public bool IsDisplayDetail
        {
            get { return this.Get<bool>("IsDisplayDetail"); }
            set
            {
                this.Set<bool>("IsDisplayDetail", value);


            }
        }


        [PropertyOrder(1)]
        [LocalizedCategory("http_header")]
        [LocalizedDisplayName("key_127")]
        public int ForbidCount
        {
            get { return _forbidCount; }
            set
            {
                if (_forbidCount != value)
                {
                    _forbidCount = value;
                    OnPropertyChanged("ForbidCount");
                }
            }
        }

        [PropertyOrder(1)]
        [LocalizedCategory("key_641")]
        [LocalizedDisplayName("key_119")]
        public int FiddlerPort 
        {
            get { return this.Get<int>("FiddlerPort"); }
            set
            {
                this.Set<int>("FiddlerPort", value);


            }
        }

        [PropertyOrder(2)]
        [LocalizedCategory("key_641")]
        [LocalizedDisplayName("auto_startstop_fiddler")]
        public bool AutoStartStopFiddler 
        {
            get { return this.Get<bool>("AutoStartStopFiddler"); }
            set
            {
                this.Set<bool>("AutoStartStopFiddler", value);
            }
        }
        [LocalizedCategory("user_login")]
        [LocalizedDescription("market_url_check")]
        [LocalizedDisplayName("market_url")]
        public string MarketUrl
        {
            get { return   this.Get<string>("MarketUrl"); ; }
            set
            {
                this.Set<string>("MarketUrl", value);
              
            }
        }
        [LocalizedCategory("user_login")]
        public bool AutoConnectGithub
        {
            get { return this.Get<bool>("AutoConnectGithub"); ; }
            set
            {
                this.Set<bool>("AutoConnectGithub", value);

            }
        }

        [LocalizedDisplayName("keep_pass")]
        [LocalizedCategory("user_login")]
        [PropertyOrder(2)]
        public bool IsKeepPassword
        {
            get { return this.Get<bool>("IsKeepPassword"); }
            set
            {
                this.Set("IsKeepPassword", value);


            }
        } 



        [LocalizedCategory("user_login")]
        [LocalizedDisplayName("key_25")]
        [PropertyOrder(0)]
        public string Login
        {
            get { return this.Get<string>("Login"); }
            set
            {
                this.Set<string>("Login", value);


            }
        }


        [LocalizedDisplayName("key_26")]
        [LocalizedCategory("user_login")]
        [PropertyOrder(1)]
        [PropertyEditor("PasswordEditor")]
        public string Password
        {
            get { return this.Get<string>("Password"); }
            set
            {
                this.Set<string>("Password", value);


            }
        }



        [PropertyOrder(2)]
        [LocalizedCategory("http_header")]
        [LocalizedDisplayName("key_128")]
        public int MaxForbidCount
        {
            get { return _maxForbidCount; }
            set
            {
                if (_maxForbidCount != value)
                {
                    _maxForbidCount = value;
                    OnPropertyChanged("MaxForbidCount");
                }
            }
        }

        [PropertyOrder(3)]
        [LocalizedCategory("http_header")]
        [LocalizedDisplayName("key_129")]
        public int ParseErrorCount
        {
            get { return _parseErrorCount; }
            set
            {
                if (_parseErrorCount != value)
                {
                    _parseErrorCount = value;
                    OnPropertyChanged("ParseErrorCount");
                }
            }
        }

        private int _requestCount;
        private int _forbidCount;
        private int _maxForbidCount;
        private int _parseErrorCount;
        private int _timeoutCount;


        [PropertyOrder(4)]
        [LocalizedCategory("http_header")]
        [LocalizedDisplayName("key_130")]
        public int TimeoutCount
        {
            get { return _timeoutCount; }
            set
            {
                if (_timeoutCount != value)
                {
                    if (_timeoutCount != value)
                    {
                        _timeoutCount = value;
                        OnPropertyChanged("TimeoutCount");
                    }
                }
            }
        }


     
        [PropertyOrder(8)]
        [LocalizedCategory("http_header")]
        [LocalizedDisplayName("DisplayPopupMenu")]
        public bool DisplayPopupMenu
        {
            get { return this.Get<bool>("DisplayPopupMenu"); }
            set
            {
                this.Set<bool>("DisplayPopupMenu", value);


            }
        }





        [PropertyOrder(5)]
        [LocalizedDisplayName("key_131")]
        [LocalizedCategory("http_header")]
        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_132"), obj => { TimeoutCount = 0;
                            ForbidCount = 0;
                            RequestCount = 0;
                            ParseErrorCount = 0;
                        }),
                    });
            }
        }
        [Browsable(false)]
        public List<ProjectItem> Projects { get; set; }

        #region Constructors and Destructors

        public override void RebuildConfig()
        {
            MaxForbidCount = 10;
            IsAddErrorCollection = true;
            AutoSaveTime = 300;
            FiddlerPort = 8888;
            AutoStartStopFiddler = true;
        }

        public override FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            FreeDocument dict = base.DictSerialize(scenario);
            dict.Children = new List<FreeDocument>();
            dict.Children.AddRange(Projects.Select(d => d.DictSerialize(scenario)));
            return dict;
        }

        public override void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            base.DictDeserialize(dicts, scenario);
            var doc = dicts as FreeDocument;
            if (doc == null || doc.Children == null) return;
            foreach (FreeDocument child in doc.Children)
            {
                var project = new ProjectItem();

                project.DictDeserialize(child);
                Projects.Add(project);
            }
        }

        #endregion

        #region Public Methods

        public static string GetXMLFileLocation(string fileName)
        {
            //  if (MainDescription.IsUIForm)
            return MainDescription.MainFrm.MainPluginLocation + "\\" + fileName + ".xml";
        }

        public static string GetFileLocation(string fileName)
        {
            // if (MainDescription.IsUIForm)
            {
                return MainDescription.MainFrm.MainPluginLocation + "\\" + fileName;
            }
        }


        public static string GetFolderLocaion(string folderName)
        {
            // if (MainDescription.IsUIForm)
            {
                return MainDescription.MainFrm.MainPluginLocation + "\\" + folderName + "\\";
            }
        }

        #endregion

        // Public Methods (3) 
    }
}