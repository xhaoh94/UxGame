// 自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ux.UI
{
    public partial class LoginView : UIView
    {
        protected GameObject btnLogin;
        protected TMP_Text title;
        protected TMP_InputField inputAcc;
        protected TMP_Text text_Area;
        protected TMP_Text text;
        protected TMP_Text title2;
        protected TMP_Text title3;
        protected TMP_Text title4;

        protected override void CreateChildren()
        {
            var root = GameObject.transform;
            btnLogin = root.GetChild(2).gameObject;
            var node_2 = root.GetChild(2);
            title = node_2.GetChild(3).GetComponent<TMP_Text>();
            inputAcc = root.GetChild(4).GetComponent<TMP_InputField>();
            var node_4 = root.GetChild(4);
            text_Area = node_4.GetChild(0).GetComponent<TMP_Text>();
            var node_4_0 = node_4.GetChild(0);
            text = node_4_0.GetChild(0).GetComponent<TMP_Text>();
            var node_6 = root.GetChild(6);
            title2 = node_6.GetChild(2).GetComponent<TMP_Text>();
            var node_7 = root.GetChild(7);
            title3 = node_7.GetChild(2).GetComponent<TMP_Text>();
            var node_8 = root.GetChild(8);
            title4 = node_8.GetChild(2).GetComponent<TMP_Text>();
        }

        protected override void OnAddEvent()
        {
            AddClick(btnLogin, _OnBtnLoginClick);
        }

        private void _OnBtnLoginClick(UIEventContext e)
        {
            OnBtnLoginClick(e);
        }
        partial void OnBtnLoginClick(UIEventContext e);
    }
}
