namespace Ux.UI
{
    [UI]
    partial class Stack3View
    {
        protected override UILayer Layer => UILayer.View;
        public override UIType Type => UIType.Normal;

        protected override void OnShow()
        {
            base.OnShow();            
        }
        partial void OnBtn1Click(UIEventContext e)
        {
            UIMgr.Ins.Create().Show<UI.Stack1View>();
        }
        partial void OnBtn2Click(UIEventContext e)
        {
            UIMgr.Ins.Create().Show<UI.Stack2View>();
        }
        partial void OnBtn4Click(UIEventContext e)
        {
            UIMgr.Ins.Create().Show<UI.Stack4View>();
        }
        partial void OnBtnBackClick(UIEventContext e)
        {
            HideSelf();            
        }
    }
}
