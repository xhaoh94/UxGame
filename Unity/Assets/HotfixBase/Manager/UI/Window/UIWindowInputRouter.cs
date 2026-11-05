using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ux
{
    /// <summary>
    /// 窗口快捷键（Input System）：ESC 关最上层窗口、Alt+Tab 在窗口间切换。
    ///
    /// 默认挂在 [UIRoot] 上，但两个开关默认关闭，避免和业务自己的 ESC/键位冲突。
    /// 需要时在启动流程里打开：
    /// <code>
    /// UIMgr.Ins.GetComponent&lt;UIWindowInputRouter&gt;().EnableEscapeClose = true;
    /// </code>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIWindowInputRouter : MonoBehaviour
    {
        /// <summary>ESC 关闭最上层窗口。默认关闭。</summary>
        public bool EnableEscapeClose = false;

        /// <summary>Alt+Tab 切换窗口。默认关闭。</summary>
        public bool EnableCycle = false;

        private readonly List<UIWindow> _buffer = new List<UIWindow>();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (EnableEscapeClose && keyboard.escapeKey.wasPressedThisFrame)
                CloseTopmost();

            if (EnableCycle && keyboard.tabKey.wasPressedThisFrame &&
                (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed))
                CycleWindow();
        }

        private void CloseTopmost()
        {
            var top = UIMgr.Ins.Windows.GetTopmost();
            if (top == null)
                return;

            top.Hide();
        }

        private void CycleWindow()
        {
            UIMgr.Ins.Windows.FillWindows(_buffer);
            if (_buffer.Count < 2)
                return;

            var top = UIMgr.Ins.Windows.GetTopmost();
            var index = top != null ? _buffer.IndexOf(top) : -1;
            var next = _buffer[(index + 1) % _buffer.Count];

            if (!next.Visible)
            {
                UIMgr.Ins.Request(next.TypeId).Show();
            }
            else
            {
                // 最小化窗口 Visible 仍为 true（只是 alpha 0 / Canvas 关），
                // 只 BringToFront 切过去等于没切，必须先还原。
                if (next.LayoutState == WindowState.Minimized)
                    next.Restore();

                next.BringToFront();
            }
        }
    }
}
