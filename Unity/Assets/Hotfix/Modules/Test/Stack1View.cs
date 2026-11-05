namespace Ux.UI
{
    [UI]
    partial class Stack1View
    {
        protected override UILayer Layer => UILayer.View;
        public override UIType Type => UIType.Stackable;
        partial void OnBtn2Click(UIEventContext e)
        {
            UIMgr.Ins.Show<UI.Stack2View>();
        }
        partial void OnBtn3Click(UIEventContext e)
        {
            UIMgr.Ins.Show<UI.Stack3View>();
        }
        partial void OnBtn4Click(UIEventContext e)
        {
            UIMgr.Ins.Show<UI.Stack4View>();
        }
        partial void OnBtnBackClick(UIEventContext e)
        {
            HideSelf();            
        }
    }
}
