using FairyGUI;
using System;
using static Ux.UIMgr;

namespace Ux
{
    public class UIDialog : UIWindow
    {
        protected override string PkgName => null;

        protected override string ResName => null;

        public override int HideDestroyTime => -1;
        public override UIType Type => UIType.Fixed;
        public override UIBlur Blur => UIBlur.None | UIBlur.Blur | UIBlur.Fixed;
        protected DialogData dialogData;
        private DialogCallbackData _callbackData;

        #region 组件
        protected virtual GTextField __txtTitle { get; private set; } = null;
        protected virtual GTextField __txtContent { get; private set; } = null;
        protected virtual UIButton __btnClose { get; private set; } = null;
        protected virtual UIButton __btn1 { get; private set; } = null;
        protected virtual UIButton __btn2 { get; private set; } = null;
        protected virtual UIButton __checkbox { get; private set; } = null;
        #endregion

        public override void InitData(IUIData data, CallbackData initData)
        {
            OnHideCallback += _Hide;
            base.InitData(data, initData);
        }

        protected override void ToShow(bool isAnim, int id, bool isStack, int showVersion)
        {
            if (TryGetParam(out DialogData _dialogData))
            {
                dialogData = _dialogData;
            }
            else
            {
                Log.Error("Dialog 参数类型错误");
                return;
            }
            if (TryGetParam(out DialogCallbackData callbackData, UIParamType.B))
            {
                _callbackData = callbackData;
                _callbackData.ShowCallback?.Invoke(this);
            }
            else
            {
                Log.Error("DialogCallbackData 参数类型错误");
                return;
            }

            base.ToShow(isAnim, id, isStack, showVersion);
            InitParam();
        }

        protected virtual void InitParam()
        {
            AddClick(__btnClose, HideSelf);
            // 标题
            if (__txtTitle != null)
            {
                __txtTitle.text =  string.IsNullOrEmpty(dialogData.Title) ? "提示" : dialogData.Title;
            }

            // 内容
            if (__txtContent != null && !string.IsNullOrEmpty(dialogData.Content))
            {
                __txtContent.text = dialogData.Content;
            }

            // 按钮1
            if (__btn1 != null)
            {
                __btn1.visible = dialogData.Btn1.Visible;
                if (dialogData.Btn1.Visible)
                {
                    __btn1.text =  dialogData.Btn1.BtnTitle;
                    AddClick(__btn1, OnBtn1Click);
                }
            }

            // 按钮2
            if (__btn2 != null)
            {
                __btn2.visible = dialogData.Btn2.Visible;
                if (dialogData.Btn2.Visible)
                {
                    __btn2.text = dialogData.Btn2.BtnTitle;
                    AddClick(__btn2, OnBtn2Click);
                }
            }

            // 复选框
            if (__checkbox != null)
            {
                __checkbox.visible = dialogData.CheckBoxData.Visible;
                if (dialogData.CheckBoxData.Visible)
                {
                    __checkbox.text = dialogData.CheckBoxData.CheckBoxDesc;
                }
            }

            // 自定义参数处理
            OnParamCustom();
        }

        protected virtual void OnParamCustom()
        {
            // 子类可重写处理额外逻辑
        }
        void OnBtn1Click()
        {
            dialogData.Btn1.BtnCallback?.Invoke();
            HideSelf();
        }

        void OnBtn2Click()
        {
            dialogData.Btn2.BtnCallback?.Invoke();
            HideSelf();
        }
        protected override void OnLayout()
        {
            SetLayout(UILayout.Center_Middle);
        }
        private void _Hide()
        {
            // 如果勾选了复选框，触发回调
            if (__checkbox != null && __checkbox.selected)
            {
                dialogData.CheckBoxData.CheckBoxCallback?.Invoke(dialogData.CheckBoxData.CheckBoxTag);
            }

            _callbackData.HideCallback?.Invoke(this);
        }
    }
}
