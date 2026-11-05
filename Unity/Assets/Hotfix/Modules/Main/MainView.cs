using Ux;

namespace Ux.UI
{
    [UI]
    public partial class MainView
    {
        protected override UILayer Layer => UILayer.View;
        public override UIType Type => UIType.NonStackable;        
        partial void OnBtnBackClick(UIEventContext e)
        {
            GameMain.Machine.Enter<StateLogin>();
        }

        partial void OnBtnMainViewClick(UIEventContext e)
        {
            UIMgr.Ins.Show<TestView>(IUIParam.Create("1234"));
        }
        partial void OnBtnStack1Click(UIEventContext e)
        {
            UIMgr.Ins.Show<Stack1View>(IUIParam.Create("123"));
        }
        partial void OnBtnStack2Click(UIEventContext e)
        {
            UIMgr.Ins.Show<Stack2View>(IUIParam.Create(123));
        }
        partial void OnBtnStack3Click(UIEventContext e)
        {
            UIMgr.Ins.Show<Stack3View>(IUIParam.Create(13));
        }
        partial void OnBtnStack4Click(UIEventContext e)
        {
            UIMgr.Ins.Show<Stack4View>(IUIParam.Create(1));
        }
    }
}