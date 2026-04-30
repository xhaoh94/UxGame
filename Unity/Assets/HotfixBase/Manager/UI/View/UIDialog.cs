using FairyGUI;
using System;
using System.Threading;
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

        protected override void ToShow(bool isAnim, int id, bool isStack, CancellationTokenSource token)
        {
            if(TryGetParam(out DialogData _dialogData))
            {
                dialogData = _dialogData;
                dialogData.ShowCallback?.Invoke(this);
            }
            else
            {
                Log.Error("Dialog 参数类型错误");
                return;
            }
            
            InitParam();
            base.ToShow(isAnim, id, isStack, token);
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
            
            // 标题
            if (__txtTitle != null && !string.IsNullOrEmpty(dialogData.Title))
            {
                __txtTitle.text = dialogData.Title;
            }
            
            // 内容
            if (__txtContent != null && !string.IsNullOrEmpty(dialogData.Content))
            {
                __txtContent.text = dialogData.Content;
            }
            
            // 按钮1
            if (__btn1 != null && !string.IsNullOrEmpty(dialogData.Btn1.BtnTitle))
            {
                __btn1.text = dialogData.Btn1.BtnTitle;
                __btn1.visible = true;
                if (dialogData.Btn1.BtnCallback != null)
                {
                    AddClick(__btn1, OnBtn1Click);
                }
            }
            
            // 按钮2
            if (__btn2 != null && !string.IsNullOrEmpty(dialogData.Btn2.BtnTitle))
            {
                __btn2.text = dialogData.Btn2.BtnTitle;
                __btn2.visible = true;
                if (dialogData.Btn2.BtnCallback != null)
                {
                    AddClick(__btn2, OnBtn2Click);
                }
            }
            
            // 复选框
            if (__checkbox != null && dialogData.CheckBoxData.HasCheckBox)
            {
                __checkbox.text = dialogData.CheckBoxData.CheckBoxDesc;
                __checkbox.visible = true;
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
            if (__checkbox != null
                && __checkbox.selected
                && dialogData.CheckBoxData.HasCheckBox)
            {
                dialogData.CheckBoxData.CheckBoxCallback?.Invoke(dialogData.CheckBoxData.CheckBoxTag, true);
            }
            
            dialogData.HideCallback?.Invoke(this);
        }
    }
}
