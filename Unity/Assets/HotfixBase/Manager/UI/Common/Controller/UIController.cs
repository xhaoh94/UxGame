using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// UGUI 版控制器，对应 FairyGUI 的 Controller：
    /// 维护一组页面和当前页，切换时通知所有绑定它的 UIControllerBinding。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Ux/UI/UI Controller")]
    public sealed class UIController : MonoBehaviour
    {
        [SerializeField] private ControllerPage[] _pages =
        {
            new ControllerPage { Name = "page0", Id = 0 },
        };

        [SerializeField] private int _selectedIndex;

        private readonly List<UIControllerBinding> _bindings = new List<UIControllerBinding>(4);

        /// <summary>页面切换事件：(控制器, 旧索引, 新索引)。</summary>
        public event Action<UIController, int, int> Changed;

        public ControllerPage[] Pages => _pages;
        public int PageCount => _pages?.Length ?? 0;

        public int SelectedIndex
        {
            get => PageCount > 0 ? Mathf.Clamp(_selectedIndex, 0, PageCount - 1) : 0;
            set => SetSelectedIndex(value);
        }

        public string SelectedPageName
        {
            get
            {
                if (PageCount == 0) return string.Empty;
                var index = SelectedIndex;
                return _pages[index].Name;
            }
        }

        public int SelectedPageId
        {
            get
            {
                if (PageCount == 0) return 0;
                return _pages[SelectedIndex].Id;
            }
        }

        private void OnValidate()
        {
            // 只纠正索引；绑定对象的实际写入延迟到 Inspector 绘制结束后执行。
            if (PageCount > 0)
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, PageCount - 1);
            else
                _selectedIndex = 0;

#if UNITY_EDITOR
            if (!isActiveAndEnabled) return;
            UnityEditor.EditorApplication.delayCall -= DelayedApplyAll;
            UnityEditor.EditorApplication.delayCall += DelayedApplyAll;
#endif
        }

#if UNITY_EDITOR
        private void DelayedApplyAll()
        {
            if (this == null) return;
            ApplyAll();
        }
#endif

        public void Register(UIControllerBinding binding)
        {
            if (binding == null) return;
            if (!_bindings.Contains(binding))
                _bindings.Add(binding);
            binding.MarkRegistered(this);
            // 注册时立刻对齐当前页，避免新加入的对象停留在默认外观。
            binding.ApplyPage(SelectedIndex);
        }

        public void Unregister(UIControllerBinding binding)
        {
            if (binding == null) return;
            _bindings.Remove(binding);
            binding.MarkUnregistered(this);
        }

        public int BindingCount => _bindings.Count;

        /// <summary>把当前页重新应用到所有绑定对象。</summary>
        public void ApplyAll()
        {
            for (int i = _bindings.Count - 1; i >= 0; i--)
            {
                var binding = _bindings[i];
                if (binding == null)
                {
                    _bindings.RemoveAt(i);
                    continue;
                }
                binding.ApplyPage(SelectedIndex);
            }
        }

        public void SetSelectedIndex(int index, bool force = false)
        {
            if (PageCount == 0) return;
            index = Mathf.Clamp(index, 0, PageCount - 1);
            if (!force && index == _selectedIndex) return;

            var old = _selectedIndex;
            _selectedIndex = index;
            ApplyAll();
            Changed?.Invoke(this, old, index);
        }

        public bool SetSelectedPage(string name)
        {
            var index = IndexOfPage(name);
            if (index < 0) return false;
            SetSelectedIndex(index);
            return true;
        }

        public int IndexOfPage(string name)
        {
            if (_pages == null || string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < _pages.Length; i++)
                if (string.Equals(_pages[i].Name, name, StringComparison.Ordinal))
                    return i;
            return -1;
        }

        public int IndexOfPageId(int id)
        {
            if (_pages == null) return -1;
            for (int i = 0; i < _pages.Length; i++)
                if (_pages[i].Id == id) return i;
            return -1;
        }

        public bool SetSelectedPageId(int id)
        {
            var index = IndexOfPageId(id);
            if (index < 0) return false;
            SetSelectedIndex(index);
            return true;
        }

        public void AddPage(string name = null, int id = 0)
        {
            var pages = _pages ?? Array.Empty<ControllerPage>();
            var next = new ControllerPage[pages.Length + 1];
            Array.Copy(pages, next, pages.Length);
            next[pages.Length] = new ControllerPage
            {
                Name = string.IsNullOrEmpty(name) ? $"page{pages.Length}" : name,
                Id = id != 0 ? id : pages.Length,
            };
            _pages = next;
        }

        public void RemovePage(int index)
        {
            if (_pages == null || index < 0 || index >= _pages.Length) return;
            var next = new ControllerPage[_pages.Length - 1];
            var cursor = 0;
            for (int i = 0; i < _pages.Length; i++)
                if (i != index) next[cursor++] = _pages[i];
            _pages = next;
            if (_selectedIndex >= _pages.Length)
                _selectedIndex = Mathf.Max(0, _pages.Length - 1);
        }
    }
}
