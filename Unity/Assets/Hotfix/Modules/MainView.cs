using FairyGUI;
using Ux;
using System.Net;

namespace Ux.UI
{
    [UI]
    public partial class MainView
    {
        public override bool IsDestroy => false;
        protected override UILayer Layer => UILayer.Normal;
        protected override void OnShow(object param)
        {
            base.OnShow(param);

            // AddLongClick(OnLongBagClick, 2f, 0.2f);
            //AddDoubleClick(OnBagDoubleClick);            
        }
        partial void OnBtnMultipleClick(EventContext e)
        {
            UIMgr.Instance.Show<MultipleView>();
        }
        partial void OnBtnNoneViewClick(EventContext e)
        {
            UIMgr.Instance.Show<TipView>();
        }
        partial void OnBtnNoneWindowClick(EventContext e)
        {
            UIMgr.Instance.Show<BagWindow>();
        }
        partial void OnBtnTestClick(EventContext e)
        {
            UIMgr.Dialog.SingleBtn("提示1", $"获取资源版本失败，请检测网络状态。", "确定", null);
            UIMgr.Dialog.DoubleBtn("提示2", $"获取资源版本失败，请检测网络状态。", "确定", null, "取消", null);
        }
        partial void OnBtnSingleClick(EventContext e)
        {
            Log.Debug("单击");            
        }
        partial void OnBtnDoubleDoubleClick(EventContext e)
        {
            Log.Debug("双击");
        }
        partial void OnBtnLongClickLongClick(ref bool isBreak)
        {
            Log.Debug("长按");
            isBreak = false;
        }
        partial void OnBtnBackClick(EventContext e)
        {
            GameMain.Machine.Enter<StateLogin>();
        }
    }
}