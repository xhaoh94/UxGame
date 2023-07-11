using FairyGUI;

namespace Ux.UI
{
    [UI]
    partial class MultipleView
    {
        //public override bool IsDestroy => false;
        protected override UILayer Layer => UILayer.Normal;
        protected override IUIAnim ShowAnim => new UITransition(t0);
        protected override IUIAnim HideAnim => new UITransition(t1);

        protected override void OnShow(object param)
        {
            base.OnShow(param);
            //TimeMgr.Ins.DoTimer(5, 1, () =>
            //{
            //    var data = UIMgr.Ins.GetUIData(ID);
            //    data.Children.Reverse();
            //    RefreshTab();
            //    Log.Debug("RefreshTab");
            //});
        }

        protected override void OnHide()
        {
            base.OnHide();
            TimeMgr.Ins.DoTimer(0.1f, 1, () =>
            {
                UIMgr.Ins.Show<Multiple1TabView>();
                UIMgr.Ins.Hide<Multiple1TabView>();
            });
        }

    }

    [UI(typeof(MultipleView))]
    [TabTitle("T1")]
    partial class Multiple1TabView
    {
        public override bool IsDestroy => false;
        //protected override IUIAnim ShowAnim => new UITransition(t0);
        //protected override IUIAnim HideAnim => new UITransition(t1);
    }

    [UI(typeof(MultipleView))]
    [TabTitle("T2")]
    partial class Multiple2TabView
    {
        public override bool IsDestroy => false;
        protected override IUIAnim ShowAnim => new UITransition(t0);
        protected override IUIAnim HideAnim => new UITransition(t1);
    }

    [UI(typeof(MultipleView))]
    [TabTitle("T3")]
    partial class Multiple3TabView
    {
        public override bool IsDestroy => false;
    }
}