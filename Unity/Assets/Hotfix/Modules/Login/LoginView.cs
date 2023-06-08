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
        }

        partial void OnBtnLoginClick(EventContext e)
        {
            LoginModule.Instance.Connect(OnConnect);
        }

        void OnConnect()
        {
            LoginModule.Instance.LoginAccount(inputAcc.text, inputPass.text);
            //LoginModule.Instance.LoginAccountRPC(inputAcc.text, inputPass.text);
        }
    }
}
