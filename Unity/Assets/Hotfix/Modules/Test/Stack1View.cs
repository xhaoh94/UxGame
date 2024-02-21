using FairyGUI;

namespace Ux.UI
{
    [UI]
    partial class Stack1View
    {
        protected override UILayer Layer => UILayer.View;
        public override UIType Type => UIType.Stack;
        partial void OnBtn2Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack2View>();
        }
        partial void OnBtn3Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack3View>();
        }
        partial void OnBtn4Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack4View>();
        }
        partial void OnBtnBackClick(EventContext e)
        {
            HideSelf();
        }
    }
}
