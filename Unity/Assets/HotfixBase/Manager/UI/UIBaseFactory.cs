using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>
    /// UI 工厂基类：只维护「哪些实例正在显示」。
    ///
    /// 实例的创建/复用/回收全部由 UIMgr 按记录（record）管理：
    /// 类型与 ID 一一对应（<see cref="UIMgr.GetTypeId"/>），同一类型要并存多个实例时
    /// 由显示模式 <see cref="UIShowMode.NewInstance"/> 决定，不在这里做实例池。
    /// </summary>
    public abstract class UIBaseFactory<TUI> where TUI : IUI
    {
        /// <summary>当前正在显示的实例 ID（同一类型可能有多个）。</summary>
        protected readonly HashSet<int> _showed = new HashSet<int>();

        /// <summary>当UI显示时调用</summary>
        protected void OnShow(TUI ui)
        {
            _showed.Add(ui.ID);
        }

        /// <summary>当UI隐藏时调用</summary>
        protected void OnHide(TUI ui)
        {
            _showed.Remove(ui.ID);
        }

        /// <summary>实例销毁时由等待销毁队列回调；实例池已移除，这里保留空实现以兼容调用点。</summary>
        public void RemoveWaitDelById(int id)
        {
        }

        /// <summary>清空「正在显示」的记录。</summary>
        public void Clear()
        {
            _showed.Clear();
        }

        /// <summary>默认的UI类型</summary>
        protected Type _defaultType;

        /// <summary>设置默认的UI类型</summary>
        public void SetDefaultType<T>() where T : TUI
        {
            _defaultType = typeof(T);
        }
    }
}
