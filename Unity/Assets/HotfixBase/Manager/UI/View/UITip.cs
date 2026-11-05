using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Ux.UIMgr;

namespace Ux
{
    public class UITip : UIView
    {
        public override int HideDestroyTime => -1;
        protected override UILayer Layer => UILayer.Tip;
        public override UIType Type => UIType.NonStackable;
        public override UIBlurMode Blur => UIBlurMode.Off;
        public override UIFocusMode FocusMode => UIFocusMode.None;

        protected UITipFactory.TipData tipData;
        protected virtual TMP_Text __txtContent { get; } = null;

        public override void InitData(int instanceId, IUIData data)
        {
            OnHideCallback += _Hide;
            base.InitData(instanceId, data);
        }

        protected override void ToShow(bool isAnim, int id, bool isStack, int showVersion)
        {
            if (TryGetParam(out UITipFactory.TipData value))
            {
                tipData = value;
                tipData.ShowCallBack?.Invoke(this);
            }
            else
            {
                Log.Error("Tip 参数类型错误");
                return;
            }

            InitParam();
            base.ToShow(isAnim, id, isStack, showVersion);
        }

        protected virtual void InitParam()
        {
            if (__txtContent != null)
                __txtContent.text = tipData.Content ?? string.Empty;
        }

        private void _Hide()
        {
            tipData.HideCallBack?.Invoke(this);
        }
    }
}
