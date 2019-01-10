using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Hawk.ETL.Controls.Themes;

namespace Hawk.ETL.Controls.DataViewers
{
    public class FlowDocumentViewer
    {
        public Window Generate(string doc)
        {
            var textconv = new TextToFlowDocumentConverterPlus();
            var md = textconv.Convert(doc, null, null, null);
            var flow = new FlowDocumentScrollViewer();
            flow.Document = md as FlowDocument;
            var window = new Window {Content = flow};
            return window;
        }
    }
}