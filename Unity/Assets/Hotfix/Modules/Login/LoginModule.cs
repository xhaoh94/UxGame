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
        public void Connect(Action OnConnect)
        {
            //TCP KCP
            //NetMgr.Ins.Connect(NetType.KCP, "127.0.0.1:10002", OnConnect);
            //WebSocket
            //NetMgr.Ins.Connect(NetType.WebSocket,"ws://127.0.0.1:10002/");

            GameMain.Machine.Enter<StateGameIn>();
        }
        public void LoginAccount(string account, string password)
        {
            var data = new Pb.C2SLoginGame();

            for (int i = 0; i < 10; i++)
            {
                data.Account = account + i;
                data.Password = password + i;
                Send(CS.C2S_LoginGame, data);
            }
        }

        [Net(SC.S2C_LoginGame)]
        void LoginResult(Pb.S2CLoginGame data)
        {
            Log.Debug("返回" + data.Error.ToString());
            GameMain.Machine.Enter<StateGameIn>();
        }

        public async void LoginAccountRPC(string account, string password)
        {
            var data = new Pb.C2SLoginGame();
            data.Account = account;
            data.Password = password;
            var response = await Call<Pb.S2CLoginGame>(CS.C2S_LoginGame, data);
            Log.Debug("RPC返回" + response.Error.ToString());
            GameMain.Machine.Enter<StateGameIn>();
        }


    }
}
