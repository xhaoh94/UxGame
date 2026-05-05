using FairyGUI;
using Ux;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEditor.UI;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

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
            // LoginModule.Ins.Connect(OnConnect);
            OnConnectAsync().Forget();
        }

        async UniTaskVoid OnConnectAsync()
        {
            // int mask =(int) Mathf.Pow(2, camp.selectedIndex);
            // LoginModule.Ins.LoginAccount(inputAcc.text, "x", mask).Forget();
            //LoginModule.Instance.LoginAccountRPC(inputAcc.text, inputPass.text);

            await UIMgr.Ins.Create().Show<UI.MainView>().Task();
            UIMgr.Ins.Hide<UI.LoginView>();
        }

    }
}
