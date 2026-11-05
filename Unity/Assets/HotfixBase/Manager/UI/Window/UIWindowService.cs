using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 服务层触达 <see cref="UIMgr"/> 内部处理器的回调面。
    /// 统一走 UIMgr 的 internal 方法（不再用 public 回调接口），
    /// 由 UIMgr 显式实现，服务层只持有接口，不直接碰 private 字段。
    /// </summary>
    internal interface IWindowHostAccess
    {
        /// <summary>把窗口提到本 Tier 末尾（内部会触发层内重排）。</summary>
        void Promote(int instanceId);

        /// <summary>窗口是否已在本 Tier 末尾（再 Promote 不会改变顺序）。</summary>
        bool IsLastInTier(int instanceId);

        /// <summary>把窗口降到本 Tier 开头。</summary>
        void SendToBack(int instanceId);

        /// <summary>改写窗口的 Tier 并按新 Tier 重新插入。</summary>
        void SetWindowTier(int instanceId, UIWindowTier tier);

        /// <summary>把焦点给窗口；失败返回 false（不可见/不可聚焦）。</summary>
        bool Focus(int instanceId);

        /// <summary>取窗口层容器（UILayer.Window 对应的 RectTransform）。</summary>
        RectTransform GetWindowLayer();

        /// <summary>按实例 ID 取已显示的窗口。</summary>
        bool TryGetWindow(int instanceId, out UIWindow window);
    }

    /// <summary>
    /// 窗口服务：注册表、激活仲裁（z 序 + 焦点）、Tier 调整、布局存取、实例序号分配。
    ///
    /// **不负责**过渡状态机与显隐（那是 UIMgr/UIBase 的事），
    /// **不负责**渲染排序本身（唯一入口是 UIStageHandler）。
    /// </summary>
    internal sealed class UIWindowService
    {
        private readonly IWindowHostAccess _host;
        private readonly IWindowLayoutStore _store;

        private readonly Dictionary<int, Entry> _entries = new();
        private readonly Dictionary<Type, List<int>> _slots = new();

        /// <summary>已经发过「显示」事件的实例：保证开/关事件成对，重复上屏不会重发。</summary>
        private readonly HashSet<int> _shownIds = new();

        public UIWindowService(IWindowHostAccess host, IWindowLayoutStore store)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _store = store ?? new PlayerPrefsWindowLayoutStore();
        }

        /// <summary>
        /// 同时存在的窗口上限。默认不限；由业务按产品需要设置。
        /// 超限且未开启 <see cref="EnableLruEviction"/> 时只告警、不淘汰。
        /// </summary>
        public int MaxWindows { get; set; } = int.MaxValue;

        /// <summary>
        /// 是否在超限时自动淘汰最久未激活的窗口。默认关闭：
        /// 框架擅自 Hide 业务窗口可能中断交易/编辑流程。
        /// </summary>
        public bool EnableLruEviction { get; set; }

        /// <summary>
        /// 窗口显示（上屏）。与注册表解耦：注册表按实例生命周期走，
        /// 本事件按 Show/Hide 走，任务栏/切换器才不会把缓存里的窗口当成还开着。
        /// </summary>
        public event Action<UIWindow> WindowShown;

        /// <summary>窗口隐藏（下屏）。</summary>
        public event Action<UIWindow> WindowHidden;

        /// <summary>窗口被激活（置顶/焦点变化）。</summary>
        public event Action<UIWindow> WindowActivated;

        /// <summary>
        /// 全局默认拖动范围（通常是 UIRoot 下的安全区节点）；
        /// 窗口没有指定 <see cref="UIWindow.DragArea"/> 时用它，仍为 null 则用 Window 层容器（全屏）。
        /// 启动流程里设一次即可。
        /// </summary>
        public RectTransform DefaultDragArea { get; set; }

        /// <summary>已注册窗口数。</summary>
        public int Count => _entries.Count;

        internal sealed class Entry
        {
            public UIWindow Window;
            public Type WindowType;
            public int Slot;
            public float LastActivateTime;
        }

        // —— 注册 ——

        /// <summary>
        /// 窗口实例创建时登记（见 UIWindow.OnInit）。登记只决定进不进任务栏/LRU/布局槽位，
        /// 业务看到的「打开/关闭」走 <see cref="WindowShown"/> / <see cref="WindowHidden"/>。
        /// 超限时拒绝登记并返回 false：窗口仍然可见、仍然参与 Tier 排序。
        /// </summary>
        internal bool Register(UIWindow window)
        {
            if (window == null || window.ID == 0)
                return false;

            var id = window.ID;
            if (_entries.ContainsKey(id))
                return true;

            if (_entries.Count >= MaxWindows)
            {
                if (EnableLruEviction && EvictLeastRecentlyActive())
                {
                    // 已淘汰一个，继续注册。
                }
                else
                {
                    Log.Warning("窗口数量已达上限 {0}，实例 {1}({2}) 未登记。", MaxWindows, id, window.GetType().Name);
                    return false;
                }
            }

            var type = window.GetType();
            _entries[id] = new Entry
            {
                Window = window,
                WindowType = type,
                Slot = AllocSlot(type),
                LastActivateTime = Time.unscaledTime,
            };

            RefreshOcclusion();
            return true;
        }

        /// <summary>
        /// 窗口上屏时由 <see cref="UIWindow.AddToStage"/> 调用。
        /// 未登记的实例（None 能力 / 超限被拒）不发事件，与 <see cref="FillWindows"/> 保持一致。
        /// </summary>
        internal void NotifyShown(UIWindow window)
        {
            if (window == null || !_entries.ContainsKey(window.ID))
                return;

            if (_shownIds.Add(window.ID))
                WindowShown?.Invoke(window);
        }

        /// <summary>窗口下屏时由 <see cref="UIWindow.RemoveToStage"/> 调用；没发过显示事件就是空操作。</summary>
        internal void NotifyHidden(UIWindow window)
        {
            if (window == null || !_shownIds.Remove(window.ID))
                return;

            WindowHidden?.Invoke(window);
        }

        /// <summary>窗口销毁时释放。没登记过是空操作。</summary>
        internal void Unregister(int instanceId)
        {
            if (!_entries.TryGetValue(instanceId, out var entry))
                return;

            _entries.Remove(instanceId);
            ReleaseSlot(entry.WindowType, entry.Slot);

            RefreshOcclusion();
        }

        /// <summary>超限时淘汰：优先已隐藏/最小化的窗口，其次才动仍在显示的。</summary>
        private bool EvictLeastRecentlyActive()
        {
            var id = FindEvictionCandidate();
            if (id == 0 || !_entries.TryGetValue(id, out var entry) || entry.Window == null)
                return false;

            var window = entry.Window;
            Log.Warning("窗口数量超限，淘汰最久未激活的窗口：{0}(id={1})", window.GetType().Name, id);

            // 先 Hide 再注销：关窗事件挂在 Hide 上，反过来会先删条目、事件发不出去。
            window.Hide();
            Unregister(id);
            return true;
        }

        // —— 激活 / 层序 ——

        /// <summary>
        /// 激活窗口：z 序与焦点在**同一次调用**里完成，保证"视觉最上 == 焦点所在"。
        /// 未登记的窗口（Modal、超限被拒）也会被置顶，只是不更新 LRU 时间。
        /// </summary>
        public void Activate(int instanceId)
        {
            if (instanceId == 0)
                return;

            // 已经在本 Tier 末尾时层序不会变，跳过 Promote + RefreshOcclusion：
            // 拖动时每次按下都会走到这里，而 Promote/Refresh 是整层重刷。
            var reordered = !_host.IsLastInTier(instanceId);
            if (reordered)
                _host.Promote(instanceId);

            _host.Focus(instanceId);

            if (_entries.TryGetValue(instanceId, out var entry))
            {
                entry.LastActivateTime = Time.unscaledTime;
                if (entry.Window != null)
                    WindowActivated?.Invoke(entry.Window);
            }

            if (reordered)
                RefreshOcclusion();
        }

        public void SendToBack(int instanceId)
        {
            if (instanceId != 0)
                _host.SendToBack(instanceId);
        }

        /// <summary>运行时调整层叠级别（AlwaysOnTop 开关等）。</summary>
        public void SetTier(int instanceId, UIWindowTier tier)
        {
            if (instanceId != 0)
                _host.SetWindowTier(instanceId, tier);
        }

        // —— 查询 ——

        public bool IsRegistered(int instanceId) => _entries.ContainsKey(instanceId);

        public bool TryGetWindow(int instanceId, out UIWindow window)
        {
            window = null;
            if (!_entries.TryGetValue(instanceId, out var entry) || entry.Window == null)
                return false;

            window = entry.Window;
            return true;
        }

        public int GetSlot(int instanceId)
        {
            return _entries.TryGetValue(instanceId, out var entry) ? entry.Slot : 0;
        }

        /// <summary>按 LRU 时间戳取最久未激活的窗口实例 ID（0 表示没有）。</summary>
        public int GetLeastRecentlyActive()
        {
            var bestId = 0;
            var bestTime = float.MaxValue;
            foreach (var kv in _entries)
            {
                var entry = kv.Value;
                if (entry.Window == null || entry.LastActivateTime >= bestTime)
                    continue;

                bestId = kv.Key;
                bestTime = entry.LastActivateTime;
            }
            return bestId;
        }

        /// <summary>淘汰候选：优先不可见/最小化的最久未激活窗口；没有才退化为最久未激活。</summary>
        private int FindEvictionCandidate()
        {
            var hiddenId = 0;
            var hiddenTime = float.MaxValue;
            var anyId = 0;
            var anyTime = float.MaxValue;

            foreach (var kv in _entries)
            {
                var entry = kv.Value;
                var window = entry.Window;
                if (window == null)
                    continue;

                if (entry.LastActivateTime < anyTime)
                {
                    anyTime = entry.LastActivateTime;
                    anyId = kv.Key;
                }

                var isHidden = !window.Visible || window.LayoutState == WindowState.Minimized;
                if (isHidden && entry.LastActivateTime < hiddenTime)
                {
                    hiddenTime = entry.LastActivateTime;
                    hiddenId = kv.Key;
                }
            }

            return hiddenId != 0 ? hiddenId : anyId;
        }

        // —— 布局 ——

        /// <summary>
        /// 读窗口布局：有存档且合法则用存档，否则用调用方给的默认比例矩形。
        /// M0 只读写数据，真正写进 RectTransform 在 M3 接。
        /// </summary>
        public WindowLayout LoadLayout(UIWindow window, in WindowRect fallback)
        {
            var key = GetLayoutKey(window);
            if (!string.IsNullOrEmpty(key) && _store.TryLoad(key, out var saved))
                return saved;

            var slot = window != null ? GetSlot(window.ID) : 0;
            var tier = window != null ? window.WindowTier : UIWindowTier.Normal;
            return WindowLayout.CreateDefault(slot, tier, fallback);
        }

        public void SaveLayout(UIWindow window, in WindowLayout layout)
        {
            var key = GetLayoutKey(window);
            if (!string.IsNullOrEmpty(key))
                _store.Save(key, layout);
        }

        /// <summary>布局键 = 类型全名 + #实例序号。用类型全名而不是资源地址：Prefab 改名不影响存档。</summary>
        public string GetLayoutKey(UIWindow window)
        {
            if (window == null)
                return null;

            return string.Concat(window.GetType().FullName, "#", GetSlot(window.ID).ToString());
        }

        /// <summary>
        /// 分辨率/窗口尺寸变化后，重新应用所有窗口的保存布局。
        /// 目前由游戏层在分辨率变化时主动调（框架暂无全局分辨率监听）。
        /// </summary>
        public void ReapplyLayouts()
        {
            if (_entries.Count == 0)
                return;

            var windows = new List<UIWindow>(_entries.Count);
            foreach (var entry in _entries.Values)
            {
                if (entry.Window != null)
                    windows.Add(entry.Window);
            }

            for (int i = 0; i < windows.Count; i++)
                windows[i].ReapplyLayout();
        }

        // —— 遮挡 ——

        private struct OcclusionItem
        {
            public UIWindow Window;
            public int Order;
            public Rect Area;
        }

        private readonly List<OcclusionItem> _occlusionBuffer = new List<OcclusionItem>(8);

        /// <summary>
        /// 重算遮挡：按 sortingOrder 从上往下遍历，若某个不透明窗口的矩形完全包含它，
        /// 则判定为被遮挡 → 关射线并回调业务降频。
        /// 只处理「完全包含」，足够覆盖最大化盖小窗、窗口完全叠住等常见场景。
        /// 层序与矩形先算一次再排序：比较器里查层序是 O(层数 × 层内数量)/次比较。
        /// </summary>
        public void RefreshOcclusion()
        {
            if (_entries.Count == 0)
                return;

            _occlusionBuffer.Clear();

            foreach (var entry in _entries.Values)
            {
                var window = entry.Window;
                if (window == null)
                    continue;

                var visible = window.Visible && window.LayoutState != WindowState.Minimized;
                _occlusionBuffer.Add(new OcclusionItem
                {
                    Window = window,
                    // 直接读界面上屏时写回的排序值：等价于按 ID 反查，但不用遍历层内表。
                    Order = window.StageSortingOrder,
                    Area = visible ? window.GetStageRect() : default,
                });
            }

            _occlusionBuffer.Sort((a, b) => b.Order.CompareTo(a.Order));

            for (int i = 0; i < _occlusionBuffer.Count; i++)
            {
                var item = _occlusionBuffer[i];
                var window = item.Window;

                if (!window.Visible || window.LayoutState == WindowState.Minimized)
                {
                    window.SetOccluded(false);
                    continue;
                }

                var occluded = false;

                for (int j = 0; j < i; j++)
                {
                    // 最小化/隐藏的窗口 Area 是 default，Contains 会直接返回 false。
                    if (!_occlusionBuffer[j].Window.IsOpaque)
                        continue;

                    if (Contains(_occlusionBuffer[j].Area, item.Area))
                    {
                        occluded = true;
                        break;
                    }
                }

                window.SetOccluded(occluded);
            }
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            if (outer.width <= 0f || outer.height <= 0f)
                return false;

            const float eps = 0.5f;
            return outer.xMin <= inner.xMin + eps &&
                   outer.xMax >= inner.xMax - eps &&
                   outer.yMin <= inner.yMin + eps &&
                   outer.yMax >= inner.yMax - eps;
        }

        /// <summary>
        /// 取所有已登记窗口（含已关闭但还在缓存里的实例），按创建顺序。
        /// 任务栏要的是「当前打开」的窗口，请按 <see cref="WindowShown"/> / <see cref="WindowHidden"/> 自己维护，
        /// 或者用 <see cref="UIWindow.Visible"/> 过滤。
        /// </summary>
        public void FillWindows(List<UIWindow> output)
        {
            output.Clear();
            foreach (var entry in _entries.Values)
            {
                if (entry.Window != null)
                    output.Add(entry.Window);
            }

            // 按实例 ID（创建顺序）排序，保证任务栏/切换器顺序稳定。
            output.Sort((a, b) => a.ID.CompareTo(b.ID));
        }

        /// <summary>取最近激活且可见的窗口（ESC 关闭、窗口切换用）。最小化只是不渲染，不算可关闭。</summary>
        public UIWindow GetTopmost()
        {
            UIWindow result = null;
            var best = float.MinValue;

            foreach (var entry in _entries.Values)
            {
                var window = entry.Window;
                if (window == null || !window.Visible || window.LayoutState == WindowState.Minimized)
                    continue;

                if (entry.LastActivateTime > best)
                {
                    best = entry.LastActivateTime;
                    result = window;
                }
            }

            return result;
        }

        /// <summary>该窗口是否已有存档布局（用于多实例级联：只有首次打开才偏移）。</summary>
        public bool HasSavedLayout(UIWindow window)
        {
            var key = GetLayoutKey(window);
            return !string.IsNullOrEmpty(key) && _store.TryLoad(key, out _);
        }

        internal void FillDebugNames(List<string> output)
        {
            output.Clear();
            foreach (var kv in _entries)
            {
                var entry = kv.Value;
                var window = entry.Window;
                if (window == null)
                {
                    output.Add($"#{entry.Slot}  id={kv.Key}  <已销毁>");
                    continue;
                }

                output.Add($"#{entry.Slot}  id={kv.Key}  {window.GetType().Name}  " +
                           $"tier={window.WindowTier}  state={window.LayoutState}  " +
                           $"occluded={window.IsOccluded}");
            }
        }

        /// <summary>取全部已注册窗口实例 ID（调试断言用，避免暴露内部字典）。</summary>
        internal void FillWindowIds(List<int> output)
        {
            output.Clear();
            foreach (var kv in _entries)
                output.Add(kv.Key);
        }

        /// <summary>登录切换 / UIMgr.Release 时清空。不销毁界面（由 UIMgr 负责）。</summary>
        internal void Clear()
        {
            _entries.Clear();
            _slots.Clear();
            _shownIds.Clear();
        }

        // —— 实例序号分配 ——

        private int AllocSlot(Type type)
        {
            if (type == null)
                type = typeof(UIWindow);

            if (!_slots.TryGetValue(type, out var used))
            {
                used = new List<int>();
                _slots[type] = used;
            }

            // 取最小未占用序号：关掉 #1 再开应该复用 #1，而不是一路涨到 #5。
            for (var slot = 0; ; slot++)
            {
                if (!used.Contains(slot))
                {
                    used.Add(slot);
                    return slot;
                }
            }
        }

        private void ReleaseSlot(Type type, int slot)
        {
            if (type == null || !_slots.TryGetValue(type, out var used)) return;

            used.Remove(slot);
            if (used.Count == 0)
                _slots.Remove(type);
        }
    }
}
