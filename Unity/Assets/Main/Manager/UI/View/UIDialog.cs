using FairyGUI;
using System;

namespace Ux
{
    [Package("Common")]
    public class UIDialog : UIWindow
    {
        protected override string PkgName => "Common";

        protected override string ResName => "CommonDialog";
        public override UIType Type => UIType.Fixed;

        protected UIDialogFactory.DialogData dialogData;

        #region 组件
        protected virtual GTextField __txtTitle { get; private set; } = null;
        protected virtual GTextField __txtContent { get; private set; } = null;
        protected virtual UIButton __btnClose { get; private set; } = null;
        protected virtual UIButton __btn1 { get; private set; } = null;
        protected virtual UIButton __btn2 { get; private set; } = null;
        protected virtual Controller __controller { get; private set; } = null;
        #endregion

        protected override void CreateChildren()
        {
            base.CreateChildren();
            var gCom = ObjAs<Window>().contentPane;
            __txtTitle = (GTextField)gCom.GetChild("txtTitle");
            __txtContent = (GTextField)gCom.GetChild("txtContent");
            __btnClose = new UIButton(gCom.GetChild("btnClose"), this);
            __btn1 = new UIButton(gCom.GetChild("btn1"), this);
            __btn2 = new UIButton(gCom.GetChild("btn2"), this);
            __controller = (Controller)gCom.GetController("dialogState");
        }

        public override void InitData(IUIData data, Action<IUI, bool> remove)
        {
            OnHideCallBack += _Hide;
            base.InitData(data, remove);
        }

        public override void DoShow(bool isAnim, object param, Action<IUI, object> showCb)
        {
            dialogData = (UIDialogFactory.DialogData)param;
            base.DoShow(isAnim, param, showCb);
        }
        protected override void OnShow(object param)
        {
            __btnClose?.AddClick(Hide);
            foreach (var (paramType, value) in dialogData.Param)
            {
                switch (paramType)
                {
                    case UIDialogFactory.ParamType.Title:
                        if (__txtTitle != null) __txtTitle.text = value.ToString();
                        break;
                    case UIDialogFactory.ParamType.Content:
                        if (__txtContent != null) __txtContent.text = value.ToString();
                        break;
                    case UIDialogFactory.ParamType.Btn1Title:
                        if (__btn1 != null) __btn1.text = value.ToString();
                        break;
                    case UIDialogFactory.ParamType.Btn1Fn:
                        __btn1?.AddClick(OnBtn1Click);
                        break;
                    case UIDialogFactory.ParamType.Btn2Title:
                        if (__btn2 != null) __btn2.text = value.ToString();
                        break;
                    case UIDialogFactory.ParamType.Btn2Fn:
                        __btn2?.AddClick(OnBtn1Click);
                        break;
                    case UIDialogFactory.ParamType.Custom:
                        OnParamCustom(value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (__controller != null)
            {
                switch (dialogData.DType)
                {
                    case UIDialogFactory.DialogType.SingleBtn:
                        __controller.selectedPage = "btn1";
                        break;
                    case UIDialogFactory.DialogType.DoubleBtn:
                        __controller.selectedPage = "btn2";
                        break;
                    case UIDialogFactory.DialogType.Custom:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

        }

        protected virtual void OnParamCustom(object param)
        {

        }
        protected virtual void OnDialogCustom()
        {

        }
        void OnBtn1Click()
        {
            if (dialogData.Param.TryGetValue(UIDialogFactory.ParamType.Btn1Fn, out var obj))
            {
                (obj as Action)?.Invoke();
            }
            Hide();
        }
        void OnBtn2Click()
        {
            if (dialogData.Param.TryGetValue(UIDialogFactory.ParamType.Btn2Fn, out var obj))
            {
                (obj as Action)?.Invoke();
            }
            Hide();
        }
        protected override void OnLayout()
        {
            SetLayout(UILayout.Center_Middle);
        }
        private void _Hide()
        {
            dialogData.HideCallBack?.Invoke(this);
        }
        public override void Hide()
        {
            UIMgr.Ins.Hide(Data.ID, true, false);
        }
    }
}
