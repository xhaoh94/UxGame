using FairyGUI;
using System;

namespace Ux.UI
{
    partial class CommonTabItem
    {
        protected override void OnShow()
        {
            if (ShowParame is int id)
            {
                var data = UIMgr.Ins.GetUIData(id);
                if (data.TabData.Title is string title)
                {
                    txtTitle.text = title;
                }
            }
        }
    }
    [UI]
    partial class MultipleView
    {
        //public override bool IsDestroy => false;        
        protected override IUIAnim ShowAnim => new UIAnimTransition(t0);
        protected override IUIAnim HideAnim => new UIAnimTransition(t1);
        public override UIType Type => UIType.Stack;
        protected override void OnInit()
        {
            base.OnInit();
            SetItemRenderer<CommonTabItem>();
            //SetTabRenderer
        }
        protected override void OnShow()
        {
            base.OnShow();
            TimeMgr.Ins.DoTimer(5, 1, this, () =>
            {
                //var data = UIMgr.Ins.GetUIData(ID);
                //data.Children.Reverse();
                //RefreshTab();                                
            });
        }

        protected override void OnHide()
        {
            base.OnHide();
        }

    }

    [UI(typeof(MultipleView))]
    [TabTitle("T1")]
    partial class Multiple1TabView
    {
        public override bool IsDestroy => false;
        //protected override IUIAnim ShowAnim => new UITransition(t0);
        //protected override IUIAnim HideAnim => new UITransition(t1);        
        partial void OnBtn1Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack1View>();
            //UIMgr.Ins.Hide<MultipleView>();
            //UIMgr.Ins.Show<MultipleView>("222");
        }
    }

    [UI(typeof(MultipleView))]
    [TabTitle("T2")]
    partial class Multiple2TabView
    {
        public override bool IsDestroy => false;
        protected override IUIAnim ShowAnim => new UIAnimTransition(t0);
        protected override IUIAnim HideAnim => new UIAnimTransition(t1);
        protected override void OnShow()
        {
            base.OnShow();
        }
        partial void OnBtn1Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack2View>();
        }
    }

    [UI(typeof(MultipleView))]
    [TabTitle("T3")]
    partial class Multiple3TabView
    {
        public override bool IsDestroy => false;
        partial void OnBtn1Click(EventContext e)
        {
            UIMgr.Ins.Show<UI.Stack3View>();
        }
    }
}