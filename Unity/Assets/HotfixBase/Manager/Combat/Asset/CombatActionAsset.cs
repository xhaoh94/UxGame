using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public sealed class ActionCancelWindow
    {
        [Min(0)] public int StartFrame;
        [Min(0)] public int EndFrame;
        public CombatCommandType AcceptedCommand = CombatCommandType.Attack;
        [Min(1)] public int TargetActionId;
        public int Priority;
        public bool RequiresHitConfirm;

        public bool IsOpen(int actionFrame, CombatCommandType command, bool hasHitConfirmed)
        {
            return actionFrame >= StartFrame &&
                   actionFrame <= EndFrame &&
                   command == AcceptedCommand &&
                   (!RequiresHitConfirm || hasHitConfirmed);
        }

        public void ValidateData()
        {
            StartFrame = Mathf.Max(0, StartFrame);
            EndFrame = Mathf.Max(StartFrame, EndFrame);
            TargetActionId = Mathf.Max(1, TargetActionId);
        }
    }

    /// <summary>
    /// 一个可执行战斗动作的不可变配置。状态机只表示 Free/Executing，具体动作生命周期由
    /// CombatActionRunner 管理，逐帧内容由 Timeline 管理。
    /// </summary>
    [CreateAssetMenu(fileName = "CombatAction", menuName = "UxGame/战斗/技能")]
    public sealed class CombatActionAsset : ScriptableObject
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField, Min(1)] private int actionId = 1;
        [SerializeField] private string displayName = "新技能";
        [SerializeField] private CombatCommandType triggerCommand = CombatCommandType.Attack;
        [SerializeField] private int priority;
        [SerializeField, Min(0)] private int durationFrames = 30;
        [SerializeField] private ActionMovementPolicy movementPolicy = ActionMovementPolicy.Block;
        [SerializeField] private TimelineAsset timeline;
        [SerializeField] private List<ActionCancelWindow> cancelWindows = new();

        public string StableId => stableId;
        public int ActionId => actionId;
        public string DisplayName => displayName;
        public CombatCommandType TriggerCommand => triggerCommand;
        public int Priority => priority;
        public int DurationFrames => durationFrames > 0
            ? durationFrames
            : Mathf.Max(1, timeline?.DurationFrames ?? 1);
        public ActionMovementPolicy MovementPolicy => movementPolicy;
        public TimelineAsset Timeline => timeline;
        public IReadOnlyList<ActionCancelWindow> CancelWindows => cancelWindows;

        public void ValidateData()
        {
            if (string.IsNullOrEmpty(stableId))
            {
                stableId = Guid.NewGuid().ToString("N");
            }
            actionId = Mathf.Max(1, actionId);
            displayName ??= string.Empty;
            durationFrames = Mathf.Max(0, durationFrames);
            cancelWindows ??= new List<ActionCancelWindow>();
            foreach (var window in cancelWindows)
            {
                window?.ValidateData();
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
