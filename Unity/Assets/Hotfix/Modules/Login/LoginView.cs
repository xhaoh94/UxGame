using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.UI
{
    [UI]
    public partial class LoginView
    {
        protected override UILayer Layer => UILayer.Tip;

        private TMP_InputField _accountInput;
        private InputField _legacyAccountInput;
        private int _campIndex;

        protected override void OnInit()
        {
            base.OnInit();
            DisableClickScale(btnLogin);
            
            _accountInput = inputAcc.GetComponent<TMP_InputField>();
            if (_accountInput == null)
                _legacyAccountInput = inputAcc.GetComponent<InputField>();

            _campIndex = 0;
        }

        private string Account
        {
            get
            {                
                if (_accountInput != null) return _accountInput.text;
                if (_legacyAccountInput != null) return _legacyAccountInput.text;
                return string.Empty;
            }
        }

        partial void OnBtnLoginClick(UIEventContext e)
        {
            LoginModule.Ins.Connect(null);
            // OnConnectAsync().Forget();
        }

        // partial void OnCamp1Click(UIEventContext e) => _campIndex = 0;
        // partial void OnCamp2Click(UIEventContext e) => _campIndex = 1;
        // partial void OnCamp3Click(UIEventContext e) => _campIndex = 2;

        async UniTaskVoid OnConnectAsync()
        {
            var mask = 1 << _campIndex;
            LoginModule.Ins.LoginAccount(Account, "x", mask).Forget();
            UIMgr.Ins.Hide<UI.LoginView>();
            await UniTask.CompletedTask;
        }
    }
}
