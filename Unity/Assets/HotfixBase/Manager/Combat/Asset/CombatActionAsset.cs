using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ux
{
    /// <summary>带目标动作的通用转移窗口基类。</summary>
    [Serializable]
    public abstract class ActionTargetWindow : CombatLogicWindow
    {
        [Min(1)] public int TargetActionId;
        public bool RequiresHitConfirm;

        /// <summary>目标动作窗口统一使用 [StartFrame, EndFrame) 半开区间。</summary>
        public bool IsOpen(int actionFrame, bool hasHitConfirmed)
        {
            return ContainsFrame(actionFrame) &&
                   (!RequiresHitConfirm || hasHitConfirmed);
        }

        public override void ValidateData()
        {
            base.ValidateData();
            TargetActionId = Mathf.Max(1, TargetActionId);
        }
    }

    /// <summary>泛化取消窗口：闪避、受击反制或其它非连招打断。</summary>
    [Serializable]
    public sealed class ActionCancelWindow : ActionTargetWindow
    {
    }

    /// <summary>连招衔接窗口：连招和动作分支的输入衔接。</summary>
    [Serializable]
    public sealed class ActionLinkWindow : ActionTargetWindow
    {
    }

    public enum ActionHitShape : byte
    {
        Circle = 0,
    }

    [Serializable]
    public sealed class ActionHitboxWindow : CombatLogicWindow
    {
        [SerializeField] private ActionHitShape shape = ActionHitShape.Circle;
        [SerializeField, Min(1)] private int radiusMillimeters = 1000;

        public ActionHitShape Shape => shape;
        public int RadiusMillimeters => radiusMillimeters;

        /// <summary>攻击判定窗口统一使用 [StartFrame, EndFrame) 半开区间。</summary>
        public bool IsActive(int actionFrame)
        {
            return ContainsFrame(actionFrame);
        }

        public override void ValidateData()
        {
            base.ValidateData();
            radiusMillimeters = Mathf.Clamp(radiusMillimeters, 1, 10000000);
        }
    }

    /// <summary>
    /// 动作附带增益的配置 —— ⚠ 最小版本（完整形态属于 P4）。
    /// BuffId 为 0 表示不附带任何增益（默认值），所以旧资产不需要迁移。
    /// </summary>
    [Serializable]
    public sealed class ActionBuffApply
    {
        [SerializeField, Min(0)] private int buffId;
        [SerializeField, Min(1)] private int durationFrames = 120;
        [SerializeField, Min(0)] private int damagePerTick;

        public int BuffId => buffId;
        public int DurationFrames => Mathf.Max(1, durationFrames);
        public int DamagePerTick => Mathf.Max(0, damagePerTick);

        /// <summary>是否配置了增益。为 false 时命中不会往目标身上挂任何东西。</summary>
        public bool IsEnabled => buffId > 0;

        public void ValidateData()
        {
            buffId = Mathf.Max(0, buffId);
            durationFrames = Mathf.Max(1, durationFrames);
            damagePerTick = Mathf.Max(0, damagePerTick);
        }
    }

    /// <summary>
    /// 一个可执行战斗动作的权威逻辑配置。状态机只表示 Free/Executing，具体动作生命周期由
    /// CombatActionRunner 管理；客户端 Timeline 通过 CharacterCombatProfile 的独立表现映射关联。
    /// </summary>
    [CreateAssetMenu(fileName = "CombatAction", menuName = "UxGame/战斗/技能")]
    public sealed class CombatActionAsset : ScriptableObject
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField, Min(1)] private int actionId = 1;
        [SerializeField] private string displayName = "新技能";
        [SerializeField, Min(1)] private int durationFrames = 30;
        [SerializeField] private ActionMovementPolicy movementPolicy = ActionMovementPolicy.Block;
        [SerializeField] private List<ActionCancelWindow> cancelWindows = new();
        [FormerlySerializedAs("transitionWindows")]
        [SerializeField] private List<ActionLinkWindow> linkWindows = new();
        [FormerlySerializedAs("hitWindows")]
        [SerializeField] private List<ActionHitboxWindow> hitboxWindows = new();
        [SerializeReference] private List<CombatFrameEvent> frameEvents = new();

        // 伤害配置 —— 最小版本，固定值，没有修改器栈与随机区间（见 CombatDamageSystem 的类注释）。
        [Header("伤害与效果（最小版本）")]
        [SerializeField, Min(0)] private int damage = 10;
        [SerializeField] private ActionBuffApply appliedBuff = new();

        public string StableId => stableId;
        public int ActionId => actionId;
        public string DisplayName => displayName;
        public int DurationFrames => durationFrames;
        public ActionMovementPolicy MovementPolicy => movementPolicy;
        public IReadOnlyList<ActionCancelWindow> CancelWindows => cancelWindows;
        public IReadOnlyList<ActionLinkWindow> LinkWindows => linkWindows;
        public IReadOnlyList<ActionHitboxWindow> HitboxWindows => hitboxWindows;

        /// <summary>这一招包含的离散帧事件。事件只在自身 Frame 上成立一次。</summary>
        public IReadOnlyList<CombatFrameEvent> FrameEvents => frameEvents;

        /// <summary>这一招打中后扣多少血。固定值，不参与任何公式。</summary>
        public int Damage => Mathf.Max(0, damage);

        /// <summary>这一招打中后往目标身上挂什么增益。BuffId 为 0 表示不挂。</summary>
        public ActionBuffApply AppliedBuff => appliedBuff;

        public void ValidateData()
        {
            if (string.IsNullOrEmpty(stableId))
            {
                stableId = Guid.NewGuid().ToString("N");
            }
            actionId = Mathf.Max(1, actionId);
            displayName ??= string.Empty;
            damage = Mathf.Max(0, damage);
            appliedBuff ??= new ActionBuffApply();
            appliedBuff.ValidateData();
            MigrateLogicItemStableIds();
            foreach (var window in cancelWindows)
            {
                window?.ValidateData();
            }
            foreach (var window in linkWindows)
            {
                window?.ValidateData();
            }
            foreach (var window in hitboxWindows)
            {
                window?.ValidateData();
            }
            foreach (var frameEvent in frameEvents)
            {
                frameEvent?.ValidateData();
            }
        }

        /// <summary>
        /// 只迁移逻辑子项身份，不修正区间或其它业务字段。编辑器打开旧资源时可安全调用并单独落盘。
        /// </summary>
        public bool MigrateLogicItemStableIds()
        {
            var changed = false;
            if (cancelWindows == null)
            {
                cancelWindows = new List<ActionCancelWindow>();
                changed = true;
            }
            if (linkWindows == null)
            {
                linkWindows = new List<ActionLinkWindow>();
                changed = true;
            }
            if (hitboxWindows == null)
            {
                hitboxWindows = new List<ActionHitboxWindow>();
                changed = true;
            }
            if (frameEvents == null)
            {
                frameEvents = new List<CombatFrameEvent>();
                changed = true;
            }

            // 同一动作内所有逻辑子项共享 StableId 唯一域。固定先扫描取消窗口，保证后续新增
            // 攻击判定窗口或帧事件时，已有取消窗口 ID 不会被无故改写。
            var logicItemIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var window in cancelWindows)
            {
                if (window == null)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(window.StableId) ||
                    !logicItemIds.Add(window.StableId))
                {
                    window.RegenerateStableId();
                    logicItemIds.Add(window.StableId);
                    changed = true;
                }
            }
            foreach (var window in linkWindows)
            {
                if (window == null)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(window.StableId) ||
                    !logicItemIds.Add(window.StableId))
                {
                    window.RegenerateStableId();
                    logicItemIds.Add(window.StableId);
                    changed = true;
                }
            }
            foreach (var window in hitboxWindows)
            {
                if (window == null)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(window.StableId) ||
                    !logicItemIds.Add(window.StableId))
                {
                    window.RegenerateStableId();
                    logicItemIds.Add(window.StableId);
                    changed = true;
                }
            }
            foreach (var frameEvent in frameEvents)
            {
                if (frameEvent == null)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(frameEvent.StableId) ||
                    !logicItemIds.Add(frameEvent.StableId))
                {
                    frameEvent.RegenerateStableId();
                    logicItemIds.Add(frameEvent.StableId);
                    changed = true;
                }
            }
            return changed;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 导入/脚本重载阶段只维护资产结构与资产自身身份；业务区间由显式编辑提交和校验处理。
            // 子项 StableId 留给 CombatLogicTimelineSource 的可持久化迁移，避免导入时先改业务字段。
            if (string.IsNullOrEmpty(stableId))
            {
                stableId = Guid.NewGuid().ToString("N");
            }
            displayName ??= string.Empty;
            cancelWindows ??= new List<ActionCancelWindow>();
            linkWindows ??= new List<ActionLinkWindow>();
            hitboxWindows ??= new List<ActionHitboxWindow>();
            frameEvents ??= new List<CombatFrameEvent>();
            appliedBuff ??= new ActionBuffApply();
        }
#endif
    }
}
