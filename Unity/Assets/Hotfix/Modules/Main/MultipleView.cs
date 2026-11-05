using Ux;
using System;

namespace Ux.UI
{
    [UI]
    partial class MultipleView
    {
        protected override void OnShow()
        {
            base.OnShow();
            TimeMgr.Ins.Timer(5, this, () => { }).Repeat(1).Build();
        }

        int cnt = 0;
        protected override void OnHide()
        {
            base.OnHide();
            if (cnt++ < 3)
            {
                TimeMgr.Ins.Timer(.4f, this, () =>
                {
                    UIMgr.Ins.Show<Multiple2TabView>();
                    UIMgr.Ins.Show<Multiple3TabView>();
                }).Repeat(1).Build();
            }
        }
    }

    [UI(typeof(MultipleView))]
    [TabTitle("T1")]
    partial class Multiple1TabView
    {
        public override int HideDestroyTime => -1;

        partial void OnBtn1Click(UIEventContext e)
        {
            UIMgr.Ins.Show<Stack1View>();
        }
    }

    [UI(typeof(MultipleView))]
    [TabTitle("T2")]
    partial class Multiple2TabView
    {
        public override int HideDestroyTime => -1;

        partial void OnBtn1Click(UIEventContext e)
        {
            UIMgr.Ins.Show<Stack2View>();
        }
    }

    [UI(typeof(MultipleView))]
    [TabTitle("T3")]
    partial class Multiple3TabView
    {
        public override int HideDestroyTime => -1;

        partial void OnBtn1Click(UIEventContext e)
        {
            UIMgr.Ins.Show<Stack3View>();
        }
    }
}
