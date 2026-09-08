using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 角色战斗配置。只保存数值、技能表现资源和宏观状态的表现映射；状态转换规则全部由代码控制。
    /// </summary>
    [CreateAssetMenu(fileName = "CombatProfile", menuName = "UxGame/战斗/角色技能配置")]
    public sealed class CharacterCombatProfile : ScriptableObject
    {
        [SerializeField, Min(1)] private int frameRate = TimelineAsset.DefaultFrameRate;
        [SerializeField] private string group = string.Empty;
        [SerializeField, Min(0)] private float moveSpeedPerSecond = 5f;
        [SerializeField, Min(0)] private float turnDegreesPerSecond = 720f;

        [Header("状态表现列表（动态）")]
        [SerializeField] private List<CombatStatePresentation> statePresentations = new();

        // 旧版本 Profile 使用固定字段保存状态 Timeline。字段仅保留用于导入旧资源，
        // ValidateData 会把它们迁移到 statePresentations 后清空；编辑器不会再显示这些字段。
        [SerializeField, HideInInspector] private TimelineAsset idleTimeline;
        [SerializeField, HideInInspector] private TimelineAsset moveTimeline;
        [SerializeField, HideInInspector] private TimelineAsset airborneTimeline;
        [SerializeField, HideInInspector] private TimelineAsset stunnedTimeline;
        [SerializeField, HideInInspector] private TimelineAsset knockbackTimeline;
        [SerializeField, HideInInspector] private TimelineAsset frozenTimeline;
        [SerializeField, HideInInspector] private TimelineAsset deadTimeline;

        [Header("技能表现列表")]
        [SerializeField] private List<CombatActionAsset> actions = new();

        public int FrameRate => Mathf.Max(1, frameRate);
        public string Group => group;
        public float MoveSpeedPerSecond => Mathf.Max(0, moveSpeedPerSecond);
        public float TurnDegreesPerSecond => Mathf.Max(0, turnDegreesPerSecond);
        public IReadOnlyList<CombatStatePresentation> StatePresentations => statePresentations;
        public IReadOnlyList<CombatActionAsset> Actions => actions;

        /// <summary>
        /// 按逻辑状态和外部表现变体解析一条状态表现。
        /// 当指定变体不存在时，优先回退到同一状态的 default，再回退到优先级最高的条目。
        /// 这样旧调用方无需传变体也能继续工作，同时皮肤/武器可以明确选择自己的变体。
        /// </summary>
        public CombatStatePresentation GetStatePresentation(
            StateLayer layer,
            int stateId,
            string variantId = null)
        {
            var requestedVariant = CombatStatePresentation.NormalizeVariantId(variantId);
            CombatStatePresentation exact = null;
            CombatStatePresentation defaultPresentation = null;
            CombatStatePresentation fallback = null;

            if (statePresentations == null)
            {
                return null;
            }

            for (var i = 0; i < statePresentations.Count; i++)
            {
                var presentation = statePresentations[i];
                if (presentation == null ||
                    presentation.Layer != layer ||
                    presentation.StateId != stateId)
                {
                    continue;
                }

                if (string.Equals(
                        presentation.VariantId,
                        requestedVariant,
                        StringComparison.Ordinal))
                {
                    exact = SelectBetter(exact, presentation);
                }

                if (string.Equals(
                        presentation.VariantId,
                        CombatStatePresentation.DefaultVariantId,
                        StringComparison.Ordinal))
                {
                    defaultPresentation = SelectBetter(defaultPresentation, presentation);
                }

                fallback = SelectBetter(fallback, presentation);
            }

            return exact ?? defaultPresentation ?? fallback;
        }

        public TimelineAsset GetStateTimeline(
            StateLayer layer,
            int stateId,
            string variantId = null)
        {
            return GetStatePresentation(layer, stateId, variantId)?.Timeline;
        }

        public CombatActionAsset FindAction(int actionId)
        {
            if (actions == null || actionId <= 0)
            {
                return null;
            }
            foreach (var action in actions)
            {
                if (action != null && action.ActionId == actionId)
                {
                    return action;
                }
            }
            return null;
        }

        public void ValidateRuntime()
        {
            ValidateData();

            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var presentationKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var presentation in statePresentations)
            {
                if (presentation == null)
                {
                    continue;
                }

                if (presentation.Layer == StateLayer.Action)
                {
                    throw new InvalidOperationException(
                        $"状态表现不能使用 Action 层: profile={name}, state={presentation.StateId}, variant={presentation.VariantId}");
                }
                if (!CombatStateId.IsDefined(presentation.Layer, presentation.StateId))
                {
                    throw new InvalidOperationException(
                        $"状态表现使用了未定义状态: profile={name}, layer={presentation.Layer}, state={presentation.StateId}, variant={presentation.VariantId}");
                }
                if (!stableIds.Add(presentation.StableId))
                {
                    throw new InvalidOperationException(
                        $"状态表现 StableId 重复: profile={name}, stableId={presentation.StableId}");
                }

                var key = $"{(int)presentation.Layer}:{presentation.StateId}:{presentation.VariantId}";
                if (!presentationKeys.Add(key))
                {
                    throw new InvalidOperationException(
                        $"状态表现变体重复: profile={name}, layer={presentation.Layer}, state={presentation.StateId}, variant={presentation.VariantId}");
                }

                ValidateTimeline(
                    presentation.Timeline,
                    $"{presentation.Layer}/{CombatStateId.GetDisplayName(presentation.Layer, presentation.StateId)}/{presentation.VariantId}");
            }
        }

        public void ValidateData()
        {
            frameRate = Mathf.Max(1, frameRate);
            group ??= string.Empty;
            moveSpeedPerSecond = Mathf.Max(0, moveSpeedPerSecond);
            turnDegreesPerSecond = Mathf.Max(0, turnDegreesPerSecond);

            statePresentations ??= new List<CombatStatePresentation>();
            statePresentations.RemoveAll(presentation => presentation == null);
            MigrateLegacyStateTimelines();
            foreach (var presentation in statePresentations)
            {
                presentation?.ValidateData();
            }

            actions ??= new List<CombatActionAsset>();
            actions.RemoveAll(action => action == null);
        }

        private void MigrateLegacyStateTimelines()
        {
            MigrateLegacyStateTimeline(
                StateLayer.Locomotion,
                LocomotionState.Idle.ToId(),
                "Idle",
                ref idleTimeline);
            MigrateLegacyStateTimeline(
                StateLayer.Locomotion,
                LocomotionState.Move.ToId(),
                "Move",
                ref moveTimeline);
            MigrateLegacyStateTimeline(
                StateLayer.Locomotion,
                LocomotionState.Airborne.ToId(),
                "Airborne",
                ref airborneTimeline);
            MigrateLegacyStateTimeline(
                StateLayer.Control,
                ControlState.Stunned.ToId(),
                "Stunned",
                ref stunnedTimeline);
            MigrateLegacyStateTimeline(
                StateLayer.Control,
                ControlState.Knockback.ToId(),
                "Knockback",
                ref knockbackTimeline);
            MigrateLegacyStateTimeline(
                StateLayer.Control,
                ControlState.Frozen.ToId(),
                "Frozen",
                ref frozenTimeline);
            MigrateLegacyStateTimeline(
                StateLayer.Life,
                LifeState.Dead.ToId(),
                "Dead",
                ref deadTimeline);
        }

        private void MigrateLegacyStateTimeline(
            StateLayer layer,
            int stateId,
            string displayName,
            ref TimelineAsset legacyTimeline)
        {
            if (legacyTimeline == null)
            {
                return;
            }

            var existing = FindStatePresentation(
                layer,
                stateId,
                CombatStatePresentation.DefaultVariantId);
            if (existing == null)
            {
                statePresentations.Add(new CombatStatePresentation(
                    layer,
                    stateId,
                    CombatStatePresentation.DefaultVariantId,
                    legacyTimeline,
                    $"legacy.{(int)layer}.{stateId}",
                    displayName));
            }
            else if (existing.Timeline == null)
            {
                existing.SetTimeline(legacyTimeline);
            }

            legacyTimeline = null;
        }

        private CombatStatePresentation FindStatePresentation(
            StateLayer layer,
            int stateId,
            string variantId)
        {
            var normalizedVariant = CombatStatePresentation.NormalizeVariantId(variantId);
            foreach (var presentation in statePresentations)
            {
                if (presentation != null && presentation.Matches(layer, stateId, normalizedVariant))
                {
                    return presentation;
                }
            }
            return null;
        }

        private static CombatStatePresentation SelectBetter(
            CombatStatePresentation current,
            CombatStatePresentation candidate)
        {
            if (current == null)
            {
                return candidate;
            }
            if (candidate == null)
            {
                return current;
            }

            var priority = candidate.Priority.CompareTo(current.Priority);
            if (priority > 0)
            {
                return candidate;
            }
            if (priority < 0)
            {
                return current;
            }

            var stableId = string.Compare(
                candidate.StableId,
                current.StableId,
                StringComparison.Ordinal);
            return stableId < 0 ? candidate : current;
        }

        private void ValidateTimeline(TimelineAsset value, string stateName)
        {
            if (value != null && value.FrameRate != FrameRate)
            {
                throw new InvalidOperationException(
                    $"状态 Timeline 帧率不一致: profile={name}, state={stateName}, profileRate={FrameRate}, timelineRate={value.FrameRate}");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateData();
        }
#endif
    }
}
