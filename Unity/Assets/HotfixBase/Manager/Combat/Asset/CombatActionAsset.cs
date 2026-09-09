using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public sealed class ActionCancelWindow
    {
        [SerializeField, HideInInspector] private string stableId = string.Empty;
        [Min(0)] public int StartFrame;
        [Min(1)] public int EndFrame = 1;
        [Min(1)] public int TargetActionId;
        public bool RequiresHitConfirm;

        public string StableId => stableId;

        /// <summary>取消窗口统一使用 [StartFrame, EndFrame) 半开区间。</summary>
        public bool IsOpen(int actionFrame, bool hasHitConfirmed)
        {
            return actionFrame >= StartFrame &&
                   actionFrame < EndFrame &&
                   (!RequiresHitConfirm || hasHitConfirmed);
        }

        public void ValidateData()
        {
            if (string.IsNullOrEmpty(stableId))
            {
                RegenerateStableId();
            }
            StartFrame = Mathf.Clamp(StartFrame, 0, int.MaxValue - 1);
            EndFrame = (int)Math.Min(
                int.MaxValue,
                Math.Max((long)StartFrame + 1, EndFrame));
            TargetActionId = Mathf.Max(1, TargetActionId);
        }

        internal void RegenerateStableId()
        {
            stableId = Guid.NewGuid().ToString("N");
        }
    }

    public enum ActionHitShape : byte
    {
        Circle = 0,
    }

    [Serializable]
    public sealed class ActionHitWindow
    {
        [SerializeField, HideInInspector] private string stableId = string.Empty;
        [Min(0)] public int StartFrame;
        [Min(1)] public int EndFrame = 1;
        [SerializeField] private ActionHitShape shape = ActionHitShape.Circle;
        [SerializeField, Min(1)] private int radiusMillimeters = 1000;

        public string StableId => stableId;
        public ActionHitShape Shape => shape;
        public int RadiusMillimeters => radiusMillimeters;

        /// <summary>命中激活窗口统一使用 [StartFrame, EndFrame) 半开区间。</summary>
        public bool IsActive(int actionFrame)
        {
            return actionFrame >= StartFrame && actionFrame < EndFrame;
        }

        public void ValidateData()
        {
            if (string.IsNullOrEmpty(stableId))
            {
                RegenerateStableId();
            }
            StartFrame = Mathf.Clamp(StartFrame, 0, int.MaxValue - 1);
            EndFrame = (int)Math.Min(
                int.MaxValue,
                Math.Max((long)StartFrame + 1, EndFrame));
            radiusMillimeters = Mathf.Clamp(radiusMillimeters, 1, 10000000);
        }

        internal void RegenerateStableId()
        {
            stableId = Guid.NewGuid().ToString("N");
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
        [SerializeField] private List<ActionHitWindow> hitWindows = new();

        public string StableId => stableId;
        public int ActionId => actionId;
        public string DisplayName => displayName;
        public int DurationFrames => durationFrames;
        public ActionMovementPolicy MovementPolicy => movementPolicy;
        public IReadOnlyList<ActionCancelWindow> CancelWindows => cancelWindows;
        public IReadOnlyList<ActionHitWindow> HitWindows => hitWindows;

        public void ValidateData()
        {
            if (string.IsNullOrEmpty(stableId))
            {
                stableId = Guid.NewGuid().ToString("N");
            }
            actionId = Mathf.Max(1, actionId);
            displayName ??= string.Empty;
            MigrateLogicItemStableIds();
            foreach (var window in cancelWindows)
            {
                window?.ValidateData();
            }
            foreach (var window in hitWindows)
            {
                window?.ValidateData();
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
            if (hitWindows == null)
            {
                hitWindows = new List<ActionHitWindow>();
                changed = true;
            }

            // 同一动作内所有逻辑子项共享 ItemId 唯一域。固定先扫描取消窗口，保证新增轨道时
            // 已有取消窗口 ID 不会被无故改写；后续逻辑轨也必须追加到这个固定扫描顺序中。
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
            foreach (var window in hitWindows)
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
            hitWindows ??= new List<ActionHitWindow>();
        }
#endif
    }
}
