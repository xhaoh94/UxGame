namespace Ux
{
    /// <summary>
    /// UI 层级。数值越小越靠后，越大越靠前；<see cref="Root"/> 是特殊值，表示不上屏。
    ///
    /// 跨层渲染顺序由 Sorting Layer 保证，命名规则固定为 <c>"sl" + (int)layer</c>
    /// （<c>UIStageHandler</c> 按此推导，不要再写第二份字符串映射）。
    /// 新增层时只需要改这个枚举，然后跑一次
    /// <c>UxGame/工具/UI/初始化 UI SortingLayer</c> 补齐 Sorting Layer 资产。
    /// </summary>
    public enum UILayer
    {
        /// <summary>不上屏：子页面自己挂到父级容器下。</summary>
        Root = 0,

        /// <summary>底层：主界面背景、常驻氛围元素。</summary>
        Bottom = 1,

        /// <summary>页面层：主界面、功能页、Tab 页。</summary>
        View = 2,

        /// <summary>窗口层：可拖动/缩放的浮窗，层内按 UIWindowTier 再分组。</summary>
        Window = 3,

        /// <summary>普通弹窗/确认框：在窗口之上，不打断引导。</summary>
        Dialog = 4,

        /// <summary>新手引导：全屏遮罩、高亮、气泡。</summary>
        Guide = 5,

        /// <summary>系统级：公告、强制更新、断线重连（必须盖住引导）。</summary>
        System = 6,

        /// <summary>飘字、Toast、获得提示（要能浮在系统弹窗之上）。</summary>
        Tip = 7,

        /// <summary>Loading / 过场（必须盖住一切业务 UI）。</summary>
        Loading = 8,

        /// <summary>兜底最高层：调试面板、临时覆盖。</summary>
        Top = 9,
    }
}
