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

namespace Ux
{
    [Module]
    public class LoginModule : ModuleBase<LoginModule>
    {
        public void Connect(Action OnConnect)
        {
            //TCP KCP
            NetMgr.Ins.Connect(NetType.KCP, "127.0.0.1:10002", OnConnect);
            //WebSocket
            //NetMgr.Ins.Connect(NetType.WebSocket,"ws://127.0.0.1:10002/");

            //GameMain.Machine.Enter<StateGameIn>();            

            //var data = new pb.S2CLoginGame();
            //data.Error = pb.ErrCode.RoleAlready;
            //var tem = new MemoryStream();
            //ProtoBuf.Serializer.Serialize(tem, data);
            //tem.Seek(0, SeekOrigin.Begin);
            //var obj = ProtoBuf.Serializer.Deserialize(typeof(pb.S2CLoginGame), tem);
            //Log.Debug(obj);
        }
        public void LoginAccount(string account, string password)
        {
            var data = new Pb.C2SLoginGame();

            for (int i = 0; i < 10; i++)
            {
                data.Account = account + i;
                data.Password = password + i;
                Send(1000, data);
            }
        }

        [Net(1000)]
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
            var response = await Call<Pb.S2CLoginGame>(2000, data);
            Log.Debug("RPC返回" + response.Error.ToString());
            GameMain.Machine.Enter<StateGameIn>();
        }


    }
}
