using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// 界面上屏排序：跨层顺序由 Sorting Layer 保证（每层固定一个），层内顺序由
    /// 界面 Canvas 的 sortingOrder 动态重排（显示顺序 × 100）。
    /// 表里直接存 UIBase 引用——只有已上屏的界面在表里，因此不需要回查 UIMgr。
    /// </summary>
    internal sealed class UIStageHandler
    {
        private const int OrderStep = 100;

        /// <summary>模型/特效相对父界面的 sortingOrder 偏移。</summary>
        internal const int ModelOffset = 50;

        private static readonly Dictionary<UILayer, string> SortingLayerNames = new Dictionary<UILayer, string>
        {
            { UILayer.Bottom, "sl1" },
            { UILayer.BlurBackdrop, "sl2" },
            { UILayer.View, "sl3" },
            { UILayer.Window, "sl4" },
            { UILayer.Tip, "sl5" },
            { UILayer.Top, "sl6" },
        };

        private readonly Dictionary<UILayer, List<UIBase>> _stageOrder = new Dictionary<UILayer, List<UIBase>>();
        private readonly UIBlurHandler _blurHandler;
        private bool _layerNameWarned;

        public UIStageHandler(UIBlurHandler blurHandler)
        {
            _blurHandler = blurHandler;
        }

        /// <summary>界面上屏：挂 Canvas + GraphicRaycaster，进入层内顺序表并重排。</summary>
        public void Register(UIBase ui, UILayer layer)
        {
            if (ui == null || ui.GameObject == null) return;
            if (!SortingLayerNames.TryGetValue(layer, out var sortingLayer)) return;

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

            if (!_stageOrder.TryGetValue(layer, out var list))
            {
                list = new List<UIBase>();
                _stageOrder[layer] = list;
            }

            list.Remove(ui);
            list.Add(ui);
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

        public void Clear() => _stageOrder.Clear();

        /// <summary>按层内显示顺序重排 sortingOrder，并联动刷新模糊背景。</summary>
        private void Refresh(UILayer layer)
        {
            if (_stageOrder.TryGetValue(layer, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var ui = list[i];
                    if (ui == null || ui.GameObject == null) continue;
                    ui.ApplyStageSortingOrder(OrderStep * (i + 1));
                }
            }

            _blurHandler.RefreshBackdropSorting();
        }
    }
}
