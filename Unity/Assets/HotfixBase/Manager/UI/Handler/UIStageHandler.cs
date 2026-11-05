using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// 界面上屏排序：跨层顺序由 Sorting Layer 保证（每层固定一个），层内顺序由
    /// 界面 Canvas 的 sortingOrder 动态重排（Tier 基址 + 层内序号 × 100）。
    ///
    /// 层内顺序规则：
    /// 1. 先按 <see cref="IUIStaged.WindowTier"/> 分组，Normal &lt; AlwaysOnTop；
    /// 2. 同 Tier 内保持插入顺序（新窗口默认插在当前焦点窗口之上，见 InsertByActivationRule）；
    /// 3. 只提供相对操作（Promote / SendToBack / SetWindowTier），不暴露 SetOrder —— 手动排序号
    ///    与任意一次 Register/Unregister 引起的重排都会打架。
    /// </summary>
    internal sealed class UIStageHandler
    {
        private const int OrderStep = 100;

        /// <summary>模型/特效相对父界面的 sortingOrder 偏移。</summary>
        internal const int ModelOffset = 50;

        /// <summary>每个 Tier 预留的 sortingOrder 跨度。</summary>
        internal const int TierSpan = 1000;

        /// <summary>单 Tier 内最多容纳的界面数（OrderStep 决定）。</summary>
        internal const int TierCapacity = TierSpan / OrderStep - 1;

        /// <summary>
        /// Tier 数量，直接取枚举成员数：加 Tier 只改 <see cref="UIWindowTier"/>，不再改第二个常量。
        /// 前提是枚举值从 0 连续（下面按 (int)tier 当数组下标）。
        /// </summary>
        private static readonly int TierCount = Enum.GetValues(typeof(UIWindowTier)).Length;

        /// <summary>
        /// 层名固定为 "sl" + 枚举值（Bottom=1 → sl1 ... Top=9 → sl9）。
        /// 不再维护第二份映射表：加层只改 UILayer，再跑一次 SortingLayer 工具。
        /// </summary>
        private static bool TryGetSortingLayerName(UILayer layer, out string name)
        {
            if (layer == UILayer.Root)
            {
                name = null;
                return false;
            }

            name = "sl" + (int)layer;
            return true;
        }

        private readonly Dictionary<UILayer, List<UIBase>> _stageOrder = new Dictionary<UILayer, List<UIBase>>();
        private readonly List<UIBase> _sorted = new List<UIBase>(16);
        private readonly int[] _tierCounters = new int[TierCount];
        private readonly UIBlurHandler _blurHandler;
        private readonly Func<int> _getFocusedId;
        private bool _layerNameWarned;
        private bool _tierOverflowWarned;

        public UIStageHandler(UIBlurHandler blurHandler, Func<int> getFocusedId)
        {
            _blurHandler = blurHandler;
            _getFocusedId = getFocusedId;
        }

        /// <summary>界面上屏：挂 Canvas + GraphicRaycaster，进入层内顺序表并重排。</summary>
        public void Register(UIBase ui, UILayer layer)
        {
            if (ui == null || ui.GameObject == null) return;
            if (!TryGetSortingLayerName(layer, out var sortingLayer)) return;

            var canvas = ui.GameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = ui.GameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                ui.GameObject.AddComponent<GraphicRaycaster>();
            }

            canvas.sortingLayerName = sortingLayer;
            ui.StageCanvas = canvas;
            ui.StageSortingLayerName = sortingLayer;

            if (canvas.sortingLayerID == 0 && sortingLayer != "Default" && !_layerNameWarned)
            {
                _layerNameWarned = true;
                Log.Error("Sorting Layer [{0}] 解析不到（落到 Default）。跑一次 UxGame/工具/UI/初始化 UI SortingLayer，然后重启 Unity。",
                    sortingLayer);
            }

            var list = GetLayerList(layer);
            list.Remove(ui);
            InsertByActivationRule(list, ui);
            Refresh(layer);
        }

        /// <summary>界面下屏：从层内顺序表移除并重排；没登记过的界面是空操作。</summary>
        public void Unregister(int instanceId, UILayer layer)
        {
            if (!_stageOrder.TryGetValue(layer, out var list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null || list[i].ID != instanceId) continue;
                list.RemoveAt(i);
                Refresh(layer);
                return;
            }
        }

        /// <summary>查某个已上屏界面的排序信息（模糊背景要插在触发窗口之下时用）。</summary>
        public bool TryGetSorting(int instanceId, out string sortingLayer, out int order)
        {
            foreach (var kv in _stageOrder)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var ui = list[i];
                    if (ui == null || ui.ID != instanceId || string.IsNullOrEmpty(ui.StageSortingLayerName)) continue;
                    sortingLayer = ui.StageSortingLayerName;
                    order = ui.StageSortingOrder;
                    return true;
                }
            }

            sortingLayer = null;
            order = 0;
            return false;
        }

        /// <summary>提到本 Tier 末尾。窗口激活时调用。</summary>
        public void Promote(int instanceId)
        {
            if (!TryTake(instanceId, out var layer, out var list, out var ui)) return;

            list.Remove(ui);
            list.Insert(GetTierEnd(list, TierOf(ui)), ui);
            Refresh(layer);
        }

        /// <summary>降到本 Tier 开头。</summary>
        public void SendToBack(int instanceId)
        {
            if (!TryTake(instanceId, out var layer, out var list, out var ui)) return;

            list.Remove(ui);
            list.Insert(GetTierStart(list, TierOf(ui)), ui);
            Refresh(layer);
        }

        /// <summary>
        /// 是否已经在本 Tier 末尾（再 Promote 不会改变顺序）。
        /// 用于跳过重复激活：拖动时每次按下都会走到 Activate，而 Promote + Refresh 是整层重刷。
        /// </summary>
        public bool IsLastInTier(int instanceId)
        {
            if (!TryTake(instanceId, out _, out var list, out var ui))
                return false;

            var self = list.IndexOf(ui);
            if (self < 0)
                return false;

            var tier = TierOf(ui);
            for (int i = self + 1; i < list.Count; i++)
            {
                var above = list[i];
                if (above == null)
                    continue;

                // 只有比本 Tier 更高的界面允许排在我之上（Promote 也不会越过它们）。
                if (TierOf(above) <= tier)
                    return false;
            }

            return true;
        }

        /// <summary>改写界面的 Tier 并按新级别重新插入（AlwaysOnTop 开关等）。</summary>
        public void SetWindowTier(int instanceId, UIWindowTier tier)
        {
            if (!TryTake(instanceId, out var layer, out var list, out var ui)) return;
            if (!(ui is IUIStaged staged)) return;
            if (staged.WindowTier == tier) return;

            staged.SetWindowTier(tier);
            list.Remove(ui);
            list.Insert(GetTierEnd(list, tier), ui);
            Refresh(layer);
        }

        /// <summary>非窗口层界面不实现 IUIStaged，一律按 Normal 处理。</summary>
        private static UIWindowTier TierOf(UIBase ui)
            => ui is IUIStaged staged ? staged.WindowTier : UIWindowTier.Normal;

        public void Clear() => _stageOrder.Clear();

        private List<UIBase> GetLayerList(UILayer layer)
        {
            if (!_stageOrder.TryGetValue(layer, out var list))
            {
                list = new List<UIBase>(8);
                _stageOrder[layer] = list;
            }
            return list;
        }

        /// <summary>
        /// 新界面的插入位置：默认本 Tier 末尾；若本 Tier 内已有焦点界面，插到它之上。
        /// 焦点在更高 Tier 时不能越界，否则普通窗口会盖到 AlwaysOnTop 上面。
        /// </summary>
        private void InsertByActivationRule(List<UIBase> list, UIBase ui)
        {
            var tierEnd = GetTierEnd(list, TierOf(ui));
            var insert = tierEnd;

            var focusedId = _getFocusedId != null ? _getFocusedId() : 0;
            if (focusedId != 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var focused = list[i];
                    if (focused == null || focused.ID != focusedId) continue;

                    if (TierOf(focused) >= TierOf(ui) && i + 1 <= tierEnd)
                        insert = i + 1;
                    break;
                }
            }

            list.Insert(insert, ui);
        }

        /// <summary>本 Tier 之后的第一个位置（也就是本 Tier 的末尾）。</summary>
        private static int GetTierEnd(List<UIBase> list, UIWindowTier tier)
        {
            var end = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var ui = list[i];
                if (ui != null && TierOf(ui) <= tier)
                    end = i + 1;
            }
            return end;
        }

        /// <summary>本 Tier 的第一个位置。</summary>
        private static int GetTierStart(List<UIBase> list, UIWindowTier tier)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var ui = list[i];
                if (ui != null && TierOf(ui) >= tier)
                    return i;
            }
            return list.Count;
        }

        /// <summary>按实例 ID 找到界面及它所在的层内表。</summary>
        private bool TryTake(int instanceId, out UILayer layer, out List<UIBase> list, out UIBase ui)
        {
            foreach (var kv in _stageOrder)
            {
                var items = kv.Value;
                for (int i = 0; i < items.Count; i++)
                {
                    var candidate = items[i];
                    if (candidate == null || candidate.ID != instanceId) continue;

                    layer = kv.Key;
                    list = items;
                    ui = candidate;
                    return true;
                }
            }

            layer = UILayer.Root;
            list = null;
            ui = null;
            return false;
        }

        /// <summary>按 (Tier, 层内顺序) 重排 sortingOrder，并联动刷新模糊背景。</summary>
        private void Refresh(UILayer layer)
        {
            if (_stageOrder.TryGetValue(layer, out var list))
            {
                // UIWindowTier 只服务 Window 层；其他层直接按插入顺序排，不参与 Tier 分组。
                if (layer != UILayer.Window)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        if (item == null || item.GameObject == null) continue;
                        item.ApplyStageSortingOrder(OrderStep * (i + 1));
                    }

                    _blurHandler.RefreshBackdropSorting();
                    return;
                }

                // Tier 分组稳定排序：按 Tier 从小到大收集，同 Tier 内保持原相对顺序。
                _sorted.Clear();
                for (int tier = 0; tier < TierCount; tier++)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var ui = list[i];
                        if (ui == null || ui.GameObject == null) continue;
                        if ((int)TierOf(ui) == tier)
                            _sorted.Add(ui);
                    }
                }

                Array.Clear(_tierCounters, 0, _tierCounters.Length);

                for (int i = 0; i < _sorted.Count; i++)
                {
                    var ui = _sorted[i];
                    var tier = (int)TierOf(ui);
                    if (tier < 0 || tier >= TierCount)
                        tier = (int)UIWindowTier.Normal;

                    var indexInTier = ++_tierCounters[tier];
                    if (indexInTier > TierCapacity && !_tierOverflowWarned)
                    {
                        _tierOverflowWarned = true;
                        Log.Error("层内单 Tier 超过 {0} 个界面，sortingOrder 会与相邻 Tier 重叠。Layer[{1}] Tier[{2}]",
                            TierCapacity, layer, (UIWindowTier)tier);
                    }

                    ui.ApplyStageSortingOrder(tier * TierSpan + OrderStep * indexInTier);
                }
            }

            _blurHandler.RefreshBackdropSorting();
        }
    }
}
