using Ux;
using System;

namespace Ux
{
    [Module]
    public class NetModule : ModuleBase<NetModule>
    {
        [Ux.Main.Evt(Ux.Main.EventType.NET_SOCKET_CODE)]
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
                    UIMgr.Dialog.SingleBtn("��ʾ", $"���ӳ�ʱ", "ȷ��", callback);
                    break;
                case SocketCode.Error:
                    Action fn1 = () =>
                    {
                        //TODO ��������
                    };
                    Action fn2 = () =>
                    {
                        GameMain.Machine.Enter<StateLogin>();
                    };
                    UIMgr.Dialog.DoubleBtn("��ʾ", "�������ӶϿ�", "��������", fn1, "ȡ��", fn2);
                    break;
            }
        }
    }
}
