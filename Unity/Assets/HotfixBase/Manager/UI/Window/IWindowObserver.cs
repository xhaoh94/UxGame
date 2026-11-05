namespace Ux
{
    /// <summary>
    /// 窗口生命周期观察者。业务用它做任务栏、窗口列表、切换器等 UI，
    /// 不需要（也不应该）直接访问 <see cref="UIMgr"/> 内部的窗口服务。
    ///
    /// 注册方式：
    /// <code>
    /// UIMgr.Ins.AddWindowObserver(this);
    /// UIMgr.Ins.RemoveWindowObserver(this);
    /// </code>
    /// </summary>
    public interface IWindowObserver
    {
        /// <summary>窗口显示（上屏）。隐藏后再显示会再发一次。</summary>
        void OnWindowOpened(UIWindow window);

        /// <summary>窗口隐藏（下屏）。实例还在缓存里，但业务应把它当作已关闭。</summary>
        void OnWindowClosed(UIWindow window);

        /// <summary>窗口被激活（置顶/焦点变化）。</summary>
        void OnWindowActivated(UIWindow window);
    }
}
