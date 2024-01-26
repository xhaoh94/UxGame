using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux.UI
{
    [UI]
    public partial class TestView
    {
        protected override UILayer Layer => UILayer.View;
        public override UIType Type => UIType.Stack;
        public override UIBlur Blur => UIBlur.Blur;
        partial void OnBtnMultipleClick(EventContext e)
        {
            UIMgr.Ins.Show<MultipleView>();
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
            UIMgr.Dialog.SingleBtnCheckBox("_test", "本次登录不显示咯", "提示2", $"测试弹窗2", "确定", () =>
            {
                Log.Debug("CheckBox");
            });
            //UIMgr.Dialog.SingleBtn("提示1", $"测试弹窗1", "确定", null);
            //UIMgr.Dialog.DoubleBtn("提示2", $"测试弹窗2", "确定", null, "取消", null);
        }
        partial void OnBtnSingleClick(EventContext e)
        {
            Log.Debug("单击");
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
