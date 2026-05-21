using System.Collections.Generic;

namespace Ux
{
    internal class UIFocusHandler
    {
        private readonly IUIFocusHandlerCallback _callback;
        private readonly List<int> _focusStack = new();
        private int _focusedId;

        public UIFocusHandler(IUIFocusHandlerCallback callback)
        {
            _callback = callback;
        }

        public int FocusedId => _focusedId;

        public IUI GetFocusedUI()
        {
            return _focusedId == 0 ? null : _callback.GetShownUI(_focusedId);
        }

        public T GetFocusedUI<T>() where T : class, IUI
        {
            return GetFocusedUI() as T;
        }

        public bool IsFocused(int id)
        {
            return _focusedId == id;
        }

        public bool IsFocused<T>() where T : IUI
        {
            var ui = GetFocusedUI();
            return ui != null && ui is T;
        }

        public void OnShowed(IUI ui)
        {
            if (ui == null || ui.FocusMode == UIFocusMode.None)
            {
                return;
            }

            PushFocus(ui.ID);
            SetFocusInternal(ui.ID);
        }

        public void OnHidden(IUI ui)
        {
            if (ui == null)
            {
                return;
            }

            RemoveFromStack(ui.ID);
            if (_focusedId == ui.ID)
            {
                FallbackFocus();
            }
        }

        public void OnDisposed(IUI ui)
        {
            if (ui == null)
            {
                return;
            }

            RemoveFromStack(ui.ID);
            if (_focusedId == ui.ID)
            {
                FallbackFocus();
            }
        }

        public bool Focus(int id)
        {
            if (id == 0)
            {
                return false;
            }

            if (!_callback.IsVisible(id))
            {
                return false;
            }

            var ui = _callback.GetShownUI(id);
            if (ui == null || !ui.CanFocus)
            {
                return false;
            }

            PushFocus(id);
            SetFocusInternal(id);
            return true;
        }

        public void Clear()
        {
            if (_focusedId != 0)
            {
                var oldUi = _callback.GetShownUI(_focusedId);
                oldUi?.NotifyFocusExit();
                _focusedId = 0;
            }
            _focusStack.Clear();
        }

        private void PushFocus(int id)
        {
            RemoveFromStack(id);
            _focusStack.Add(id);
        }

        private void RemoveFromStack(int id)
        {
            for (int i = _focusStack.Count - 1; i >= 0; i--)
            {
                if (_focusStack[i] == id)
                {
                    _focusStack.RemoveAt(i);
                    return;
                }
            }
        }

        private void SetFocusInternal(int id)
        {
            if (_focusedId == id)
            {
                return;
            }

            var oldUi = _callback.GetShownUI(_focusedId);
            oldUi?.NotifyFocusExit();

            _focusedId = id;

            var newUi = _callback.GetShownUI(_focusedId);
            newUi?.NotifyFocusEnter();
        }

        private void FallbackFocus()
        {
            for (int i = _focusStack.Count - 1; i >= 0; i--)
            {
                var id = _focusStack[i];
                var ui = _callback.GetShownUI(id);
                if (ui != null && ui.CanFocus)
                {
                    SetFocusInternal(id);
                    return;
                }
            }

            _focusedId = 0;
        }
    }
}
