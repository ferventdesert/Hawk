using System;
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
        [DisplayName("总请求数")]
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
        [DisplayName("禁止数")]
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
        [DisplayName("最大禁止数")]
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
        [DisplayName("解析错误数")]
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
        [DisplayName("超时数")]
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
        [DisplayName("操作")]
        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("清空数据", obj => { TimeoutCount = 0;
                            ForbidCount = 0;
                            RequestCount = 0;
                            ParseErrorCount = 0;
                        }), 
                    });
            }
        }


    }


}
