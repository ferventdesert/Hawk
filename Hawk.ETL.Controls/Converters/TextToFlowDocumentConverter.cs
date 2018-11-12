using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Hawk.Core.Connectors;

namespace Hawk.ETL.Controls.Converters
{
    public class TextToFlowDocumentConverter : DependencyObject, IValueConverter
    {
        public Markdown.Xaml.Markdown Markdown
        {
            get { return (Markdown.Xaml.Markdown)GetValue(MarkdownProperty); }
            set { SetValue(MarkdownProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Markdown.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register("Markdown", typeof(Markdown.Xaml.Markdown), typeof(Markdown.Xaml.TextToFlowDocumentConverter), new PropertyMetadata(null));

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            var text = (string)value;
            try
            {
                if (ConfigFile.GetConfig().Get<bool>("IsDisplayDetail") == true)
                {
                    text = text.Split('\n')[0];
                }
                var engine = Markdown ?? mMarkdown.Value;
                
                text = string.Join("\n", text.Trim().Split('\n').Select(d => d.Trim()));
                return engine.Transform(text);
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private Lazy<Markdown.Xaml.Markdown> mMarkdown
            = new Lazy<Markdown.Xaml.Markdown>(() => new Markdown.Xaml.Markdown());
    }
}
