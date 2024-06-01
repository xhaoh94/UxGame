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
            UIMgr.Ins.Show<UI.TestView>(IUIParam.Create("1234"));
        }
        partial void OnBtnStack1Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack1View>(IUIParam.Create("123"));
        }
        partial void OnBtnStack2Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack2View>(IUIParam.Create(123));
        }
        partial void OnBtnStack3Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack3View>(IUIParam.Create(13));
        }
        partial void OnBtnStack4Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack4View>(IUIParam.Create(12443));
        }
    }
}