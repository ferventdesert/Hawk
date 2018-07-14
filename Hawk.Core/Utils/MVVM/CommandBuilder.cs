using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Hawk.Core.Utils.Logs;

namespace Hawk.Core.Utils.MVVM
{
    public class Command : PropertyChangeNotifier, ICommand
    {
        private string text;
        private string _icon;
        private string description;

        public Command(string text = null, Action<object> execute = null, Predicate<object> canExecute = null,
            string icon = null)

        {
            Text = text;
            if (canExecute != null)
            {
                CanExecute = canExecute;
            }
            else
            {
                CanExecute = d => true;
            }
            if (execute != null)
            {
                Execute = execute;
            }
            Icon = icon;
        }

        public string Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                OnPropertyChanged("Icon");
            }
        }


        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    OnPropertyChanged("Text");
                }
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                if (description != value)
                {
                    description = value;
                    OnPropertyChanged("Description");
                }
            }
        }


        public Predicate<object> CanExecute { get; set; }

        public Action<object> Execute { get; set; }


        public Key? Key { get; set; }
        public ModifierKeys Modifiers { get; set; }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        void ICommand.Execute(object parameter)
        {
            ControlExtended.SafeInvoke(() => Execute?.Invoke(parameter),LogType.Info,"点击按钮: "+this.Text);

        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute(parameter);
        }

        public override string ToString()
        {
            return Text;
        }
    }

    /// <summary>
    ///     命令缓存器
    /// </summary>
    public class CommandBuilder
    {
        private static readonly Dictionary<object, ReadOnlyCollection<ICommand>> BufferDictionary =
            new Dictionary<object, ReadOnlyCollection<ICommand>>();

        public static ReadOnlyCollection<ICommand> GetCommands(object type, Command[] newCommands)
        {
            var commands2 = new ReadOnlyCollection<ICommand>(newCommands);
            return commands2;
            ReadOnlyCollection<ICommand> commands; //这里会造成恐怖的问题，一个Object只能缓存一个命令集合？显然可以有多个啊!
            if (BufferDictionary.TryGetValue(type, out commands))
            {
                return commands;
            }
            else
            {
                commands = new ReadOnlyCollection<ICommand>(newCommands);
                BufferDictionary.Add(type, commands);
                return commands;
            }
        }
    }
}