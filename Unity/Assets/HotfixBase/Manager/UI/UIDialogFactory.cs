using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Ux
{
    public class DialogBuilder 
    {
        private DialogData _data;
        private Action<UIDialog> _showCallback;
        private Action<UIDialog> _hideCallback;
        internal void Init(int id, Action<UIDialog> showCallback, Action<UIDialog> hideCallback)
        {
            _data = new DialogData(id, null, null, default, default, default,null);
            _showCallback = showCallback;
            _hideCallback = hideCallback;
        }
        // 链式配置（直接修改内部 struct，不用外部接收返回值）
        public DialogBuilder SetTitle(string value) { _data = _data.WithTitle(value); return this; }
        public DialogBuilder SetContent(string value) { _data = _data.WithContent(value); return this; }
        public DialogBuilder SetBtn1Title(string title) { _data = _data.WithBtn1Title(title); return this; }
        public DialogBuilder SetBtn1( Action callback = null) { _data = _data.WithBtn1(callback); return this; }
        public DialogBuilder SetBtn2Title(string title) { _data = _data.WithBtn2Title(title); return this; }
        public DialogBuilder SetBtn2(Action callback = null) { _data = _data.WithBtn2(callback); return this; }
        public DialogBuilder SetCheckBox(string tag, string desc = "本次登录不再提示", Action<string> callback = null) { _data = _data.WithCheckBox(tag, desc, callback); return this; }
        public DialogBuilder SetCustom(object customData){_data = _data.WithCustom(customData); return this; }
        public void Show()
        {
            try
            {
                _data.Show(_showCallback, _hideCallback);
            }
            finally
            {
                ReleaseInternal();
            }
        }
        void ReleaseInternal()
        {
            _data = default;
            _showCallback = null;
            _hideCallback = null;
            Pool.Push(this);
        }
    }

    public readonly struct DialogData
    {
        public readonly int Id;
        public readonly string Title;
        public readonly string Content;
        public readonly DialogBtn Btn1;
        public readonly DialogBtn Btn2;
        public readonly DialogCheckbox CheckBoxData;
        public readonly object CustomData;
        public DialogData(
            int id,
            string title, string content,
            DialogBtn btn1, DialogBtn btn2, DialogCheckbox checkBoxData,object customData)
        {
            Id = id;
            Title = title;
            Content = content;
            Btn1 = btn1;
            Btn2 = btn2;
            CheckBoxData = checkBoxData;
            CustomData = customData;
        }

        public DialogData WithTitle(string value) => new(Id, value, Content, Btn1, Btn2, CheckBoxData,CustomData);
        public DialogData WithContent(string value) => new(Id, Title, value, Btn1, Btn2, CheckBoxData,CustomData);
        public DialogData WithBtn1Title(string title) => new(Id, Title, Content, Btn1.SetTitle(title), Btn2, CheckBoxData,CustomData);
        public DialogData WithBtn1(Action callback = null) => new(Id, Title, Content, Btn1.SetCallback(callback), Btn2, CheckBoxData,CustomData);
        public DialogData WithBtn2Title(string title) => new(Id, Title, Content, Btn1, Btn2.SetTitle(title), CheckBoxData,CustomData);
        public DialogData WithBtn2(Action callback = null) => new(Id, Title, Content, Btn1, Btn2.SetCallback(callback), CheckBoxData,CustomData);
        public DialogData WithCheckBox(string tag, string desc, Action<string> callback) => new(Id, Title, Content, Btn1, Btn2, new DialogCheckbox(tag, desc, callback),CustomData);
        public DialogData WithCustom(object data) => new(Id, Title, Content, Btn1, Btn2, CheckBoxData,data);
        public bool IsCheckBoxChecked => CheckBoxData.Visible && UIMgr.Dialog.IsTagShown(CheckBoxData.CheckBoxTag) == true;

        public void Show(Action<UIDialog> showCallback, Action<UIDialog> hideCallback)
        {
            if (IsCheckBoxChecked)
            {
                Btn1.BtnCallback?.Invoke();
                return;
            }

            UIMgr.Ins.Create(Id).SetParam(IUIParam.Create(this, new DialogCallbackData(showCallback, hideCallback))).Show();
        }
    }

    public readonly struct DialogCheckbox
    {
        public readonly string CheckBoxTag;
        public readonly string CheckBoxDesc;
        public readonly Action<string> CheckBoxCallback;
        public bool Visible => !string.IsNullOrEmpty(CheckBoxTag);
        public DialogCheckbox(string tag, string desc, Action<string> callback) { CheckBoxTag = tag; CheckBoxDesc = desc; CheckBoxCallback = callback; }
    }

    public readonly struct DialogBtn
    {
        public readonly string BtnTitle;
        public readonly Action BtnCallback;
        public bool Visible => BtnCallback != null;
        public DialogBtn(string title, Action callback) { BtnTitle = title; BtnCallback = callback; }
        internal DialogBtn SetTitle(string title)
        {
            return new DialogBtn(title,BtnCallback);
        }
        internal DialogBtn SetCallback(Action callback)
        {
            return new DialogBtn(BtnTitle,callback);
        }
    }

    public readonly struct DialogCallbackData
    {
        public readonly Action<UIDialog> ShowCallback;
        public readonly Action<UIDialog> HideCallback;
        public DialogCallbackData(Action<UIDialog> showCallback, Action<UIDialog> hideCallback)
        {
            ShowCallback = showCallback;
            HideCallback = hideCallback;
        }
    }

    public class UIDialogFactory : UIBaseFactory<UIDialog>
    {
        private readonly HashSet<string> _shownTags = new HashSet<string>();

        public DialogBuilder Create()
        {
            var id = GetDefaultID();
            if (id == 0) { Log.Error("没有指定对话框类型,请检查是否已初始化 SetDefaultType"); return null; }
            var builder = Pool.Get<DialogBuilder>();
            builder.Init(id, OnShow, OnHide);
            return builder;
        }

        public DialogBuilder Create<T>() where T : UIDialog
        {
            var builder = Pool.Get<DialogBuilder>();
            builder.Init(GetUIID(typeof(T)), OnShow, OnHide);
            return builder;
        }

        public void PushTag(string tag) { if (!string.IsNullOrEmpty(tag)) _shownTags.Add(tag); }
        public bool IsTagShown(string tag) => _shownTags.Contains(tag);
        public void ClearTags() => _shownTags.Clear();
    }
}