//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
    [Package("Main", "Common")]
    public partial class MainView : UIView
    {
        protected override string PkgName => "Main";
        protected override string ResName => "MainView";

        protected Btn1 btnMultiple;
        protected Btn1 btnNoneWindow;
        protected Btn1 btnNoneView;
        protected Btn1 btnSingle;
        protected Btn1 btnDouble;
        protected Btn1 btnLongClick;
        protected Btn1 btnTest;
        protected Btn1 btnBack;
        protected override void CreateChildren()
        {
            try
            {
                var gCom = ObjAs<GComponent>();
                btnMultiple = new Btn1(gCom.GetChildAt(2), this);
                btnNoneWindow = new Btn1(gCom.GetChildAt(3), this);
                btnNoneView = new Btn1(gCom.GetChildAt(4), this);
                btnSingle = new Btn1(gCom.GetChildAt(5), this);
                btnDouble = new Btn1(gCom.GetChildAt(6), this);
                btnLongClick = new Btn1(gCom.GetChildAt(7), this);
                btnTest = new Btn1(gCom.GetChildAt(8), this);
                btnBack = new Btn1(gCom.GetChildAt(9), this);
            }
            catch (System.Exception e)
            {
                Log.Error(e);
            }
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
        }
        void _OnBtnMultipleClick(EventContext e)
        {
            OnBtnMultipleClick(e);
        }
        partial void OnBtnMultipleClick(EventContext e);
        void _OnBtnNoneWindowClick(EventContext e)
        {
            OnBtnNoneWindowClick(e);
        }
        partial void OnBtnNoneWindowClick(EventContext e);
        void _OnBtnNoneViewClick(EventContext e)
        {
            OnBtnNoneViewClick(e);
        }
        partial void OnBtnNoneViewClick(EventContext e);
        void _OnBtnSingleClick(EventContext e)
        {
            OnBtnSingleClick(e);
        }
        partial void OnBtnSingleClick(EventContext e);
        void _OnBtnTestClick(EventContext e)
        {
            OnBtnTestClick(e);
        }
        partial void OnBtnTestClick(EventContext e);
        void _OnBtnBackClick(EventContext e)
        {
            OnBtnBackClick(e);
        }
        partial void OnBtnBackClick(EventContext e);
        void _OnBtnDoubleMultipleClick(EventContext e)
        {
            OnBtnDoubleMultipleClick(e);
        }
        partial void OnBtnDoubleMultipleClick(EventContext e);
        bool _OnBtnLongClickLongPress()
        {
            bool b = false;
            OnBtnLongClickLongPress(ref b);
            return b;
        }
        partial void OnBtnLongClickLongPress(ref bool isBreak);
    }
}
