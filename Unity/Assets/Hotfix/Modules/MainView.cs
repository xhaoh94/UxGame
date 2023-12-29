using FairyGUI;
using Ux;
using System.Net;

namespace Ux.UI
{
    [UI]
    public partial class MainView
    {
        public override bool IsDestroy => false;
        public override UIType Type => UIType.Stack;
        protected override void OnShow(object param)
        {
            base.OnShow(param);
        }
        partial void OnBtnMultipleClick(EventContext e)
        {
            UIMgr.Ins.Show<MultipleView>();
        }
        partial void OnBtnNoneViewClick(EventContext e)
        {
            //UIMgr.Ins.Show<TipView>();
            UIMgr.Ins.Show<BagWindow2>();
        }
        partial void OnBtnNoneWindowClick(EventContext e)
        {
            UIMgr.Ins.Show<BagWindow>();
        }
        partial void OnBtnTestClick(EventContext e)
        {
            UIMgr.Dialog.SingleBtn("提示1", $"测试弹窗1", "确定", null);
            UIMgr.Dialog.DoubleBtn("提示2", $"测试弹窗2", "确定", null, "取消", null);
        }
        partial void OnBtnSingleClick(EventContext e)
        {
            Log.Debug("单击");
            Hide();
        }
        partial void OnBtnDoubleMultipleClick(EventContext e)
        {
            Log.Debug("双击");
        }
        partial void OnBtnLongClickLongPress(ref bool isBreak)
        {
            Log.Debug("长按");
        }
        partial void OnBtnBackClick(EventContext e)
        {
            Hide();
        }
    }
}