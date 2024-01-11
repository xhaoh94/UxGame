using Ux;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Pb;

namespace Ux
{
    [Module]
    public class LoginModule : ModuleBase<LoginModule>
    {
        ClientSocket _client;
        public void Connect(Action OnConnect)
        {
            //   TCP KCP
            _client = NetMgr.Ins.Connect(NetType.TCP, "127.0.0.1:10002", OnConnect);
            //WebSocket
            //NetMgr.Ins.Connect(NetType.WebSocket,"ws://127.0.0.1:10002/");

            //GameMain.Machine.Enter<StateGameIn>();
        }
        string _account;
        int _mask;
        public void LoginAccount(string account, string password, int mask)
        {
            _mask = mask;
            var data = new Pb.C2SLoginGame();
            _account = data.Account = account;
            data.Password = password;
            NetMgr.Ins.Send(CS.C2S_LoginGame, data);
        }

        [Net(SC.S2C_LoginGame)]
        void LoginResult(Pb.S2CLoginGame data)
        {
            _client.Disconnect();
            _client = NetMgr.Ins.Connect(NetType.TCP, data.Addr, () =>
            {
                _EnterMap(data.Token);
            });
            NetMgr.Ins.SetDefaultClient(_client);
        }
        async void _EnterMap(string token)
        {
            var data = new Pb.C2SEnterMap();
            data.Account = _account;
            data.roleMask = _mask;
            data.Mapid = 1;
            data.Token = token;
            var resp = await NetMgr.Ins.Call<Pb.S2CEnterMap>(CS.C2S_EnterMap, data);
            GameMain.Machine.Enter<StateGameIn>(resp);
        }

    }
}
