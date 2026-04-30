using FairyGUI;
using Ux;
using System.Net;

namespace Ux.UI
{
    [UI]
    public partial class MainView
    {
        protected override UILayer Layer => UILayer.Normal;
        public override UIType Type => UIType.Fixed;        
        partial void OnBtnBackClick(EventContext e)
        {
            GameMain.Machine.Enter<StateLogin>();
        }

        partial void OnBtnMainViewClick(EventContext e)
        {
            UIMgr.Ins.Create().SetParam(IUIParam.Create("1234")).Show<TestView>();
        }
        partial void OnBtnStack1Click(EventContext e)
        {
            UIMgr.Ins.Create().SetParam(IUIParam.Create("123")).Show<Stack1View>();
        }
        partial void OnBtnStack2Click(EventContext e)
        {
            UIMgr.Ins.Create().SetParam(IUIParam.Create(123)).Show<Stack2View>();
        }
        partial void OnBtnStack3Click(EventContext e)
        {
            UIMgr.Ins.Create().SetParam(IUIParam.Create(13)).Show<Stack3View>();
        }
        partial void OnBtnStack4Click(EventContext e)
        {
            UIMgr.Ins.Create().SetParam(IUIParam.Create(1)).Show<Stack4View>();
        }
    }
}