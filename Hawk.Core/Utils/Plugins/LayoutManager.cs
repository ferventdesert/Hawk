using System.Windows;
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


    [XFrmWork("布局管理器", "输出调试信息", "")]
    public class LayoutManager : AbstractPlugIn, IMainFrmMenu
    {
        #region Constants and Fields

         private readonly string layoutFolder = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) +
                                               "\\Layouts";

        private IAction commands;

        private IDockableManager manager;

        private BindingAction viewMenu;

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
                        new BindingAction("布局")
                        {
                            ChildActions =
                                    new ObservableCollection<ICommand>
                                        {
                                        //     new BindingAction("保存当前布局", obj => this.SaveCurrentLayout()) {Icon = "save"},
                                        //    new BindingAction("加载布局", obj => this.UpdateLayouts()) {Icon = "inbox_out"},

                                            new BindingAction("刷新", obj => this.RefreshLayoutView()){Icon="refresh"},
                                        this.viewMenu,
            },Icon = "layout"
                        });
            }
        }




        #endregion

        #region Public Methods

        public override bool Init()
        {
            this.manager = this.MainFrmUI as IDockableManager;

            this.viewMenu = new BindingAction("视图", obj => this.RefreshLayoutView()){Icon = "layout"};

            this.UpdateLayouts();
            RefreshLayoutView();

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
            
            MessageBox.Show("布局文件已经成功保存为" + fileName);
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