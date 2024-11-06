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

            testUIModel.CloneMaterial = true;
            testUIModel.Load($"{PathHelper.Res.Prefab}/Unit/Hero_ZS.prefab").Play("Hero_ZS@Stand");

            testRtModel.Load($"{PathHelper.Res.Prefab}/Unit/Hero_ZS.prefab").Play("Hero_ZS@Stand");

            testList.SetDatas(new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 });
            //this.DoTimer(5, 1, () =>
            //{
            //    testList.SetDatas(new List<int> { 5, 4, 3, 2, 1, 0 });
            //    //testList.List.ScrollToView(7);
            //    EventMgr.Ins.Run(11111111);
            //});
            //EventMgr.Ins.Run(11111111);
        }
        partial void OnBtnMultipleClick(EventContext e)
        {
            UIMgr.Ins.Show<Multiple2TabView>();
            //UIMgr.Ins.Hide<Multiple2TabView>();
            UIMgr.Ins.Show<Multiple3TabView>();
        }
        partial void OnBtnNoneViewClick(EventContext e)
        {
            UIMgr.Ins.Show<TipView>();
        }
        partial void OnBtnNoneWindowClick(EventContext e)
        {
            UIMgr.Ins.Show<BagWindow>();
        }
        partial void OnBtnTestClick(EventContext e)
        {
            UIMgr.MessageBox.SingleBtnCheckBox("_test", "本次登录不显示咯", "提示2", $"测试弹窗2", "确定", () =>
            {
                Log.Debug("CheckBox");
            });
            //UIMgr.Dialog.SingleBtn("提示1", $"测试弹窗1", "确定", null);
            //UIMgr.Dialog.DoubleBtn("提示2", $"测试弹窗2", "确定", null, "取消", null);
        }
        partial void OnBtnSingleClick(EventContext e)
        {
            Log.Debug("单击");
            UIMgr.Tip.Show("单击");
            testRtModel.Load("Hero_CK").Play("Hero_CK@Stand");
        }
        partial void OnBtnDoubleMultipleClick(EventContext e)
        {
            Log.Debug("双击");
            UIMgr.Tip.Show("双击");
            testUIModel.Load("Hero_CK").Play("Hero_CK@Stand");
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
