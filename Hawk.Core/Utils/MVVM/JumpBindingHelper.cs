using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hawk.Core.Utils.MVVM
{
    public class JumpBindingData
    {
        #region Properties

        public string Property
        {
            get;
            set;
        }

        public string RealSource
        {
            get;
            set;
        }

        #endregion Properties
    }

    public class JumpBindingClickHelper
    {

        public static JumpBindingData GetJumpBinding(DependencyObject obj)
        {
            return (JumpBindingData)obj.GetValue(JumpBindingProperty);
        }

        public static void SetJumpBinding(DependencyObject obj, JumpBindingData value)
        {
            obj.SetValue(JumpBindingProperty, value);
        }

         

        public static readonly DependencyProperty JumpBindingProperty =
           DependencyProperty.RegisterAttached("JumpBinding", typeof(JumpBindingData), typeof(JumpBindingClickHelper), new FrameworkPropertyMetadata(null,
                   new PropertyChangedCallback(OnPropertyChanged)));
       
        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listBoxItem = (MenuItem)d;
            if (listBoxItem == null)
                return;


            var str = GetJumpBinding(d);
            var listbox = WPFOperate.FindVisualParent<FrameworkElement>(listBoxItem,str.RealSource);
            
            if (listbox == null)
                return;
            var dataContext = listbox.DataContext;
               
                listBoxItem.Click += (s2, e2) =>
                {
                  
                        var command = (ICommand)BindingEvaluator.GetValue(dataContext, str.Property);
                        if (command.CanExecute(listBoxItem.DataContext))
                            command.Execute(listBoxItem.DataContext);
                    
                };


        }

    }


    public class JumpBindingHelper
    {
        #region Constants and Fields



        public static object GetMethodPool(DependencyObject obj)
        {
            return (object)obj.GetValue(MethodPoolProperty);
        }

        public static void SetMethodPool(DependencyObject obj, object value)
        {
            obj.SetValue(MethodPoolProperty, value);
        }

        // Using a DependencyProperty as the backing store for MethodPool.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MethodPoolProperty =
            DependencyProperty.RegisterAttached("MethodPool", typeof(object), typeof(JumpBindingHelper), new PropertyMetadata(null, OnPropertyChanged));





        //public static object GetTarget(DependencyObject obj)
        //{
        //    return (object)obj.GetValue(TargetProperty);
        //}

        //public static void SetTarget(DependencyObject obj, object value)
        //{
        //    obj.SetValue(TargetProperty, value);
        //}

        //// Using a DependencyProperty as the backing store for Target.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty TargetProperty =
        //    DependencyProperty.RegisterAttached("Target", typeof(object), typeof(JumpBindingHelper), new PropertyMetadata(null));

        


        public static object GetDataContextSource2(DependencyObject obj)
        {
            return (object)obj.GetValue(DataContextSource2Property);
        }

        public static void SetDataContextSource2(DependencyObject obj, object value)
        {
            obj.SetValue(DataContextSource2Property, value);
        }

        // Using a DependencyProperty as the backing store for DataContextSource2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataContextSource2Property =
            DependencyProperty.RegisterAttached("DataContextSource2", typeof(object), typeof(JumpBindingHelper), new PropertyMetadata(null));






        public static JumpSupportData GetJumpSupportData(DependencyObject obj)
        {
            return (JumpSupportData)obj.GetValue(JumpSupportDataProperty);
        }

        public static void SetJumpSupportData(DependencyObject obj, JumpSupportData value)
        {
            obj.SetValue(JumpSupportDataProperty, value);
        }

        // Using a DependencyProperty as the backing store for JumpSupportData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty JumpSupportDataProperty =
            DependencyProperty.RegisterAttached("JumpSupportData", typeof(JumpSupportData), typeof(JumpBindingHelper), new PropertyMetadata(null));

        

        #endregion

        // Using a DependencyProperty as the backing store for DataContextSource.  This enables animation, styling, binding, etc...

        #region Methods

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = (FrameworkElement)d;

            frameworkElement.DataContextChanged += ButtonDataContextChanged;            
        }

        static void ButtonDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if(element==null)
                return;
            
            var jumpSupportData = GetJumpSupportData(element);
            var methodPool = GetMethodPool(element);

             var target= WPFOperate.GetChild<FrameworkElement>(element, jumpSupportData.TargetName);
            if (target == null) return;
            Type type = methodPool.GetType();

            MethodInfo info = type.GetMethod(jumpSupportData.MethodName);
            object result = null;
            if(jumpSupportData.ArgCount>1)
            {
                var context2 = GetDataContextSource2(element);
                result = info.Invoke(methodPool, new[] { element.DataContext, context2 });
            }
             
            else
            {
                result = info.Invoke(methodPool, new[] { element.DataContext });
            }

            PropertyInfo propertyInfo = target.GetType().GetProperty(jumpSupportData.TargetProperty);
            propertyInfo.SetValue(target, result, new object[] { });
         
          
           
        }

        
        

     
        #endregion
    }

    public  class  JumpSupportData
    {
        public JumpSupportData()
        {
            TargetProperty = "DataContext";
            this.ArgCount = 1;
        }

        public string MethodName { get; set; }

        public int ArgCount { get; set; }

        public string TargetProperty { get; set; }

        public string TargetName { get; set; }
    }

    
 
}