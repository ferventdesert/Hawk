using System;
using System.Globalization;
using System.Windows;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

namespace XFrmWork.UI.Controls
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Input;

    using Microsoft.Win32;


    [XFrmWork("LayoutManager", "LayoutManager_desc", "")]
    public class LayoutManager : AbstractPlugIn, IMainFrmMenu
    {
        #region Constants and Fields

         private readonly string layoutFolder = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) +
                                               "\\Layouts";

        private IAction commands;

        private IDockableManager manager;

        private BindingAction viewMenu;
        private BindingAction languageMenu;

        #endregion

        #region Constructors and Destructors

        public LayoutManager()
        {


        }

        #endregion

        #region Properties

        public IAction BindingCommands
        {
            get
            {
                return this.commands ??
                       (this.commands =
                        new BindingAction(GlobalHelper.Get("key_141"))
                        {
                            ChildActions =
                                    new ObservableCollection<ICommand>
                                        {
                                        //     new BindingAction("保存当前布局", obj => this.SaveCurrentLayout()) {Icon = "save"},
                                        //    new BindingAction("加载布局", obj => this.UpdateLayouts()) {Icon = "inbox_out"},

                                            new BindingAction(GlobalHelper.Get("key_142"), obj => this.RefreshLayoutView()){Icon="refresh"},
                                        this.viewMenu,
                                        this.languageMenu,
            },Icon = "layout"
                        });
            }
        }




        #endregion

        #region Public Methods

        public override bool Init()
        {
            this.manager = this.MainFrmUI as IDockableManager;

            this.viewMenu = new BindingAction(GlobalHelper.Get("key_143"), obj => this.RefreshLayoutView()){Icon = "layout"};
            this.languageMenu = new BindingAction(GlobalHelper.Get("key_lang"), obj => this.LoadLanguage()){Icon = "layout"};

            this.UpdateLayouts();
            RefreshLayoutView();
            LoadLanguage();
            return true;
        }

        public void RefreshLayoutView()
        {
            this.viewMenu.ChildActions.Clear();

            foreach (var dict in manager.ViewDictionary)
            {
                var ba = new BindingAction(dict.Name, obj => { this.manager.ActiveThisContent(dict.Name); }) {Icon = "layout"};

                this.viewMenu.ChildActions.Add(ba);
            }
        }

        private void LoadLanguage()
        {

            ResourceDictionary langRd = null;
            var files = Directory.GetFiles("Lang");
            foreach (var file in files)
            {
                var ba = new BindingAction(file, obj => { UpdateLanguage(file, files); }) { Icon = "layout" };

                this.languageMenu.ChildActions.Add(ba);
            }




        }
        private void UpdateLanguage(string name,string[] files)
        {
            CultureInfo currentCultureInfo = CultureInfo.CurrentCulture;

            ResourceDictionary langRd = null;



            try
            {
                langRd =
                    Application.LoadComponent(
                             new Uri(name, UriKind.Relative))
                    as ResourceDictionary;
            }
            catch
            {
            }

            if (langRd != null)
            {
                Application.Current.Resources.MergedDictionaries.RemoveElementsNoReturn(d => d.Keys.Count>800);
                Application.Current.Resources.MergedDictionaries.Add(langRd);
            }


        }
        public void RefreshLangView()
        {
            this.languageMenu.ChildActions.Clear();

            foreach (var dict in manager.ViewDictionary)
            {
                var ba = new BindingAction(dict.Name, obj => { this.manager.ActiveThisContent(dict.Name); }) { Icon = "layout" };

                this.viewMenu.ChildActions.Add(ba);
            }
        }

        #endregion

        #region Methods

        private string GetFileName(string name)
        {
            return this.layoutFolder + "\\" + name + ".config";
        }

        private void SaveCurrentLayout()
        {
            var ofd = new SaveFileDialog { DefaultExt = ".config", Filter = "布局文件(*.config)|*.config" };
            ofd.InitialDirectory = this.layoutFolder;
            string fileName = null;
            if (ofd.ShowDialog() == true)
            {
                fileName = ofd.FileName;
            }
            if (fileName == null)
            {
                return;
            }
            
            MessageBox.Show(GlobalHelper.Get("key_144") + fileName);
        }

        private void UpdateLayouts()
        {
            return;
            var isexist = Directory.Exists(layoutFolder);
            if (isexist == false)
                return;
            IEnumerable<string> files =
                Directory.GetFiles(this.layoutFolder).Where(d => Path.GetExtension(d) == ".config").Select(
                    Path.GetFileNameWithoutExtension);
            var action = this.BindingCommands.ChildActions[1] as IAction;
            if (action != null)
            {
                ObservableCollection<ICommand> childActions = action.ChildActions;
                childActions.Clear();

                foreach (string file in files)
                {
                    childActions.Add(
                        new BindingAction(
                            file,
                            obj =>
                            {
                                this.CurrentName = file;
                            },
                            obj => file != null));
                }
            }
        }

        public string CurrentName { get; set; }


        #endregion
    }
}