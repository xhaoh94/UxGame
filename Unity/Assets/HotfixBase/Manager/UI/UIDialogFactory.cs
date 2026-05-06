using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Ux
{
    /// <summary>
    /// 对话框构建器类，提供链式API来配置和显示对话框
    /// 使用对象池管理，避免频繁创建和销毁
    /// </summary>
    public class DialogBuilder
    {
        private DialogData _data;                  // 对话框数据
        private Action<UIDialog> _showCallback;    // 显示回调函数
        private Action<UIDialog> _hideCallback;    // 隐藏回调函数
        
        /// <summary>
        /// 初始化对话框构建器
        /// </summary>
        /// <param name="id">对话框ID</param>
        /// <param name="showCallback">显示回调函数</param>
        /// <param name="hideCallback">隐藏回调函数</param>
        internal void Init(int id, Action<UIDialog> showCallback, Action<UIDialog> hideCallback)
        {
            _data = new DialogData(id, null, null, new DialogBtn("确定"), new DialogBtn("取消"), default, null);
            _showCallback = showCallback;
            _hideCallback = hideCallback;
        }
        
        // 链式配置方法（直接修改内部结构体，不需要外部接收返回值）
        
        /// <summary>
        /// 设置对话框标题
        /// </summary>
        /// <param name="value">标题文本</param>
        /// <returns>当前构建器实例，支持链式调用</returns>
        public DialogBuilder SetTitle(string value) { _data = _data.WithTitle(value); return this; }
        
        /// <summary>
        /// 设置对话框内容
        /// </summary>
        /// <param name="value">内容文本</param>
        /// <returns>当前构建器实例，支持链式调用</returns>
        public DialogBuilder SetContent(string value) { _data = _data.WithContent(value); return this; }
        
        /// <summary>
        /// 设置第一个按钮的标题
        /// </summary>
        /// <param name="title">按钮标题</param>
        /// <returns>当前构建器实例，支持链式调用</returns>
        public DialogBuilder SetBtn1Title(string title) { _data = _data.WithBtn1Title(title); return this; }
        
        /// <summary>
        /// 设置第一个按钮的点击回调
        /// </summary>
        /// <param name="callback">点击回调函数，可选参数</param>
        /// <returns>当前构建器实例，支持链式调用</returns>
        public DialogBuilder SetBtn1(Action callback = null) { _data = _data.WithBtn1(callback); return this; }
        
        /// <summary>
        /// 设置第一个按钮的可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        /// <returns>当前构建器实例，支持链式调用</returns>
        public DialogBuilder SetBtn1Visibe(bool visible) { _data = _data.WithBtn1Visibe(visible); return this; }
        
        /// <summary>
        /// 设置第二个按钮的标题
        /// </summary>
        /// <param name="title">按钮标题</param>
        /// <returns>当前构建器实例，支持链式调用</returns>
        public DialogBuilder SetBtn2Title(string title) { _data = _data.WithBtn2Title(title); return this; }
        
        /// <summary>
        /// 设置第二个按钮的点击回调
        /// </summary>
        /// <param name="callback">点击回调函数，可选参数</param>
        /// <returns>当前构建器实例，支持链式调用</returns>
        public DialogBuilder SetBtn2(Action callback = null) { _data = _data.WithBtn2(callback); return this; }
        
        /// <summary>
        /// 设置第二个按钮的可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        /// <returns>当前构建器实例，支持链式调用</returns>
        public DialogBuilder SetBtn2Visibe(bool visible) { _data = _data.WithBtn2Visibe(visible); return this; }
        
        /// <summary>
        /// 设置复选框选项
        /// </summary>
        /// <param name="tag">复选框标签，用于标识是否已勾选</param>
        /// <param name="desc">复选框描述文本，默认为"本次登录不再提示"</param>
        /// <param name="callback">复选框状态变化回调函数，可选参数</param>
        /// <returns>当前构建器实例，支持链式调用</returns>
        public DialogBuilder SetCheckBox(string tag, string desc = "本次登录不再提示", Action<string> callback = null) { _data = _data.WithCheckBox(tag, desc, callback); return this; }
        
        /// <summary>
        /// 设置自定义数据
        /// </summary>
        /// <param name="customData">自定义数据对象</param>
        /// <returns>当前构建器实例，支持链式调用</returns>
        public DialogBuilder SetCustom(object customData) { _data = _data.WithCustom(customData); return this; }
        
        /// <summary>
        /// 显示对话框
        /// 注意：调用此方法后，构建器会被回收，不能再使用
        /// </summary>
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
        
        /// <summary>
        /// 内部释放方法，将构建器回收到对象池
        /// </summary>
        void ReleaseInternal()
        {
            _data = default;
            _showCallback = null;
            _hideCallback = null;
            Pool.Push(this);
        }
    }

    /// <summary>
    /// 对话框数据结构体，包含对话框的所有配置信息
    /// 使用只读结构体确保数据不可变性
    /// </summary>
    public readonly struct DialogData
    {
        public readonly int Id;                     // 对话框ID
        public readonly string Title;               // 对话框标题
        public readonly string Content;             // 对话框内容
        public readonly DialogBtn Btn1;             // 第一个按钮
        public readonly DialogBtn Btn2;             // 第二个按钮
        public readonly DialogCheckbox CheckBoxData; // 复选框数据
        public readonly object CustomData;          // 自定义数据
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">对话框ID</param>
        /// <param name="title">对话框标题</param>
        /// <param name="content">对话框内容</param>
        /// <param name="btn1">第一个按钮</param>
        /// <param name="btn2">第二个按钮</param>
        /// <param name="checkBoxData">复选框数据</param>
        /// <param name="customData">自定义数据</param>
        public DialogData(
            int id,
            string title, string content,
            DialogBtn btn1, DialogBtn btn2, DialogCheckbox checkBoxData, object customData)
        {
            Id = id;
            Title = title;
            Content = content;
            Btn1 = btn1;
            Btn2 = btn2;
            CheckBoxData = checkBoxData;
            CustomData = customData;
        }

        // 以下方法用于创建新的DialogData实例，实现不可变数据的更新
        
        /// <summary>
        /// 创建新的DialogData实例，修改标题
        /// </summary>
        public DialogData WithTitle(string value) => new(Id, value, Content, Btn1, Btn2, CheckBoxData, CustomData);
        
        /// <summary>
        /// 创建新的DialogData实例，修改内容
        /// </summary>
        public DialogData WithContent(string value) => new(Id, Title, value, Btn1, Btn2, CheckBoxData, CustomData);
        
        /// <summary>
        /// 创建新的DialogData实例，修改第一个按钮的标题
        /// </summary>
        public DialogData WithBtn1Title(string title) => new(Id, Title, Content, Btn1.WithTitle(title), Btn2, CheckBoxData, CustomData);
        
        /// <summary>
        /// 创建新的DialogData实例，修改第一个按钮的回调
        /// </summary>
        public DialogData WithBtn1(Action callback = null) => new(Id, Title, Content, Btn1.WithCallback(callback), Btn2, CheckBoxData, CustomData);
        
        /// <summary>
        /// 创建新的DialogData实例，修改第一个按钮的可见性
        /// </summary>
        public DialogData WithBtn1Visibe(bool visible) => new(Id, Title, Content, Btn1.WithVisibe(visible), Btn2, CheckBoxData, CustomData);
        
        /// <summary>
        /// 创建新的DialogData实例，修改第二个按钮的标题
        /// </summary>
        public DialogData WithBtn2Title(string title) => new(Id, Title, Content, Btn1, Btn2.WithTitle(title), CheckBoxData, CustomData);
        
        /// <summary>
        /// 创建新的DialogData实例，修改第二个按钮的回调
        /// </summary>
        public DialogData WithBtn2(Action callback = null) => new(Id, Title, Content, Btn1, Btn2.WithCallback(callback), CheckBoxData, CustomData);
        
        /// <summary>
        /// 创建新的DialogData实例，修改第二个按钮的可见性
        /// </summary>
        public DialogData WithBtn2Visibe(bool visible) => new(Id, Title, Content, Btn1, Btn2.WithVisibe(visible), CheckBoxData, CustomData);
        
        /// <summary>
        /// 创建新的DialogData实例，修改复选框数据
        /// </summary>
        public DialogData WithCheckBox(string tag, string desc, Action<string> callback) => new(Id, Title, Content, Btn1, Btn2, new DialogCheckbox(tag, desc, callback), CustomData);
        
        /// <summary>
        /// 创建新的DialogData实例，修改自定义数据
        /// </summary>
        public DialogData WithCustom(object data) => new(Id, Title, Content, Btn1, Btn2, CheckBoxData, data);
        
        /// <summary>
        /// 检查复选框是否已被勾选
        /// 如果复选框可见且标签已显示，则认为已被勾选
        /// </summary>
        public bool IsCheckBoxChecked => CheckBoxData.Visible && UIMgr.Dialog.IsTagShown(CheckBoxData.CheckBoxTag) == true;

        /// <summary>
        /// 显示对话框
        /// 如果复选框已被勾选，直接执行第一个按钮的回调
        /// 否则创建并显示对话框UI
        /// </summary>
        /// <param name="showCallback">显示回调函数</param>
        /// <param name="hideCallback">隐藏回调函数</param>
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

    /// <summary>
    /// 复选框数据结构体
    /// </summary>
    public readonly struct DialogCheckbox
    {
        public readonly string CheckBoxTag;         // 复选框标签，用于标识是否已勾选
        public readonly string CheckBoxDesc;        // 复选框描述文本
        public readonly Action<string> CheckBoxCallback; // 复选框状态变化回调
        
        /// <summary>
        /// 复选框是否可见（标签不为空则可见）
        /// </summary>
        public bool Visible => !string.IsNullOrEmpty(CheckBoxTag);
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tag">复选框标签</param>
        /// <param name="desc">复选框描述</param>
        /// <param name="callback">复选框回调</param>
        public DialogCheckbox(string tag, string desc, Action<string> callback) { CheckBoxTag = tag; CheckBoxDesc = desc; CheckBoxCallback = callback; }
    }

    /// <summary>
    /// 对话框按钮数据结构体
    /// </summary>
    public readonly struct DialogBtn
    {
        public readonly string BtnTitle;     // 按钮标题
        public readonly Action BtnCallback;  // 按钮点击回调函数
        public readonly bool Visible;        // 按钮是否可见
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="title">按钮标题</param>
        /// <param name="callback">按钮点击回调函数，可选参数</param>
        /// <param name="visible">按钮是否可见，默认为true</param>
        public DialogBtn(string title, Action callback = null, bool visible = true) { BtnTitle = title; BtnCallback = callback; Visible = visible; }
        
        /// <summary>
        /// 创建新的DialogBtn实例，修改标题
        /// </summary>
        internal DialogBtn WithTitle(string title)
        {
            return new DialogBtn(title, BtnCallback, Visible);
        }
        
        /// <summary>
        /// 创建新的DialogBtn实例，修改回调函数
        /// </summary>
        internal DialogBtn WithCallback(Action callback)
        {
            return new DialogBtn(BtnTitle, callback, Visible);
        }
        
        /// <summary>
        /// 创建新的DialogBtn实例，修改可见性
        /// </summary>
        internal DialogBtn WithVisibe(bool visible)
        {
            return new DialogBtn(BtnTitle, BtnCallback, visible);
        }
    }

    /// <summary>
    /// 对话框回调数据结构体
    /// </summary>
    public readonly struct DialogCallbackData
    {
        public readonly Action<UIDialog> ShowCallback;  // 显示回调函数
        public readonly Action<UIDialog> HideCallback;  // 隐藏回调函数
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="showCallback">显示回调函数</param>
        /// <param name="hideCallback">隐藏回调函数</param>
        public DialogCallbackData(Action<UIDialog> showCallback, Action<UIDialog> hideCallback)
        {
            ShowCallback = showCallback;
            HideCallback = hideCallback;
        }
    }

    /// <summary>
    /// 对话框工厂类，继承自UIBaseFactory，专门用于创建和管理对话框
    /// </summary>
    public class UIDialogFactory : UIBaseFactory<UIDialog>
    {
        /// <summary>
        /// 已显示标签集合，用于记录哪些复选框标签已被勾选过
        /// </summary>
        private readonly HashSet<string> _shownTags = new HashSet<string>();

        /// <summary>
        /// 创建默认类型的对话框构建器
        /// 注意：需要先调用SetDefaultType设置默认类型
        /// </summary>
        /// <returns>对话框构建器实例，如果未设置默认类型则返回null</returns>
        public DialogBuilder Create()
        {
            var id = GetDefaultID();
            if (id == 0) { Log.Error("没有指定对话框类型,请检查是否已初始化 SetDefaultType"); return null; }
            var builder = Pool.Get<DialogBuilder>();
            builder.Init(id, OnShow, OnHide);
            return builder;
        }

        /// <summary>
        /// 创建指定类型的对话框构建器
        /// </summary>
        /// <typeparam name="T">对话框类型，必须是UIDialog的子类</typeparam>
        /// <returns>对话框构建器实例</returns>
        public DialogBuilder Create<T>() where T : UIDialog
        {
            var builder = Pool.Get<DialogBuilder>();
            builder.Init(GetUIID(typeof(T)), OnShow, OnHide);
            return builder;
        }

        /// <summary>
        /// 添加已显示的标签
        /// 当用户勾选了"不再提示"复选框时调用，避免重复显示相同对话框
        /// </summary>
        /// <param name="tag">标签字符串</param>
        public void PushTag(string tag) { if (!string.IsNullOrEmpty(tag)) _shownTags.Add(tag); }
        
        /// <summary>
        /// 检查标签是否已显示过
        /// </summary>
        /// <param name="tag">要检查的标签</param>
        /// <returns>如果标签已显示过返回true，否则返回false</returns>
        public bool IsTagShown(string tag) => _shownTags.Contains(tag);
        
        /// <summary>
        /// 清空所有已显示的标签
        /// 通常在用户登出或需要重置时调用
        /// </summary>
        public void ClearTags() => _shownTags.Clear();
    }
}