using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Ux.UI
{
    [UI]
    public partial class TestView
    {
        protected override UILayer Layer => UILayer.View;
        public override UIType Type => UIType.Stack;
        public override UIBlur Blur => UIBlur.Blur;

        protected override void OnInit()
        {
            base.OnInit();
            testList.SetItemRenderer<TestItem>();
            testList.SetItemProvider((int index) =>
            {
                var id = testList.GetData<int>(index);
                if (id % 2 == 0) return typeof(Test22Item);
                return typeof(TestItem);
            });
        }
        protected override void OnShow()
        {
            base.OnShow();
            var loader = new UxLoader();
            loader.autoSize = true;
            loader.url = "130G_TieKuang";
            GObject.asCom.AddChild(loader);

            // testUIModel.CloneMaterial = true;
            // testUIModel.Load(string.Format(PathHelper.Res.Prefab, "Hero_ZS")).Play(string.Format(PathHelper.Res.Prefab, "Hero_ZS@Stand"));

            // testRtModel.Load(string.Format(PathHelper.Res.Prefab, "Hero_ZS")).Play(string.Format(PathHelper.Res.Prefab, "Hero_ZS@Stand"));

            testList.SetDatas(new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 });
            TimeMgr.Ins.Timer(5, this, () =>
            {
                testList.SetDatas(new List<int> { 5, 4, 3, 2, 1, 0 });
                EventMgr.Ins.Run(EventType.Test_EventCall);
                //testList.List.ScrollToView(7);
                //EventMgr.Ins.Run(11111111);
            }).Repeat(1).Build();
            //EventMgr.Ins.Run(11111111);
        }
        partial void OnBtnMultipleClick(EventContext e)
        {
            UIMgr.Ins.Create().Show<Multiple2TabView>();
            //UIMgr.Ins.Hide<Multiple2TabView>();
            UIMgr.Ins.Create().Show<Multiple3TabView>();
        }
        partial void OnBtnNoneViewClick(EventContext e)
        {
            UIMgr.Ins.Create().Show<TipView>();            
        }
        partial void OnBtnNoneWindowClick(EventContext e)
        {
            UIMgr.Ins.Create().Show<BagWindow>();        
        }
        partial void OnBtnTestClick(EventContext e)
        {

            var messageBox3 = UIMgr.Dialog.Create();
            messageBox3.SetCheckBox("_test", "本次登录不显示咯");
            messageBox3.SetTitle("提示1");
            messageBox3.SetContent("测试弹窗2");
            messageBox3.SetBtn1(() =>
            {
                 Log.Debug("CheckBox");
            });
            messageBox3.Show();

            var messageBox1 = UIMgr.Dialog.Create();            
            messageBox1.SetTitle("提示1");
            messageBox1.SetContent("测试弹窗1");            
            messageBox1.Show();
            var messageBox2 = UIMgr.Dialog.Create();
            messageBox2.SetTitle("提示2");
            messageBox2.SetContent("测试弹窗2");            
            messageBox2.Show();
        }
        partial void OnBtnSingleClick(EventContext e)
        {
            Log.Debug("单击");
            UIMgr.Tip.Show("单击");
            // testRtModel.Load(string.Format(PathHelper.Res.Prefab, "Hero_CK")).Play(string.Format(PathHelper.Res.Prefab, "Hero_CK@Idle01"));
        }
        partial void OnBtnDoubleMultipleClick(EventContext e)
        {
            Log.Debug("双击");
            UIMgr.Tip.Show("双击");
            // testUIModel.Load(string.Format(PathHelper.Res.Prefab, "Hero_CK")).Play(string.Format(PathHelper.Res.Prefab, "Hero_CK@Idle01"));
        }
        partial void OnBtnLongClickLongPress(ref bool isBreak)
        {
            Log.Debug("长按");
            UIMgr.Tip.Show("长按");
        }
        partial void OnBtnBackClick(EventContext e)
        {
            HideSelf();
        }
        partial void OnTestListItemClick(IItemRenderer e)
        {
            Log.Debug(e.Index);
        }
    }
}
