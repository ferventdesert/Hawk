using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;

namespace Hawk.ETL.Controls.Controls
{
    public class PopupWindowBase : Window
    {
        #region Constructors and Destructors

        public PopupWindowBase()
        {
            this.AllowsTransparency = true;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.None;
            this.Background = new SolidColorBrush(Colors.Transparent);
           

            var newBlurEffect = new BlurEffect();

            this.Loaded += (s, e) =>
                {
                    //设定模糊效果值Radius
                   // newBlurEffect.Radius = 10; //为Image添加Blur效果
                    //Application.Current.MainWindow.Effect = newBlurEffect;
                    // Application.Current.MainWindow.IsEnabled = false;
                };
            this.Closing += (s, e) =>
                {
                    newBlurEffect.Radius = 0;
                    Application.Current.MainWindow.IsEnabled = true;
                };
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }



        public object BindingSource { get; set; }

       [Category("SetProperty")]
        public bool DefaultResult
        {
            get { return (bool)GetValue(DefaultResultProperty); }
            set { SetValue(DefaultResultProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultResult.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultResultProperty =
            DependencyProperty.Register("DefaultResult", typeof(bool), typeof(PopupWindowBase), new PropertyMetadata(true));

            

        /// <summary>
        /// 允许用户在当前退出
        /// </summary>
        [Category("SetProperty")]
        public bool AllowCancel
        {
            get { return (bool)GetValue(AllowCancelProperty); }
            set { SetValue(AllowCancelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowCancel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowCancelProperty =
            DependencyProperty.Register("AllowCancel", typeof(bool), typeof(PopupWindowBase), new PropertyMetadata(true));

        [Category("SetProperty")]
        public TimeSpan DisplayTime
        {
            get { return (TimeSpan)GetValue(DisplayTimeProperty); }
            set { SetValue(DisplayTimeProperty, value); }
        }

     
        public static readonly DependencyProperty DisplayTimeProperty =
            DependencyProperty.Register("DisplayTime", typeof(TimeSpan), typeof(PopupWindowBase), new PropertyMetadata(TimeSpan.Zero));
        [Category("SetProperty")]
        public int Progress
        {
            get { return (int)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }
        public static readonly DependencyProperty ProgressProperty =
               DependencyProperty.Register("Progress", typeof(int), typeof(PopupWindowBase), new PropertyMetadata(0));
        [Category("SetProperty")]
        public int MaxProgress { get; set; }
        public  virtual void ShowDialogAdvance()
        {
          
            this.Progress = 0;
          
            var allmill = DisplayTime.TotalMilliseconds;
            MaxProgress = (int)(allmill / 500.0);
            this.DataContext = this;
            if(DisplayTime!=TimeSpan.Zero)
            {
                var task = new Task(
                    d =>
                    {
                        try
                        {
                            for (int i = 0; i < allmill/500; i++)
                            {
                                Thread.Sleep(500);
                                ControlExtended.UIInvoke(() => { this.Progress++; });
                                if (token.IsCancellationRequested == true)
                                    return;
                            }
                            if (token.IsCancellationRequested == true)
                                return;

                            ControlExtended.UIInvoke(
                                () =>
                                {
                                    try
                                    {
                                        this.DialogResult = DefaultResult;
                                    }
                                    catch (Exception ex)
                                    {
                                        return;
                                            
                                    }
                                 
                                    this.Close();
                                });

                        }
                        catch (Exception ex)
                        {
                            
                        }
                    },
                        this.token)
                        ;
               

                task.Start();
            }
            base.ShowDialog();


        }

        [Category("SetProperty")]
        public bool AllowOK
        {
            get { return (bool)GetValue(AllowOKProperty); }
            set { SetValue(AllowOKProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowOK.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowOKProperty =
            DependencyProperty.Register("AllowOK", typeof(bool), typeof(PopupWindowBase), new PropertyMetadata(true));

        
        public virtual  ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                        {
                           
                             new Command("save",  obj=>ButtonClick1(),obj=>AllowOK) { Icon = "save" },
                              new Command("quit",obj=>ButtonClick2(),obj=>AllowCancel) { Icon = "cancel" },
                            new Command("other",obj=>ButtonClick3(),obj=>true) { Icon = "cancel" },
                        });
            }
        }

        protected  virtual void ButtonClick3()
        {
           
        }

        protected readonly CancellationTokenSource token = new CancellationTokenSource();
        protected virtual void ButtonClick1()
        {
            this.token.Cancel();
            this.DialogResult = true;
            this.Close();
        }

        protected virtual void ButtonClick2()
        {
            this.token.Cancel();
            this.DialogResult = false;
            this.Close();
        }

       
        #endregion
    }
}