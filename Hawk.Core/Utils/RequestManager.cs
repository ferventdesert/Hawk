using System;
using Hawk.Core.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using System.Windows.Input;
using Hawk.Core.Utils.MVVM;

namespace Hawk.Core.Utils
{

    public class RequestManager:PropertyChangeNotifier
    {

        private RequestManager()
        {
            MaxForbidCount = 10;
        }

        [PropertyOrder(0)]
        [LocalizedDisplayName("key_126")]
        public int RequestCount
        {
            get { return _requestCount; }
            set
            {
                if (_requestCount != value)
                {
                    _requestCount = value;
                    OnPropertyChanged("RequestCount");
                }
            }
        }


        [PropertyOrder(1)]
        [LocalizedDisplayName("key_127")]
        public int ForbidCount
        {
            get { return _forbidCount; }
            set
            {
                if (_forbidCount != value)
                {
                    _forbidCount = value;
                    OnPropertyChanged("ForbidCount");
                }
            }
        }


        [PropertyOrder(2)]
        [LocalizedDisplayName("key_128")]
        public int MaxForbidCount
        {
            get { return _maxForbidCount; }
            set
            {
                if (_maxForbidCount != value)
                {
                    _maxForbidCount = value;
                    OnPropertyChanged("MaxForbidCount");
                }
            }
        }

        [PropertyOrder(3)]
        [LocalizedDisplayName("key_129")]
        public int ParseErrorCount
        {
            get { return _parseErrorCount; }
            set
            {
                if (_parseErrorCount != value)
                {
                    _parseErrorCount = value;
                    OnPropertyChanged("ParseErrorCount");
                }
            }
        }

        private static RequestManager instance=null;
        private int _requestCount;
        private int _forbidCount;
        private int _maxForbidCount;
        private int _parseErrorCount;
        private int _timeoutCount;

        public static RequestManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new RequestManager();
                return instance;
            }
        }

        [PropertyOrder(4)]
        [LocalizedDisplayName("key_130")]
        public int TimeoutCount
        {
            get { return _timeoutCount; }
            set
            {
                if (_timeoutCount != value)
                {
                    if(_timeoutCount != value)
                    {
                        _timeoutCount = value;
                        OnPropertyChanged("TimeoutCount");
                    }
                }
            }
        }


        [PropertyOrder(5)]
        [LocalizedDisplayName("key_131")]
        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command(GlobalHelper.Get("key_132"), obj => { TimeoutCount = 0;
                            ForbidCount = 0;
                            RequestCount = 0;
                            ParseErrorCount = 0;
                        }), 
                    });
            }
        }


    }


}
