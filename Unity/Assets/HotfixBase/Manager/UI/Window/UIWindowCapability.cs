using System;

namespace Ux
{
    /// <summary>
    /// 窗口的交互能力声明。由 <see cref="UIWindow"/> override 决定，
    /// <see cref="UIWindowFrame"/>（框架预制）或退化路径据此装配交互。
    ///
    /// 这是**编译期开关**（继承覆盖），不是运行时玩家设置；
    /// 运行时可切换的能力后续通过 Frame.SetEnabled 之类的方法提供。
    /// </summary>
    [Flags]
    public enum UIWindowCapability
    {
        None = 0,

        /// <summary>按住标题栏 / 主体拖动窗口。</summary>
        Drag = 1 << 0,

        /// <summary>松手时贴父容器边缘。依赖 <see cref="Drag"/>。</summary>
        Snap = 1 << 1,

        /// <summary>拖四边/四角改尺寸。</summary>
        Resize = 1 << 2,

        /// <summary>铺满拖拽范围（DragArea 或父容器）。</summary>
        Maximize = 1 << 3,

        /// <summary>收起为不可见（不隐藏、不进缓存），靠 Restore/任务栏/快捷键恢复。</summary>
        Minimize = 1 << 4,
    }
}
