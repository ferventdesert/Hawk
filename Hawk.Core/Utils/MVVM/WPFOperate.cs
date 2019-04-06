using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hawk.Core.Connectors;
using Microsoft.Win32;

namespace Hawk.Core.Utils.MVVM
{
    /// <summary>
    /// 对WPF界面元素的基本操作的封装类
    /// </summary>
    public class WPFOperate
    {
        #region Public Methods

        /// <summary>
        /// 找到一个元素的父类
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T FindVisualParent<T>(DependencyObject obj, string name = null,bool shouldbeFather=false) where T : class
        {
            var first = obj;
            while (obj != null)
            {
                if (obj is T)
                {
                    if(shouldbeFather==true&&obj==first)
                    {
                        
                    }
                    else
                    {
                        var ele = obj as FrameworkElement;
                        if (ele != null)
                        {
                            if (name == null)
                            {
                                return obj as T;
                            }
                            if (ele.Name == name)
                            {
                                return ele as T;
                            }
                        }
                    }
                    
                }

                obj = VisualTreeHelper.GetParent(obj);
            }

            return null;
        }
     
        /// <summary>
        /// 搜索Visual Tree并尝试返回制定类型的DependencyObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="itemName"> </param>
        /// <returns></returns>
        public static T GetChild<T>(DependencyObject obj, string itemName = null) where T : DependencyObject
        {
            DependencyObject child = null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);

                if (IsSame(child, typeof(T), itemName))
                {
                    break;
                }
                else
                {
                    child = GetChild<T>(child, itemName);
                }
            }

            return child as T;
        }

        public static bool ImageSave(FrameworkElement inkCanvas, string fileName = null)
        {
            if (fileName == null)
            {
                var ofd = new SaveFileDialog { DefaultExt = ".jpg", Filter = "jpg file|*.jpg" };

                if (ofd.ShowDialog() == true)
                {
                    fileName = ofd.FileName;
                }
                if (fileName == null)
                {
                    return false;
                }
            }

            try
            {
                int ratio = 1;

                var width = (int)Math.Round(inkCanvas.ActualWidth * ratio);
                var height = (int)Math.Round(inkCanvas.ActualHeight * ratio);


                if (inkCanvas != null)
                {
                    if (!Double.IsNaN(inkCanvas.Width))
                        width = (int)inkCanvas.Width;
                    if (!Double.IsNaN(inkCanvas.Height))
                        height = (int)inkCanvas.Height;
                }

                Size size = new Size(width, height);
                inkCanvas.Measure(size);
                inkCanvas.Arrange(new Rect(size));


                var bmpCopied = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Default);
                var dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    var vb = new VisualBrush(inkCanvas) { Stretch = Stretch.Fill };
                    dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(width, height)));
                }
                bmpCopied.Render(dv);
                using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    var encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bmpCopied));
                    encoder.Save(file);
                    return true;
                }
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.ToString());
                return false;
            }
        }

        public static T GetParentObject<T>(DependencyObject obj, string name) where T : FrameworkElement
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);

            while (parent != null)
            {
                if (parent is T && (((T)parent).Name == name | string.IsNullOrEmpty(name)))
                {
                    return (T)parent;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        #endregion

        #region Methods

        private static bool IsSame(object child, Type type, string name = null)
        {
            if (child == null)
            {
                return false;
            }
            if (!child.GetType().IsSubclassOf(type))
            {
                return false;
            }
            if (name == null)
            {
                return true;
            }
            var rc = child as FrameworkElement;
            if (rc == null)
            {
                return false;
            }
            if (rc.Name != name)
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}