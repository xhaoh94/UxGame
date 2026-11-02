using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class UIList : UIObject
    {
        private UIVirtualListComponent _virtualList;
        private readonly Dictionary<GameObject, IItemRenderer> _renderers = new();
        private readonly List<IUIParam> _datas = new();
        private Func<int, int> _templateIdFunc;
        /// <summary>模板 TypeId -> Renderer 类型（每个列表自己的映射）。</summary>
        private readonly Dictionary<int, Type> _templateRenderers = new();
        private readonly HashSet<int> _missingRendererWarned = new();
        private Action<IItemRenderer> _itemClickEvt;
        private bool _firstShow;
        private bool _bindingsConfigured;
        private bool _clickConfigured;

        public UIList(GameObject gameObject, UIObject parent)
        {
            Init(gameObject, parent);
            parent?.AddComponent(this);
        }

        protected override void OnInit()
        {
            _virtualList = GameObject.GetComponent<UIVirtualListComponent>() ?? CreateRuntimeList();
        }

        private UIVirtualListComponent CreateRuntimeList()
        {
            var root = GameObject.transform as RectTransform;
            if (root == null)
                return null;

            var scrollRect = GameObject.GetComponent<UnityEngine.UI.ScrollRect>() ?? GameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D));
            var viewport = (RectTransform)viewportObject.transform;
            viewport.SetParent(root, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;

            var contentObject = new GameObject("Content", typeof(RectTransform));
            var content = (RectTransform)contentObject.transform;
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            var template = new GameObject("ItemTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var templateRect = (RectTransform)template.transform;
            templateRect.SetParent(root, false);
            templateRect.sizeDelta = new Vector2(186f, 65f);
            template.SetActive(false);

            var textObject = new GameObject("txtTitle", typeof(RectTransform), typeof(CanvasRenderer));
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(templateRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            CreateTemplateText(textObject);

            var component = GameObject.AddComponent<UIVirtualListComponent>();
            component.ConfigureRuntime(scrollRect, template);
            return component;
        }

        /// <summary>
        /// 运行时模板文本：优先 TMP（需要默认字体资源），否则回退传统 Text。
        /// </summary>
        private static void CreateTemplateText(GameObject textObject)
        {
            var fontAsset = TMPro.TMP_Settings.defaultFontAsset;
            if (fontAsset != null)
            {
                var tmp = textObject.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.font = fontAsset;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.fontSize = 24;
                tmp.color = Color.white;
                return;
            }

            var text = textObject.AddComponent<UnityEngine.UI.Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 24;
            text.color = Color.white;
        }

        public UIVirtualListComponent VirtualList => _virtualList;
        public int SelectedIndex { get; private set; } = -1;

        public void SetSelectedIndex(int index, bool scrollToView = true)
        {
            SelectedIndex = index;
            if (_virtualList != null && index >= 0 && scrollToView)
                _virtualList.ScrollToView(index);
        }

        /// <summary>
        /// 按索引返回模板 TypeId（就是 Prefab 里 ItemTemplates 的那个编号）。
        /// Renderer 类型由 RegisterItemType 注册的映射决定。
        /// </summary>
        public void SetItemProvider(Func<int, int> templateIdProvider)
        {
            _templateIdFunc = templateIdProvider;

            if (_virtualList != null)
            {
                _virtualList.ItemTypeProvider = _templateIdFunc == null
                    ? null
                    : (index, data) => _templateIdFunc(index);
            }

            ConfigureBindings();
        }

        /// <summary>
        /// 注册模板 TypeId 对应的 Renderer 类型。
        /// 映射只属于当前列表，不影响其他列表。
        /// </summary>
        public void SetItemRenderer<T>(int templateTypeId = 0) where T : ItemRenderer
        {
            _templateRenderers[templateTypeId] = typeof(T);
        }

        private Type ResolveItemType(int index)
        {
            // 没有 provider 时用默认模板 0 的映射（对应 Prefab 的 Default Item Type）。
            var templateId = _templateIdFunc != null ? _templateIdFunc(index) : 0;
            if (_templateRenderers.TryGetValue(templateId, out var mapped))
                return mapped;

            if (_missingRendererWarned.Add(templateId))
            {
                Log.Error("列表模板 TypeId[{0}] 未注册 Renderer，请调用 SetItemRenderer。", templateId);
            }
            return null;
        }

        public void AddItemClick(Action<IItemRenderer> callback)
        {
            _itemClickEvt = callback;
            ConfigureBindings();
            if (_virtualList != null && !_clickConfigured)
            {
                _virtualList.ItemClicked += OnItemClicked;
                _clickConfigured = true;
            }
        }

        private void ConfigureBindings()
        {
            if (_virtualList == null || _bindingsConfigured)
                return;
            _virtualList.ItemRenderer = OnItemRenderer;
            _virtualList.ItemRecycled += OnItemRecycled;
            _bindingsConfigured = true;
        }

        private void OnItemRenderer(int index, object data, GameObject item)
        {
            var itemType = ResolveItemType(index);
            if (item == null || itemType == null)
                return;

            if (_renderers.TryGetValue(item, out var renderer) && renderer.GetType() != itemType)
            {
                renderer.Release();
                _renderers.Remove(item);
                renderer = null;
            }

            if (renderer == null)
            {
                renderer = Pool.Get<IItemRenderer>(itemType);
                renderer.Init(item, this);
                _renderers[item] = renderer;
            }

            renderer.Set(index, data as IUIParam ?? IUIParam.Create(data), _firstShow);
        }

        private void OnItemRecycled(int index, object data, GameObject item)
        {
            if (item != null && _renderers.TryGetValue(item, out var renderer))
            {
                renderer.Release();
                _renderers.Remove(item);
            }
        }

        private void OnItemClicked(int index, object data)
        {
            foreach (var pair in _renderers)
            {
                if (pair.Value.Index == index)
                {
                    _itemClickEvt?.Invoke(pair.Value);
                    return;
                }
            }
        }

        public int IndexOf(IUIParam data) => _datas.IndexOf(data);

        public int IndexOf<T>(T data, UIParamType type = UIParamType.A)
        {
            for (var i = 0; i < _datas.Count; i++)
                if (_datas[i].TryGet(out T value, type) && EqualityComparer<T>.Default.Equals(data, value))
                    return i;
            return -1;
        }

        public T GetData<T>(int index, UIParamType type = UIParamType.A)
        {
            return index >= 0 && index < _datas.Count && _datas[index].TryGet(out T value, type) ? value : default;
        }

        public IUIParam GetData(int index) => index >= 0 && index < _datas.Count ? _datas[index] : null;
        public List<IUIParam> GetDatas() => _datas;

        public void SetDatas<T>(IEnumerable<T> datas, int scrollToIndex = -1)
        {
            _datas.Clear();
            if (datas != null)
                foreach (var data in datas) _datas.Add(IUIParam.Create(data));
            _virtualList?.SetDatas(_datas, scrollToIndex);
            SelectedIndex = -1;
        }

        public void SetDatas(IEnumerable<IUIParam> datas, int scrollToIndex = -1)
        {
            _datas.Clear();
            if (datas != null) _datas.AddRange(datas);
            _virtualList?.SetDatas(_datas, scrollToIndex);
            SelectedIndex = -1;
        }

        public void RefreshVirtualList() => _virtualList?.Refresh();

        protected override void OnShow()
        {
            base.OnShow();
            _firstShow = true;
        }

        protected override void OnHide()
        {
            base.OnHide();
            foreach (var renderer in _renderers.Values) renderer.Release();
            _renderers.Clear();
            _datas.Clear();
            _itemClickEvt = null;
            SelectedIndex = -1;
        }

        protected override void OnDispose()
        {
            if (_virtualList != null)
            {
                _virtualList.ItemRenderer = null;
                _virtualList.ItemRecycled -= OnItemRecycled;
                if (_clickConfigured) _virtualList.ItemClicked -= OnItemClicked;
                _virtualList.Release();
            }
            foreach (var renderer in _renderers.Values) renderer.Release();
            _renderers.Clear();
            _templateIdFunc = null;
            _templateRenderers.Clear();
            _missingRendererWarned.Clear();
        }
    }
}
