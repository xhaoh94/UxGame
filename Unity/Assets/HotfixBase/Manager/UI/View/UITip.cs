using FairyGUI;
using System.Collections;
using System.Threading;
using UnityEngine;
using static Ux.UIMgr;

namespace Ux
{
    public class UITip : UIView
    {
        protected override string PkgName => null;

        protected override string ResName => null;
        public override bool IsDestroy => false;
        protected override UILayer Layer => UILayer.Tip;
        public override UIType Type => UIType.Fixed;
        public override UIBlur Blur => UIBlur.None;

        protected UITipFactory.TipData tipData;

        #region 组件        
        protected virtual GTextField __txtContent { get; private set; } = null;
        protected virtual Transition __transition { get; private set; } = null;
        #endregion

        public override void InitData(IUIData data, CallBackData initData)
        {
            OnHideCallBack += _Hide;
            base.InitData(data, initData);
        }

        protected override void ToShow(bool isAnim, int id, object param, bool isStack, CancellationTokenSource token)
        {
            tipData = (UITipFactory.TipData)param;
            tipData.ShowCallBack?.Invoke(this);
            InitParam();
            base.ToShow(isAnim, id, param, isStack, token);
        }

        protected virtual void InitParam()
        {
            if (__txtContent != null)
            {
                __txtContent.text = tipData.Content;
            }
            if (__transition != null)
            {
                __transition?.Play(HideSelf);
            }
        }

        protected override void OnLayout()
        {
            GObject parent = GObject.parent ?? UIMgr.Ins.GetLayer(UILayer.Root);
            GObject.SetPosition((parent.width - GObject.width) / 2, 150, 0);
            //SetLayout(UILayout.Center_Middle);
        }
        private void _Hide()
        {
            tipData.HideCallBack?.Invoke(this);
        }
    }
}