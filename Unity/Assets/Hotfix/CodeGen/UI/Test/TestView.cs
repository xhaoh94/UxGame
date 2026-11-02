//自动生成的代码，请勿修改!!!
using UnityEngine;
namespace Ux.UI
{
    public partial class TestView : UIView
    {
        protected GameObject btnMultiple;
        protected GameObject btnNoneWindow;
        protected GameObject btnNoneView;
        protected GameObject btnSingle;
        protected GameObject btnDouble;
        protected GameObject btnLongClick;
        protected GameObject btnTest;
        protected GameObject btnBack;
        protected UIList testList;

        protected override void CreateChildren()
        {
            var root = GameObject.transform;
            btnMultiple = root.GetChild(2).gameObject;
            btnNoneWindow = root.GetChild(3).gameObject;
            btnNoneView = root.GetChild(4).gameObject;
            btnSingle = root.GetChild(5).gameObject;
            btnDouble = root.GetChild(6).gameObject;
            btnLongClick = root.GetChild(7).gameObject;
            btnTest = root.GetChild(8).gameObject;
            btnBack = root.GetChild(9).gameObject;
            testList = new UIList(root.GetChild(12).gameObject, this);
        }

        protected override void OnAddEvent()
        {
            AddClick(btnMultiple, _OnBtnMultipleClick);
            AddClick(btnNoneWindow, _OnBtnNoneWindowClick);
            AddClick(btnNoneView, _OnBtnNoneViewClick);
            AddClick(btnSingle, _OnBtnSingleClick);
            AddClick(btnTest, _OnBtnTestClick);
            AddClick(btnBack, _OnBtnBackClick);
            AddMultipleClick(btnDouble, _OnBtnDoubleMultipleClick, 2, 0.2f);
            AddLongPress(btnLongClick, -1f, _OnBtnLongClickLongPress, 0.2f, 0, 50);
            AddItemClick(testList, _OnTestListItemClick);
        }

        private void _OnBtnMultipleClick(UIEventContext e) => OnBtnMultipleClick(e);
        partial void OnBtnMultipleClick(UIEventContext e);
        private void _OnBtnNoneWindowClick(UIEventContext e) => OnBtnNoneWindowClick(e);
        partial void OnBtnNoneWindowClick(UIEventContext e);
        private void _OnBtnNoneViewClick(UIEventContext e) => OnBtnNoneViewClick(e);
        partial void OnBtnNoneViewClick(UIEventContext e);
        private void _OnBtnSingleClick(UIEventContext e) => OnBtnSingleClick(e);
        partial void OnBtnSingleClick(UIEventContext e);
        private void _OnBtnTestClick(UIEventContext e) => OnBtnTestClick(e);
        partial void OnBtnTestClick(UIEventContext e);
        private void _OnBtnBackClick(UIEventContext e) => OnBtnBackClick(e);
        partial void OnBtnBackClick(UIEventContext e);
        private void _OnBtnDoubleMultipleClick(UIEventContext e) => OnBtnDoubleMultipleClick(e);
        partial void OnBtnDoubleMultipleClick(UIEventContext e);
        private bool _OnBtnLongClickLongPress()
        {
            bool isBreak = false;
            OnBtnLongClickLongPress(ref isBreak);
            return isBreak;
        }
        partial void OnBtnLongClickLongPress(ref bool isBreak);
        private void _OnTestListItemClick(IItemRenderer item) => OnTestListItemClick(item);
        partial void OnTestListItemClick(IItemRenderer item);
    }
}
