using System;

namespace Hawk.Core.Utils.Plugins
{
    [Interface( "ICustomView")]
    public interface ICustomView
    {
        FrmState FrmState { get; }
    }

    public interface IRemoteInvoke
    {
        Func<string,object,bool> RemoteFunc { get; set; }
    }

     [Interface("ICustomClass")]
    public interface  ICustomClass
    {
        
    }


}