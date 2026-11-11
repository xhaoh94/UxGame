using System;

namespace Ux
{
    [Module]
    public class NetModule : ModuleBase<NetModule>
    {
        protected override void OnInit()
        {
            base.OnInit();
            // 显式注册：替代原来的 [Evt(MainEventType.NET_SOCKET_CODE)] 方法特性扫描。
            // 模块 Release 时 ModuleBase 会按 tag 批量移除（OffTag），这里不需要手动反注册。
            EventMgr.Ins.On<string, SocketCode>(MainEventType.NET_SOCKET_CODE, this, OnSocketCode);
        }

        void OnSocketCode(string address, SocketCode code)
        {
            switch (code)
            {
                case SocketCode.ConnectionTimeout:
                    Action callback = () =>
                    {
                        if (!(GameMain.Machine.StateNode is StateLogin))
                        {
                            GameMain.Machine.Enter<StateLogin>();
                        }
                    };
                    // UIMgr.Dialog.SingleBtn("提示", $"链接超时", "确定", callback);
                    break;
                case SocketCode.Error:
                    Action fn1 = () =>
                    {
                        
                    };
                    Action fn2 = () =>
                    {
                        GameMain.Machine.Enter<StateLogin>();
                    };
                    // UIMgr.Dialog.DoubleBtn("提示", "网络断开连接", "重新连接", fn1, "返回登录", fn2);
                    break;
            }
        }
    }
}
