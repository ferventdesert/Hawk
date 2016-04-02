using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Hawk.Core.Utils.Plugins;
using Xceed.Wpf.DataGrid;

namespace Hawk.ETL.Controls.Adorners
{
    public class DataGridSetter : DependencyObject
    {
        #region Constants and Fields

        public static readonly DependencyProperty DataProperty = DependencyProperty.RegisterAttached(
            "Data", typeof(DataGridSetter), typeof(DataGridSetter), new FrameworkPropertyMetadata(null, OnDataChanged));

        //绑定路径

        private INotifyPropertyChanged inpc;

        #endregion

        #region Properties

        public string Path { get; set; }

        #endregion

        #region Public Methods

        public static DataGridSetter GetData(DependencyObject d)
        {
            return (DataGridSetter)d.GetValue(DataProperty);
        }

        public static void SetData(DependencyObject d, DataGridSetter value)
        {
            d.SetValue(DataProperty, value);
        }

        #endregion

        #region Methods

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataGridSetter = (DataGridSetter)e.NewValue;
            var newData = (DataGridControl)d;

            newData.DataContextChanged += dataGridSetter.ff_DataContextChanged;
        }
        PropertyInfo propInfo;
        private DataGridControl control;
        private void ff_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            control = sender as DataGridControl;
            this.inpc = e.NewValue as INotifyPropertyChanged;
            if (this.inpc != null)
            {
                this.propInfo = e.NewValue.GetType().GetProperty(this.Path);
                Debug.WriteLine("New DataContext: {0}", e.NewValue);
                this.inpc.PropertyChanged += this.inpc_PropertyChanged;
            }
        }

        private void inpc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == this.Path)
            {
              var value=  propInfo.GetValue(this.inpc, new object[0]) as ListCollectionView;
                if (value != null)
                {
                    var data = value.SourceCollection;

                    if (data != null)
                    {
                    
                        var su = data.GetEnumerator();
                        this.control.Columns.Clear();
                        var res=  su.MoveNext();
                        if (res == true)
                        {
                            IDictionarySerializable dict = su.Current as IDictionarySerializable;
                      
                            if (dict != null)
                            {
                                foreach (var item in dict.DictSerialize(Scenario.Binding))
                                {
                                    this.control.Columns.Add(new Column { Title = item.Key, FieldName = item.Key });
                                }
                            }
                        }
                  
                    }
                }
            }
        }

        #endregion
    }
}