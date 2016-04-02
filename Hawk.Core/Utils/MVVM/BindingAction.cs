using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Hawk.Core.Utils.MVVM
{
    public class BindingAction : Command, IAction
    {


        private ObservableCollection<ICommand> childs;

        public ObservableCollection<ICommand> ChildActions
        {
            get
            {
                if (Func == null)
                {
                    return childs;
                }
                else
                {
                    return new ObservableCollection<ICommand>( Func());
                }
            }
            set { childs = value; }
        }

        private Func<IEnumerable<ICommand>> Func;

        public void SetChildActionSource(Func<IEnumerable<ICommand>> func)
        {
            Func = func;
        }
        
        public BindingAction(string text=null, Action<object> action=null, Predicate<object> func=null)
            : base(text,action,func)
        {
            ChildActions = new ObservableCollection<ICommand>();
        }
    }
}
