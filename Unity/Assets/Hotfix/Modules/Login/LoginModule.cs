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
using Cysharp.Threading.Tasks;

namespace Ux
{
    [Module]
    public class LoginModule : ModuleBase<LoginModule>
    {
        public Pb.S2CEnterScene resp;
        public void Connect(Action OnConnect)
        {
            //   TCP KCP
            //NetMgr.Ins.Connect(NetType.TCP, "127.0.0.1:10002", OnConnect);
            //WebSocket
            //NetMgr.Ins.Connect(NetType.WebSocket,"ws://127.0.0.1:10002/");

            resp = new Pb.S2CEnterScene()
            {
                Self = new Pb.Entity()
                {
                    Position = new Pb.Vector3(),
                    roleId = 1,
                    roleMask = 1,
                }
            };
            GameMain.Machine.Enter<StateGameIn>();
            TimeMgr.Ins.DoTimer(10, 1, this, Test1);
            TimeMgr.Ins.DoTimer(9, 1, this, Test2);
            TimeMgr.Ins.DoTimer(80, 1, this, Test3);
        }
        void Test1()
        {
            Log.Info("1");
        }
        void Test2()
        {
            Log.Info("2");
        }
        void Test3()
        {
            Log.Info("3");
        }
        struct LoginReslut
        {
            public int mask;
            public string account;
            public string token;
        }
        public async UniTaskVoid LoginAccount(string account, string password, int mask)
        {
            var c2slogin = new Pb.C2SLoginGame()
            {
                Account = account,
                Password = password
            };
            //请求登录获取Gate服务器登录Token
            var s2clogin = await NetMgr.Ins.Call<Pb.C2SLoginGame, Pb.S2CLoginGame>(CS.C2S_LoginGame, c2slogin);
            if (s2clogin == null)
            {
                Log.Error("请求登录失败");
                return;
            }
            if (s2clogin.Error == Pb.ErrCode.UnKnown)
            {
                Log.Error("请求登录失败");
                return;
            }
            //断开与Login服务器的Socket
            NetMgr.Ins.Disconnect();
            //链接Gate服务器
            NetMgr.Ins.Connect(NetType.TCP, s2clogin.Addr, () =>
            {
                _EnterMap(new LoginReslut
                {
                    mask = mask,
                    account = account,
                    token = s2clogin.Token,
                }).Forget();
            });
        }

        async UniTaskVoid _EnterMap(LoginReslut login)
        {
            var data = new Pb.C2SEnterScene();
            data.Account = login.account;
            data.roleMask = login.mask;
            data.Sceneid = 1;
            data.Token = login.token;            
            resp = await NetMgr.Ins.Call<Pb.C2SEnterScene, Pb.S2CEnterScene>(CS.C2S_EnterScene, data);
            if (resp.Error == Pb.ErrCode.UnKnown)
            {
                Log.Error("请求进入场景失败");
                return;
            }
            GameMain.Machine.Enter<StateGameIn>();
        }
    }
}
