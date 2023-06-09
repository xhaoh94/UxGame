using System;

namespace Ux
{
    [Module]
    public class NetModule : ModuleBase<NetModule>
    {
        [Evt(MainEventType.NET_SOCKET_CODE)]
        void OnSocketCode(string address, SocketCode code)
        {
            switch (code)
            {
                case SocketCode.ConnectionTimeout:
                    Action callback = () =>
                    {
                        if (!(GameMain.Machine.CurrentNode is StateLogin))
                        {
                            GameMain.Machine.Enter<StateLogin>();
                        }
                    };
                    UIMgr.Dialog.SingleBtn("提示", $"链接超时", "确定", callback);
                    break;
                case SocketCode.Error:
                    Action fn1 = () =>
                    {
                        
                    };
                    Action fn2 = () =>
                    {
                        GameMain.Machine.Enter<StateLogin>();
                    };
                    UIMgr.Dialog.DoubleBtn("提示", "网络断开连接", "重新连接", fn1, "返回登录", fn2);
                    break;
            }
        }
    }
}
