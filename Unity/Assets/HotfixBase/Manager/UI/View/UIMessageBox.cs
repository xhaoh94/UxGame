using FairyGUI;
using System;
using System.Threading;
using static Ux.UIMgr;

namespace Ux
{
    public class UIMessageBox : UIWindow
    {
        protected override string PkgName => null;

        protected override string ResName => null;

        public override bool IsDestroy => false;
        public override UIType Type => UIType.Fixed;
        public override UIBlur Blur => UIBlur.None | UIBlur.Blur | UIBlur.Fixed;
        protected UIMessageBoxFactory.MessageBoxData dialogData;

        #region 组件
        protected virtual GTextField __txtTitle { get; private set; } = null;
        protected virtual GTextField __txtContent { get; private set; } = null;
        protected virtual UIButton __btnClose { get; private set; } = null;
        protected virtual UIButton __btn1 { get; private set; } = null;
        protected virtual UIButton __btn2 { get; private set; } = null;
        protected virtual UIButton __checkbox { get; private set; } = null;        
        #endregion

        public override void InitData(IUIData data, CallBackData initData)
        {
            OnHideCallBack += _Hide;
            base.InitData(data, initData);
        }

        protected override void ToShow(bool isAnim, int id, object param, bool isStack, CancellationTokenSource token)
        {
            dialogData = (UIMessageBoxFactory.MessageBoxData)param;
            dialogData.ShowCallBack?.Invoke(this);
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
            AddClick(__btnClose, HideSelf);
            foreach (var (paramType, value) in dialogData.Param)
            {
                switch (paramType)
                {
                    case UIMessageBoxFactory.ParamType.Title:
                        if (__txtTitle != null) __txtTitle.text = value.ToString();
                        break;
                    case UIMessageBoxFactory.ParamType.Content:
                        if (__txtContent != null) __txtContent.text = value.ToString();
                        break;
                    case UIMessageBoxFactory.ParamType.Btn1Title:
                        if (__btn1 != null)
                        {
                            __btn1.text = value.ToString();
                            __btn1.visible = true;
                        }
                        break;
                    case UIMessageBoxFactory.ParamType.Btn1Fn:
                        AddClick(__btn1, OnBtn1Click);
                        break;
                    case UIMessageBoxFactory.ParamType.Btn2Title:
                        if (__btn2 != null)
                        {
                            __btn2.text = value.ToString();
                            __btn2.visible = true;
                        }
                        break;
                    case UIMessageBoxFactory.ParamType.Btn2Fn:
                        AddClick(__btn2, OnBtn1Click);
                        break;
                    case UIMessageBoxFactory.ParamType.ChcekBox:
                        if (__checkbox != null)
                        {
                            __checkbox.text = ((UIMessageBoxFactory.MessageBoxCheckBox)value).Desc;
                            __checkbox.visible = true;
                        }
                        break;
                    case UIMessageBoxFactory.ParamType.Custom:
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
            if (dialogData.Param.TryGetValue(UIMessageBoxFactory.ParamType.Btn1Fn, out var obj))
            {
                (obj as Action)?.Invoke();
            }
            HideSelf();
        }
        void OnBtn2Click()
        {
            if (dialogData.Param.TryGetValue(UIMessageBoxFactory.ParamType.Btn2Fn, out var obj))
            {
                (obj as Action)?.Invoke();
            }
            HideSelf();
        }
        protected override void OnLayout()
        {
            SetLayout(UILayout.Center_Middle);
        }
        private void _Hide()
        {
            if (__checkbox != null
                && __checkbox.selected
                && dialogData.Param.TryGetValue(UIMessageBoxFactory.ParamType.ChcekBox, out var obj)
                && obj is UIMessageBoxFactory.MessageBoxCheckBox checkBox)
            {
                dialogData.PushTagCallBack?.Invoke(checkBox.Tag);
            }
            dialogData.HideCallBack?.Invoke(this);
        }
    }
}
