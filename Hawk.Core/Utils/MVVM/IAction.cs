using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Hawk.Core.Utils.MVVM
{
    public interface IAction:ICommand
    {

        string Text { get; set; }

        ObservableCollection<ICommand> ChildActions { get; set; }

        string Icon { get; set; }
    }


   


}
