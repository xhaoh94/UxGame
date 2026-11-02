//自动生成的代码，请勿修改!!!
using UnityEngine;

namespace Ux.UI
{
    public partial class MainView : UIView
    {
        protected GameObject btnMainView;
        protected GameObject btnBack;
        protected GameObject btnStack1;
        protected GameObject btnStack2;
        protected GameObject btnStack3;
        protected GameObject btnStack4;

        protected override void CreateChildren()
        {
            var root = GameObject.transform;
            btnMainView = root.GetChild(0).gameObject;
            btnBack = root.GetChild(1).gameObject;
            btnStack1 = root.GetChild(2).gameObject;
            btnStack2 = root.GetChild(3).gameObject;
            btnStack3 = root.GetChild(4).gameObject;
            btnStack4 = root.GetChild(5).gameObject;
        }

        protected override void OnAddEvent()
        {
            AddClick(btnMainView, _OnBtnMainViewClick);
            AddClick(btnBack, _OnBtnBackClick);
            AddClick(btnStack1, _OnBtnStack1Click);
            AddClick(btnStack2, _OnBtnStack2Click);
            AddClick(btnStack3, _OnBtnStack3Click);
            AddClick(btnStack4, _OnBtnStack4Click);
        }

        private void _OnBtnMainViewClick(UIEventContext e)
        {
            OnBtnMainViewClick(e);
        }
        partial void OnBtnMainViewClick(UIEventContext e);

        private void _OnBtnBackClick(UIEventContext e)
        {
            OnBtnBackClick(e);
        }
        partial void OnBtnBackClick(UIEventContext e);

        private void _OnBtnStack1Click(UIEventContext e)
        {
            OnBtnStack1Click(e);
        }
        partial void OnBtnStack1Click(UIEventContext e);

        private void _OnBtnStack2Click(UIEventContext e)
        {
            OnBtnStack2Click(e);
        }
        partial void OnBtnStack2Click(UIEventContext e);

        private void _OnBtnStack3Click(UIEventContext e)
        {
            OnBtnStack3Click(e);
        }
        partial void OnBtnStack3Click(UIEventContext e);

        private void _OnBtnStack4Click(UIEventContext e)
        {
            OnBtnStack4Click(e);
        }
        partial void OnBtnStack4Click(UIEventContext e);
    }
}
