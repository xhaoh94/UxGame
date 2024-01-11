using FairyGUI;
using Ux;
using System;
using System.Diagnostics;

namespace Ux.UI
{
    [UI]
    public partial class LoginView
    {
        protected override UILayer Layer => UILayer.Tip;

        protected override void OnInit()
        {
            base.OnInit();
        }


        partial void OnBtnLoginClick(EventContext e)
        {
            LoginModule.Ins.Connect(OnConnect);
        }

        void OnConnect()
        {
            LoginModule.Ins.LoginAccount(inputAcc.text, "x", int.Parse(inputPass.text));
            //LoginModule.Instance.LoginAccountRPC(inputAcc.text, inputPass.text);
        }

    }
}
