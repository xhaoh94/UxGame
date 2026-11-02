//自动生成的代码，请勿修改!!!
using UnityEngine;
namespace Ux.UI
{
    public partial class Stack4View : UIView
    {
        protected GameObject btnBack;
        protected GameObject btn1;
        protected GameObject btn2;
        protected GameObject btn3;
        protected override void CreateChildren()
        {
            var root = GameObject.transform;
            btnBack = root.GetChild(2).gameObject;
            btn1 = root.GetChild(3).gameObject;
            btn2 = root.GetChild(4).gameObject;
            btn3 = root.GetChild(5).gameObject;
        }
        protected override void OnAddEvent()
        {
            AddClick(btnBack, _OnBtnBackClick);
            AddClick(btn1, _OnBtn1Click);
            AddClick(btn2, _OnBtn2Click);
            AddClick(btn3, _OnBtn3Click);
        }
        private void _OnBtnBackClick(UIEventContext e) => OnBtnBackClick(e);
        partial void OnBtnBackClick(UIEventContext e);
        private void _OnBtn1Click(UIEventContext e) => OnBtn1Click(e);
        partial void OnBtn1Click(UIEventContext e);
        private void _OnBtn2Click(UIEventContext e) => OnBtn2Click(e);
        partial void OnBtn2Click(UIEventContext e);
        private void _OnBtn3Click(UIEventContext e) => OnBtn3Click(e);
        partial void OnBtn3Click(UIEventContext e);
    }
}
