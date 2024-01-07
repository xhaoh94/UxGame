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
            UIMgr.Ins.Show<UI.TestView>();
        }
        partial void OnBtnStack1Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack1View>();
        }
        partial void OnBtnStack2Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack2View>();
        }
        partial void OnBtnStack3Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack3View>();
        }
        partial void OnBtnStack4Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack4View>();
        }
    }
}