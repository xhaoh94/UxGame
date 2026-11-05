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
            UIMgr.Ins.Create().SetParam(IUIParam.Create("1234")).Show<TestView>();
        }
        partial void OnBtnStack1Click(UIEventContext e)
        {
            UIMgr.Ins.Create().SetParam(IUIParam.Create("123")).Show<Stack1View>();
        }
        partial void OnBtnStack2Click(UIEventContext e)
        {
            UIMgr.Ins.Create().SetParam(IUIParam.Create(123)).Show<Stack2View>();
        }
        partial void OnBtnStack3Click(UIEventContext e)
        {
            UIMgr.Ins.Create().SetParam(IUIParam.Create(13)).Show<Stack3View>();
        }
        partial void OnBtnStack4Click(UIEventContext e)
        {
            UIMgr.Ins.Create().SetParam(IUIParam.Create(1)).Show<Stack4View>();
        }
    }
}