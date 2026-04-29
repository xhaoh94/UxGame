using System;
using System.Collections.Generic;

namespace Ux
{
    public readonly struct DialogData
    {
        public readonly int Id;
        public readonly string Title;
        public readonly string Content;
        public readonly DialogBtn Btn1;
        public readonly DialogBtn Btn2;        
        public readonly DialogCheckbox CheckBoxData;
        public readonly object CustomData;

        public bool HasCustom => CustomData != null;

        internal readonly Action<UIDialog> ShowCallback;
        internal readonly Action<UIDialog> HideCallback;
        internal readonly Func<string, bool> CheckShownCallback;

        private DialogData(
            int id,
            string title, string content,
            DialogBtn btn1,DialogBtn btn2, DialogCheckbox checkBoxData,
            object customData,
            Action<UIDialog> showCallback,
            Action<UIDialog> hideCallback,
            Func<string, bool> checkShownCallback)
        {
            Id = id;
            Title = title;
            Content = content;
            Btn1 = btn1;
            Btn2 = btn2;
            CheckBoxData = checkBoxData;
            CustomData = customData;
            ShowCallback = showCallback;
            HideCallback = hideCallback;
            CheckShownCallback = checkShownCallback;
        }

        /// <summary>
        /// 设置标题
        /// </summary>
        public DialogData WithTitle(string value) => new DialogData(
            Id, value, Content,
            Btn1, Btn2, CheckBoxData,
            CustomData,
            ShowCallback, HideCallback, CheckShownCallback);

        /// <summary>
        /// 设置内容
        /// </summary>
        public DialogData WithContent(string value) => new DialogData(
            Id, Title, value,
            Btn1, Btn2, CheckBoxData,
            CustomData,
            ShowCallback, HideCallback, CheckShownCallback);

        /// <summary>
        /// 设置按钮1
        /// </summary>
        public DialogData WithBtn1(string title,Action callback = null) => new DialogData(
            Id, Title, Content,
            new DialogBtn(title, callback), Btn2, CheckBoxData,
            CustomData,
            ShowCallback, HideCallback, CheckShownCallback);

        /// <summary>
        /// 设置按钮2
        /// </summary>
        public DialogData WithBtn2(string title,Action callback = null) => new DialogData(
            Id, Title, Content,
            Btn1, new DialogBtn(title, callback), CheckBoxData,
            CustomData,
            ShowCallback, HideCallback, CheckShownCallback);
        /// <summary>
        /// 设置"不再提示"复选框
        /// </summary>
        public DialogData WithCheckBox(string tag, string desc = "不再提示", Action<string, bool> callback = null) => new DialogData(
            Id, Title, Content,
            Btn1,Btn2,new DialogCheckbox(tag, desc, callback),
            CustomData,
            ShowCallback, HideCallback, CheckShownCallback);

        /// <summary>
        /// 设置自定义数据
        /// </summary>
        public DialogData WithCustom(object data) => new DialogData(
            Id, Title, Content,
            Btn1,Btn2, CheckBoxData,
            data,
            ShowCallback, HideCallback, CheckShownCallback);

        /// <summary>
        /// 检查复选框是否已勾选
        /// </summary>
        public bool IsCheckBoxChecked => CheckBoxData.HasCheckBox && CheckShownCallback?.Invoke(CheckBoxData.CheckBoxTag) == true;

        /// <summary>
        /// 显示对话框
        /// </summary>
        public void Show()
        {
            if (IsCheckBoxChecked)
            {
                Btn1.BtnCallback?.Invoke();
                return;
            }

            UIMgr.Ins.Show(Id, IUIParam.Create(this));
        }

        /// <summary>
        /// 创建基础实例（内部使用）
        /// </summary>
        internal static DialogData Create(
            int id,
            Action<UIDialog> showCallback,
            Action<UIDialog> hideCallback,
            Func<string, bool> checkShownCallback) => new DialogData(
            id, null, null,
            default, default, default,
            null,
            showCallback, hideCallback, checkShownCallback);
    }
    public readonly struct DialogCheckbox
    {
        public readonly string CheckBoxTag;
        public readonly string CheckBoxDesc;
        public readonly Action<string, bool> CheckBoxCallback;
        public bool HasCheckBox => !string.IsNullOrEmpty(CheckBoxTag);

        public DialogCheckbox(string tag, string desc, Action<string, bool> callback)
        {
            CheckBoxTag = tag;
            CheckBoxDesc = desc;
            CheckBoxCallback = callback;
        }
    }
    public readonly struct DialogBtn
    {
        public readonly string BtnTitle;
        public readonly Action BtnCallback;
        public bool HasBtn => BtnCallback != null;
        public DialogBtn(string title, Action callback)
        {
            BtnTitle = title;
            BtnCallback = callback;
        }
    }
    public class UIDialogFactory : UIBaseFactory<UIDialog>
    {
        private readonly HashSet<string> _shownTags = new HashSet<string>();

        /// <summary>
        /// 获取默认对话框
        /// </summary>
        public DialogData Get()
        {
            var id = GetDefaultID();
            if (id == 0)
            {
                Log.Error("没有指定对话框类型,请检查是否已初始化 SetDefaultType");
                return default;
            }
            return DialogData.Create(id, OnShow, OnHide, IsTagShown);
        }

        /// <summary>
        /// 获取指定类型对话框
        /// </summary>
        public DialogData Get<T>() where T : UIDialog
        {
            return DialogData.Create(GetUIID(typeof(T)), OnShow, OnHide, IsTagShown);
        }

        /// <summary>
        /// 标记tag已显示过
        /// </summary>
        public void PushTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                _shownTags.Add(tag);
            }
        }

        /// <summary>
        /// 检查tag是否已显示过
        /// </summary>
        public bool IsTagShown(string tag)
        {
            return _shownTags.Contains(tag);
        }

        /// <summary>
        /// 清除所有记住的tag
        /// </summary>
        public void ClearTags()
        {
            _shownTags.Clear();
        }
    }
}