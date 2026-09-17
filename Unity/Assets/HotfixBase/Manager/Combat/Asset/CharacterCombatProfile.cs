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

        [Header("基础属性（最小版本）")]
        [SerializeField, Min(1)] private int maxHp = 100;

        [Header("状态表现列表（动态）")]
        [SerializeField] private List<CombatStatePresentation> statePresentations = new();

        [Header("技能逻辑列表")]
        [SerializeField] private List<CombatActionAsset> actions = new();

        [Header("技能表现映射")]
        [SerializeField] private List<CombatActionPresentation> actionPresentations = new();

        public int FrameRate => Mathf.Max(1, frameRate);
        public string Group => group;
        public float MoveSpeedPerSecond => Mathf.Max(0, moveSpeedPerSecond);
        public float TurnDegreesPerSecond => Mathf.Max(0, turnDegreesPerSecond);

        /// <summary>最大生命值。最小版本的属性系统只有这一项。</summary>
        public int MaxHp => Mathf.Max(1, maxHp);
        public IReadOnlyList<CombatStatePresentation> StatePresentations => statePresentations;
        public IReadOnlyList<CombatActionAsset> Actions => actions;
        public IReadOnlyList<CombatActionPresentation> ActionPresentations => actionPresentations;

        /// <summary>
        /// 按逻辑状态和外部表现变体解析一条状态表现。
        /// 当指定变体不存在时，优先回退到同一状态的 default，再回退到优先级最高的条目。
        /// 这样旧调用方无需传变体也能继续工作，同时皮肤/武器可以明确选择自己的变体。
        /// </summary>
        public CombatStatePresentation GetStatePresentation(StateLayer layer, int stateId, string variantId = null)
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

        public TimelineAsset GetStateTimeline(StateLayer layer, int stateId, string variantId = null)
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

        public CombatActionPresentation GetActionPresentation(int actionId)
        {
            return GetActionPresentation(FindAction(actionId));
        }

        public CombatActionPresentation GetActionPresentation(CombatActionAsset action)
        {
            if (action == null || actionPresentations == null)
            {
                return null;
            }

            foreach (var presentation in actionPresentations)
            {
                if (presentation != null && presentation.Matches(action))
                {
                    return presentation;
                }
            }
            return null;
        }

        public TimelineAsset GetActionTimeline(int actionId)
        {
            return GetActionPresentation(actionId)?.Timeline;
        }

        public TimelineAsset GetActionTimeline(CombatActionAsset action)
        {
            return GetActionPresentation(action)?.Timeline;
        }

        public void ValidateRuntime()
        {
            ValidateData();

            var actionIds = new HashSet<int>();
            var actionStableIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var action in actions)
            {
                if (action.ActionId <= 0 || string.IsNullOrEmpty(action.StableId))
                {
                    throw new InvalidOperationException(
                        $"动作缺少 ActionId 或 StableId: profile={name}, action={action.name}");
                }
                if (action.DurationFrames <= 0)
                {
                    throw new InvalidOperationException(
                        $"动作逻辑持续帧必须大于 0: profile={name}, action={action.name}, duration={action.DurationFrames}");
                }
                if (!actionIds.Add(action.ActionId))
                {
                    throw new InvalidOperationException(
                        $"动作 ID 重复: profile={name}, actionId={action.ActionId}");
                }
                if (!actionStableIds.Add(action.StableId))
                {
                    throw new InvalidOperationException(
                        $"动作 StableId 重复: profile={name}, stableId={action.StableId}");
                }
            }

            foreach (var action in actions)
            {
                if (action.CancelWindows == null)
                {
                    throw new InvalidOperationException(
                        $"取消窗口列表为空引用: profile={name}, action={action.name}");
                }
                if (action.HitWindows == null)
                {
                    throw new InvalidOperationException(
                        $"命中窗口列表为空引用: profile={name}, action={action.name}");
                }

                var logicItemIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var window in action.CancelWindows)
                {
                    if (window == null)
                    {
                        throw new InvalidOperationException(
                            $"取消窗口为空引用: profile={name}, action={action.name}");
                    }
                    if (string.IsNullOrEmpty(window.StableId) ||
                        !logicItemIds.Add(window.StableId))
                    {
                        throw new InvalidOperationException(
                            $"逻辑子项 StableId 缺失或重复: profile={name}, action={action.name}, item={window.StableId}");
                    }
                    if (window.StartFrame < 0 ||
                        window.EndFrame <= window.StartFrame ||
                        window.EndFrame > action.DurationFrames)
                    {
                        throw new InvalidOperationException(
                            $"取消窗口区间无效: profile={name}, action={action.name}, range=[{window.StartFrame}, {window.EndFrame}), duration={action.DurationFrames}");
                    }
                    if (!actionIds.Contains(window.TargetActionId))
                    {
                        throw new InvalidOperationException(
                            $"取消窗口目标动作不存在: profile={name}, action={action.name}, target={window.TargetActionId}");
                    }
                }
                foreach (var window in action.HitWindows)
                {
                    if (window == null)
                    {
                        throw new InvalidOperationException(
                            $"命中窗口为空引用: profile={name}, action={action.name}");
                    }
                    if (string.IsNullOrEmpty(window.StableId) ||
                        !logicItemIds.Add(window.StableId))
                    {
                        throw new InvalidOperationException(
                            $"逻辑子项 StableId 缺失或重复: profile={name}, action={action.name}, item={window.StableId}");
                    }
                    if (window.StartFrame < 0 ||
                        window.EndFrame <= window.StartFrame ||
                        window.EndFrame > action.DurationFrames)
                    {
                        throw new InvalidOperationException(
                            $"命中窗口区间无效: profile={name}, action={action.name}, range=[{window.StartFrame}, {window.EndFrame}), duration={action.DurationFrames}");
                    }
                    if (!Enum.IsDefined(typeof(ActionHitShape), window.Shape) ||
                        window.RadiusMillimeters <= 0 ||
                        window.RadiusMillimeters > 10000000)
                    {
                        throw new InvalidOperationException(
                            $"命中窗口形状参数无效: profile={name}, action={action.name}, shape={window.Shape}, radius={window.RadiusMillimeters}");
                    }
                }
            }

            var mappedActions = new HashSet<CombatActionAsset>();
            foreach (var presentation in actionPresentations)
            {
                var action = presentation.Action;
                if (action == null)
                {
                    throw new InvalidOperationException($"技能表现映射缺少逻辑技能: profile={name}");
                }
                if (!actions.Contains(action))
                {
                    throw new InvalidOperationException(
                        $"技能表现映射引用了不属于当前 Profile 的动作: profile={name}, action={action.name}");
                }
                if (!mappedActions.Add(action))
                {
                    throw new InvalidOperationException(
                        $"技能表现映射重复: profile={name}, actionId={action.ActionId}");
                }
                ValidateTimeline(presentation.Timeline, $"Action/{action.ActionId}");
            }

            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var presentationKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var presentation in statePresentations)
            {
                if (presentation == null)
                {
                    continue;
                }

                if (!CombatStateId.IsStatePresentationMappable(
                        presentation.Layer,
                        presentation.StateId))
                {
                    throw new InvalidOperationException(
                        $"状态表现不能映射该状态: profile={name}, layer={presentation.Layer}, state={presentation.StateId}, variant={presentation.VariantId}");
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
            maxHp = Mathf.Max(1, maxHp);

            statePresentations ??= new List<CombatStatePresentation>();
            statePresentations.RemoveAll(presentation => presentation == null);
            foreach (var presentation in statePresentations)
            {
                presentation?.ValidateData();
            }

            actions ??= new List<CombatActionAsset>();
            actions.RemoveAll(action => action == null);

            actionPresentations ??= new List<CombatActionPresentation>();
            actionPresentations.RemoveAll(presentation => presentation == null);
        }

        private static CombatStatePresentation SelectBetter(CombatStatePresentation current, CombatStatePresentation candidate)
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
