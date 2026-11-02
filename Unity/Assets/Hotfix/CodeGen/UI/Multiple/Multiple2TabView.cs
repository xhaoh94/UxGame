//自动生成的代码，请勿修改!!!
using UnityEngine;

namespace Ux.UI
{
    public partial class Multiple2TabView : UITabView
    {
        protected GameObject btn1;

        protected override void CreateChildren()
        {
            btn1 = GameObject.transform.GetChild(2).gameObject;
        }

        protected override void OnAddEvent()
        {
            AddClick(btn1, _OnBtn1Click);
        }

        private void _OnBtn1Click(UIEventContext e) => OnBtn1Click(e);
        partial void OnBtn1Click(UIEventContext e);
    }
}
