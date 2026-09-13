using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 一个宏观战斗状态的可选表现。
    ///
    /// 状态本身仍由代码中的 StateLayer/stateId 定义；资源不再为 Idle、Move、Dead 等状态保留固定
    /// Timeline 字段，而是通过这个动态条目列表绑定任意数量的表现变体。variantId 由角色皮肤、武器
    /// 或上层表现逻辑指定，空值统一视为 default。
    /// </summary>
    [Serializable]
    public sealed class CombatStatePresentation
    {
        public const string DefaultVariantId = "default";

        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private StateLayer layer = StateLayer.Locomotion;
        [SerializeField, Min(1)] private int stateId = (int)LocomotionState.Idle;
        [SerializeField] private string variantId = DefaultVariantId;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private int priority;
        [SerializeField] private TimelineAsset timeline;

        public CombatStatePresentation()
        {
        }

        public CombatStatePresentation(StateLayer layer, int stateId, string variantId, TimelineAsset timeline, string stableId = null, string displayName = null, int priority = 0)
        {
            this.layer = layer;
            this.stateId = stateId;
            this.variantId = NormalizeVariantId(variantId);
            this.timeline = timeline;
            this.stableId = string.IsNullOrEmpty(stableId)
                ? BuildStableId(layer, stateId, this.variantId)
                : stableId;
            this.displayName = displayName ?? string.Empty;
            this.priority = priority;
        }

        public string StableId => stableId;
        public StateLayer Layer => layer;
        public int StateId => stateId;
        public string VariantId => NormalizeVariantId(variantId);
        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? CombatStateId.GetDisplayName(layer, stateId)
            : displayName;
        public int Priority => priority;
        public TimelineAsset Timeline => timeline;

        /// <summary>供 Profile 迁移旧字段时使用；正常编辑应通过 SerializedObject 完成。</summary>
        public void SetTimeline(TimelineAsset value)
        {
            timeline = value;
        }

        public bool Matches(StateLayer targetLayer, int targetStateId, string targetVariantId)
        {
            return layer == targetLayer &&
                   stateId == targetStateId &&
                   string.Equals(VariantId, NormalizeVariantId(targetVariantId), StringComparison.Ordinal);
        }

        public void ValidateData()
        {
            stateId = Mathf.Max(1, stateId);
            variantId = NormalizeVariantId(variantId);
            if (string.IsNullOrEmpty(stableId))
            {
                stableId = BuildStableId(layer, stateId, variantId);
            }

            displayName ??= string.Empty;
        }

        public static string NormalizeVariantId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? DefaultVariantId
                : value.Trim();
        }

        public static string BuildStableId(StateLayer layer, int stateId, string variantId)
        {
            return $"state.{(int)layer}.{stateId}.{NormalizeVariantId(variantId)}";
        }
    }
}
