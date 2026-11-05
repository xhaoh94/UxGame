using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 窗口系统的业务门面 + 宿主访问实现。
    /// 业务只通过这里的方法/属性操作窗口，不直接接触 <see cref="UIWindowService"/>。
    /// </summary>
    public partial class UIMgr
    {
        private readonly List<IWindowObserver> _windowObservers = new List<IWindowObserver>();

        /// <summary>全局默认拖动范围（通常是 UIRoot 下的安全区）。</summary>
        public RectTransform WindowDefaultDragArea
        {
            get => _windowService.DefaultDragArea;
            set => _windowService.DefaultDragArea = value;
        }

        /// <summary>同时存在的窗口上限。默认不限。</summary>
        public int MaxWindows
        {
            get => _windowService.MaxWindows;
            set => _windowService.MaxWindows = value;
        }

        /// <summary>超限是否自动淘汰最久未激活的窗口。默认关闭。</summary>
        public bool EnableWindowLruEviction
        {
            get => _windowService.EnableLruEviction;
            set => _windowService.EnableLruEviction = value;
        }

        public void AddWindowObserver(IWindowObserver observer)
        {
            if (observer != null && !_windowObservers.Contains(observer))
                _windowObservers.Add(observer);
        }

        public void RemoveWindowObserver(IWindowObserver observer)
        {
            if (observer != null)
                _windowObservers.Remove(observer);
        }

        private void NotifyWindowObservers(Action<IWindowObserver> notify)
        {
            for (int i = _windowObservers.Count - 1; i >= 0; i--)
            {
                var observer = _windowObservers[i];
                if (observer == null)
                {
                    _windowObservers.RemoveAt(i);
                    continue;
                }

                notify(observer);
            }
        }

        // —— IWindowHostAccess：窗口服务对内部处理器的唯一入口 ——

        void IWindowHostAccess.Promote(int instanceId) => _stageHandler.Promote(instanceId);

        bool IWindowHostAccess.IsLastInTier(int instanceId) => _stageHandler.IsLastInTier(instanceId);

        void IWindowHostAccess.SendToBack(int instanceId) => _stageHandler.SendToBack(instanceId);

        void IWindowHostAccess.SetWindowTier(int instanceId, UIWindowTier tier)
            => _stageHandler.SetWindowTier(instanceId, tier);

        bool IWindowHostAccess.Focus(int instanceId) => _focusHandler.Focus(instanceId);

        RectTransform IWindowHostAccess.GetWindowLayer() => GetUILayer(UILayer.Window);

        bool IWindowHostAccess.TryGetWindow(int instanceId, out UIWindow window)
        {
            window = GetUI(instanceId) as UIWindow;
            return window != null;
        }
    }
}
