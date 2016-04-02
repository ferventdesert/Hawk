using System;

namespace Hawk.Core.Utils.Plugins
{
    [Interface( "可实现自助替换界面的接口")]
    public interface ICustomView
    {
        FrmState FrmState { get; }
    }

    public interface IRemoteInvoke
    {
        Func<string,object,bool> RemoteFunc { get; set; }
    }

     [Interface("一项弱类型的简化接口，用于不指定强功能的插件集合")]
    public interface  ICustomClass
    {
        
    }


}