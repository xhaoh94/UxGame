using FairyGUI;
using System;
using System.Threading;
using static Ux.UIMgr;

namespace Ux
{
    [Package("Common")]
    public class UIDialog : UIWindow
    {
        protected override string PkgName => "Common";

        protected override string ResName => "CommonDialog";
        public override UIType Type => UIType.Fixed;
        public override UIBlur Blur => UIBlur.None | UIBlur.Blur | UIBlur.Fixed;
        protected UIDialogFactory.DialogData dialogData;

        #region 组件
        protected virtual GTextField __txtTitle { get; private set; } = null;
        protected virtual GTextField __txtContent { get; private set; } = null;
        protected virtual UIButton __btnClose { get; private set; } = null;
        protected virtual UIButton __btn1 { get; private set; } = null;
        protected virtual UIButton __btn2 { get; private set; } = null;
        protected virtual UIButton __checkbox { get; private set; } = null;        
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
            __checkbox = new UIButton(gCom.GetChild("checkbox"), this);
        }

        public override void InitData(IUIData data, CallBackData initData)
        {
            OnHideCallBack += _Hide;
            base.InitData(data, initData);
        }

        protected override void ToShow(bool isAnim, int id, object param, bool isStack, CancellationTokenSource token)
        {
            dialogData = (UIDialogFactory.DialogData)param;
            InitParam();
            base.ToShow(isAnim, id, param, isStack, token);
        }
        protected virtual void ResetBtns()
        {
            if (__btn1 != null)
            {
                __btn1.visible = false;
            }
            if (__btn2 != null)
            {
                __btn2.visible = false;
            }
            if (__checkbox != null)
            {
                __checkbox.visible = false;
            }
        }
        protected virtual void InitParam()
        {
            ResetBtns();
            AddClick(__btnClose, Hide);
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
                        if (__btn1 != null)
                        {
                            __btn1.text = value.ToString();
                            __btn1.visible = true;
                        }
                        break;
                    case UIDialogFactory.ParamType.Btn1Fn:
                        AddClick(__btn1, OnBtn1Click);
                        break;
                    case UIDialogFactory.ParamType.Btn2Title:
                        if (__btn2 != null)
                        {
                            __btn2.text = value.ToString();
                            __btn2.visible = true;
                        }
                        break;
                    case UIDialogFactory.ParamType.Btn2Fn:
                        AddClick(__btn2, OnBtn1Click);
                        break;
                    case UIDialogFactory.ParamType.ChcekBox:
                        if (__checkbox != null)
                        {
                            __checkbox.text = ((UIDialogFactory.DialogCheckBox)value).Desc;
                            __checkbox.visible = true;
                        }
                        break;
                    case UIDialogFactory.ParamType.Custom:
                        OnParamCustom(value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

        }

        protected virtual void OnParamCustom(object param)
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
            if (__checkbox != null
                && __checkbox.selected
                && dialogData.Param.TryGetValue(UIDialogFactory.ParamType.ChcekBox, out var obj)
                && obj is UIDialogFactory.DialogCheckBox checkBox)
            {
                dialogData.PushTagCallBack?.Invoke(checkBox.Tag);
            }
            dialogData.HideCallBack?.Invoke(this);
        }
    }
}
