#if UNITY_EDITOR
using System;

namespace Ux.Editor.Build.UI
{
    /// <summary>成员默认绑定的事件类型。</summary>
    [Flags]
    public enum UGUIMemberEvents
    {
        None = 0,
        Click = 1 << 0,
        DoubleClick = 1 << 1,
        LongPress = 1 << 2,
        ItemClick = 1 << 3,
    }
}
#endif
