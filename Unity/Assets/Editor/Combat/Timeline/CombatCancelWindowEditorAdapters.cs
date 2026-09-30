using System.Collections.Generic;
using Assets.Editor.Timeline;
using UnityEditor;
using UnityEngine;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    /// <summary>CombatActionAsset 中固定存在的取消窗口轨。多个取消目标允许在同一帧区间内同时开放。</summary>
    public sealed class CombatCancelWindowEditorTrack : CombatWindowEditorTrackBase<ActionCancelWindow, CombatCancelWindowEditorClip>
    {
        internal CombatCancelWindowEditorTrack(CombatLogicTimelineSource source) : base(source)
        {
        }

        public override string Name => "可取消窗口";
        public override string TypeName => nameof(ActionCancelWindow);
        public override Color Color => new(0.88f, 0.52f, 0.18f);
        protected override string TrackIdSuffix => "cancel-windows";
        protected override string WindowPropertyName => "cancelWindows";
        protected override IReadOnlyList<ActionCancelWindow> Windows => Source.Action.CancelWindows;

        protected override CombatCancelWindowEditorClip MakeClip(string stableId) => new(this, stableId);

        protected override void WriteNewWindowFields(SerializedProperty element)
        {
            element.FindPropertyRelative("TargetActionId").intValue = Mathf.Max(1, Source.Action.ActionId);
            element.FindPropertyRelative("RequiresHitConfirm").boolValue = false;
        }

        public override TimelineInspectorBase CreateInspector() => new CombatCancelWindowTrackInspector(Source, this);

        protected override string AddUndoKey => "combat_add_cancel_window";
        protected override string RemoveUndoKey => "combat_remove_cancel_window";
    }

    public sealed class CombatCancelWindowEditorClip : CombatWindowEditorClipBase<ActionCancelWindow>
    {
        internal CombatCancelWindowEditorClip(CombatCancelWindowEditorTrack track, string stableId)
            : base(track.Source, stableId)
        {
            Track = track;
        }

        public CombatCancelWindowEditorTrack Track { get; }
        protected override ITimelineEditorTrack EditorTrack => Track;
        protected override ActionCancelWindow Window => Source.FindCancelWindow(Id);
        public override string TypeName => "可取消";

        public override string Name
        {
            get
            {
                var window = Window;
                if (window == null)
                {
                    return "可取消窗口";
                }

                var target = Source.Profile?.FindAction(window.TargetActionId);
                var targetName = target == null || string.IsNullOrEmpty(target.DisplayName)
                    ? window.TargetActionId.ToString()
                    : $"{target.DisplayName} ({window.TargetActionId})";
                return window.RequiresHitConfirm ? $"→ {targetName} [需命中]" : $"→ {targetName}";
            }
        }

        public int TargetActionId => Window?.TargetActionId ?? 1;

        public bool RequiresHitConfirm => Window?.RequiresHitConfirm == true;

        public override TimelineInspectorBase CreateInspector() => new CombatCancelWindowClipInspector(Source, this);

        protected override string FramesUndoKey => "combat_cancel_window_frames";

        internal override string DragUndoKey => "combat_drag_cancel_window";

        public void SetTargetActionId(int actionId)
        {
            var window = Window;
            actionId = Mathf.Max(1, actionId);
            if (!Source.CanEdit || window == null || window.TargetActionId == actionId)
            {
                return;
            }

            Source.RecordEdit("combat_cancel_target_action");
            window.TargetActionId = actionId;
            CommitEdit();
        }

        public void SetRequiresHitConfirm(bool value)
        {
            var window = Window;
            if (!Source.CanEdit || window == null || window.RequiresHitConfirm == value)
            {
                return;
            }

            Source.RecordEdit("combat_cancel_hit_confirm");
            window.RequiresHitConfirm = value;
            CommitEdit();
        }
    }
}
