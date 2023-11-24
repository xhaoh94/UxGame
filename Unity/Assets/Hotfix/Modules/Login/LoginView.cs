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

        protected override void OnShow(object param)
        {
            base.OnShow(param);
            TimeMgr.Ins.DoTimer(5, 1, Test);                        
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
