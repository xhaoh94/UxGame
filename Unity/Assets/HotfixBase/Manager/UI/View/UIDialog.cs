using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Ux.UIMgr;

namespace Ux
{
    public class UIDialog : UIWindow
    {
        public override int HideDestroyTime => -1;
        public override UIType Type => UIType.Fixed;
        public override UIBlur Blur => UIBlur.Blur;

        protected DialogData dialogData;
        private DialogCallbackData _callbackData;

        #region UI 组件
        protected virtual TMP_Text __txtTitle { get; } = null;
        protected virtual TMP_Text __txtContent { get; } = null;
        protected virtual GameObject __btnClose { get; } = null;
        protected virtual GameObject __btn1 { get; } = null;
        protected virtual GameObject __btn2 { get; } = null;
        protected virtual Toggle __checkbox { get; } = null;
        #endregion

        public override void InitData(IUIData data, CallbackData initData, int instanceId)
        {
            OnHideCallback += _Hide;
            base.InitData(data, initData, instanceId);
        }

        protected override void ToShow(bool isAnim, int id, bool isStack, int showVersion)
        {
            if (TryGetParam(out DialogData value))
            {
                dialogData = value;
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
            SetText(__txtTitle, string.IsNullOrEmpty(dialogData.Title) ? "提示" : dialogData.Title);
            SetText(__txtContent, dialogData.Content);

            SetActive(__btn1, dialogData.Btn1.Visible);
            if (dialogData.Btn1.Visible)
            {
                SetText(__btn1, dialogData.Btn1.BtnTitle);
                AddClick(__btn1, OnBtn1Click);
            }

            SetActive(__btn2, dialogData.Btn2.Visible);
            if (dialogData.Btn2.Visible)
            {
                SetText(__btn2, dialogData.Btn2.BtnTitle);
                AddClick(__btn2, OnBtn2Click);
            }

            SetActive(__checkbox, dialogData.CheckBoxData.Visible);
            if (dialogData.CheckBoxData.Visible)
                SetText(__checkbox, dialogData.CheckBoxData.CheckBoxDesc);

            if (__btnClose != null)
                AddClick(__btnClose, HideSelf);

            OnParamCustom();
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        private static void SetActive(Toggle target, bool active)
        {
            if (target != null)
                target.gameObject.SetActive(active);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }

        private static void SetText(Component target, string value)
        {
            if (target == null)
                return;
            var text = target.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = value ?? string.Empty;
        }

        private static void SetText(GameObject target, string value)
        {
            if (target == null)
                return;
            var text = target.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = value ?? string.Empty;
        }

        protected virtual void OnParamCustom()
        {
        }

        private void OnBtn1Click()
        {
            dialogData.Btn1.BtnCallback?.Invoke();
            HideSelf();
        }

        private void OnBtn2Click()
        {
            dialogData.Btn2.BtnCallback?.Invoke();
            HideSelf();
        }

        private void _Hide()
        {
            var selected = false;
            if (__checkbox != null)
            {
                selected = __checkbox.isOn;
            }

            if (selected)
                dialogData.CheckBoxData.CheckBoxCallback?.Invoke(dialogData.CheckBoxData.CheckBoxTag);

            _callbackData.HideCallback?.Invoke(this);
        }
    }
}
