using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;

namespace Hawk.ETL.Interfaces
{
   [XFrmWorkIgnore]
    public class AbstractProcessMethod : PropertyChangeNotifier, IDataProcess, IDictionarySerializable
    {
        #region Constants and Fields

        protected string _name;



        #endregion

        #region Constructors and Destructors

        public AbstractProcessMethod()
        {
            Name = TypeName;
        }

        #endregion


        #region Events

        #endregion

        #region Properties

   
        [Browsable(false)]
        public IMainFrm MainFrm { get; set; }

        [Browsable(false)]
        public string MainPluginLocation { get; set; }

        [Browsable(false)]
        public PropertyGrid PropertyGrid
        {
            get
            {
                var property = new PropertyGrid();
                property.SelectedObject=this;
                return property;
            }
        } 


        /// <summary>
        ///     模块名称
        /// </summary>
        [LocalizedCategory("1.基本信息")]
        [LocalizedDisplayName("模块名称")]
        public virtual string Name
        {
            get { return _name; }
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged("Name");
                if (MainDescription.IsUIForm)
                {
                    var dock = MainFrm as IDockableManager;
                    var view = dock?.ViewDictionary.FirstOrDefault(d => d.Model == this);
                    if(view==null)
                        return;
                    dynamic container = view.Container;
                    container.Title =  _name;
                }
            }
        }


        [Browsable(false)]
        public bool IsOpen { get; set; }


        [Browsable(false)]
        public IDataManager SysDataManager { get; set; }

        [Browsable(false)]
        public IProcessManager SysProcessManager { get; set; }
      


        [Browsable(false)]
        public string TypeName => AttributeHelper.GetCustomAttribute(GetType()).Name;

        [Browsable(false)]
       public string LogoURL => AttributeHelper.GetCustomAttribute(GetType()).LogoURL;



        #endregion

        #region Implemented Interfaces

        #region IBackgroundMethod

        public virtual void ReportFinalResult()
        {
        }

        #endregion

        #region IDictionarySerializable

        public virtual void DictDeserialize(IDictionary<string, object> dicts, Scenario scenario = Scenario.Database)
        {
            Name = dicts.Set("Name", Name);
        }

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = new FreeDocument
            {
              
                {"Name", Name},
                {"Type", this.GetType().Name},
            };

            return dict;
        }

        #endregion

        #region IProcess

        public virtual bool Close()
        {
            return true;
        }

        public virtual bool Process()
        {
            return true;
        }


        public virtual bool Init()
        {
            return true;
        }

        #endregion

        #endregion

        #region Methods




        #endregion
    }
}