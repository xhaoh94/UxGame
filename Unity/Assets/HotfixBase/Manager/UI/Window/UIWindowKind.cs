namespace Ux
{
    /// <summary>
    /// 窗口层（<see cref="UILayer.Window"/>）内的层叠级别。
    /// 跨层顺序由 Sorting Layer 保证，同一层内按本枚举分组。
    /// 枚举值即分组顺序：越大越靠上。
    /// </summary>
    public enum UIWindowTier
    {
        /// <summary>普通浮窗（默认）。</summary>
        Normal = 0,

        /// <summary>需要始终压在普通浮窗之上的窗口（聊天、系统提示）。</summary>
        AlwaysOnTop = 1,
    }

    /// <summary>
    /// 窗口的布局状态。
    /// 与 <see cref="UIState"/>（Show/ShowAnim/Hide/HideAnim 过渡状态机）完全独立：
    /// 最小化不是隐藏，不走 UIState，也不进缓存。
    /// </summary>
    public enum WindowState
    {
        Normal = 0,
        Maximized = 1,
        Minimized = 2,
    }
}
