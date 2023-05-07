using Ux;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    [Module]
    public class LoginModule : ModuleBase<LoginModule>
    {       
        public void Connect(Action OnConnect)
        {
            //TCP KCP
            //NetMgr.Instance.Connect(NetType.KCP, "127.0.0.1:10002", OnConnect);
            //WebSocket
            //NetMgr.Instance.Connect(NetType.WebSocket,"ws://127.0.0.1:10002/");

            GameMain.Machine.Enter<StateGameIn>();            
        }
        public void LoginAccount(string account, string password)
        {            
            var data = new pb.C2SLoginGame();
            data.Account = account;
            data.Password = password;
            Send(1000, data);
        }
       
        [Net(1000)]
        void LoginResult(pb.S2CLoginGame data)
        {
            Log.Debug("返回" + data.Error.ToString());
            GameMain.Machine.Enter<StateGameIn>();
        }

        public async void LoginAccountRPC(string account, string password)
        {
            var data = new pb.C2SLoginGame();
            data.Account = account;
            data.Password = password;
            var response = await Call<pb.S2CLoginGame>(2000, data);
            Log.Debug("RPC返回" + response.Error.ToString());
            GameMain.Machine.Enter<StateGameIn>();
        }


    }
}
