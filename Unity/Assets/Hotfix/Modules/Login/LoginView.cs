using FairyGUI;
using Ux;
using System;
using System.Diagnostics;

namespace Ux.UI
{
    [UI]
    public partial class LoginView
    {
        protected override UILayer Layer => UILayer.Normal;

        protected override void OnInit()
        {
            base.OnInit();
            TimeMgr.Ins.DoTimer(50, 1, OnConnect);
            DoTimer(1, 1, ttt);
        }

        void ttt()
        {
            TimeMgr.Ins.DoTimer(5, 1, Test);
            TimeMgr.Ins.RemoveAll(this);
            TimeMgr.Ins.DoTimer(10, 1, Test);
            TimeMgr.Ins.RemoveTimer(Test);
        }

        partial void OnBtnLoginClick(EventContext e)
        {
            LoginModule.Ins.Connect(OnConnect);
        }

        void OnConnect()
        {
            LoginModule.Ins.LoginAccount(inputAcc.text, inputPass.text);
            //LoginModule.Instance.LoginAccountRPC(inputAcc.text, inputPass.text);
        }

        void Test()
        {

        }
    }
}
