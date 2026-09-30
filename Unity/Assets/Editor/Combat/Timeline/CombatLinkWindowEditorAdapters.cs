using System.Collections.Generic;
using Assets.Editor.Timeline;
using UnityEditor;
using UnityEngine;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    /// <summary>连招衔接轨：用于连招和动作分支，不承担泛化取消语义。</summary>
    public sealed class CombatLinkWindowEditorTrack : CombatWindowEditorTrackBase<ActionLinkWindow, CombatLinkWindowEditorClip>
    {
        internal CombatLinkWindowEditorTrack(CombatLogicTimelineSource source) : base(source)
        {
        }

        public override string Name => "连招衔接";
        public override string TypeName => nameof(ActionLinkWindow);
        public override Color Color => new(0.66f, 0.42f, 0.88f);
        protected override string TrackIdSuffix => "link-windows";
        protected override string WindowPropertyName => "linkWindows";
        protected override IReadOnlyList<ActionLinkWindow> Windows => Source.Action.LinkWindows;

        protected override CombatLinkWindowEditorClip MakeClip(string stableId) => new(this, stableId);

        protected override void WriteNewWindowFields(SerializedProperty element)
        {
            element.FindPropertyRelative("TargetActionId").intValue = Mathf.Max(1, Source.Action.ActionId);
            element.FindPropertyRelative("RequiresHitConfirm").boolValue = false;
        }

        public override TimelineInspectorBase CreateInspector() =>
            new CombatLinkWindowTrackInspector(Source, this);

        protected override string AddUndoKey => "combat_add_link_window";
        protected override string RemoveUndoKey => "combat_remove_link_window";
    }

    public sealed class CombatLinkWindowEditorClip : CombatWindowEditorClipBase<ActionLinkWindow>
    {
        internal CombatLinkWindowEditorClip(
            CombatLinkWindowEditorTrack track,
            string stableId)
            : base(track.Source, stableId)
        {
            Track = track;
        }

        public CombatLinkWindowEditorTrack Track { get; }
        protected override ITimelineEditorTrack EditorTrack => Track;
        protected override ActionLinkWindow Window => Source.FindLinkWindow(Id);
        public override string TypeName => "连招";

        public override string Name
        {
            get
            {
                var window = Window;
                if (window == null)
                {
                    return "连招衔接";
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

        public override TimelineInspectorBase CreateInspector() =>
            new CombatLinkWindowClipInspector(Source, this);

        protected override string FramesUndoKey => "combat_link_window_frames";
        internal override string DragUndoKey => "combat_drag_link_window";

        public void SetTargetActionId(int actionId)
        {
            var window = Window;
            actionId = Mathf.Max(1, actionId);
            if (!Source.CanEdit || window == null || window.TargetActionId == actionId)
            {
                return;
            }

            Source.RecordEdit("combat_link_target_action");
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

            Source.RecordEdit("combat_link_hit_confirm");
            window.RequiresHitConfirm = value;
            CommitEdit();
        }
    }
}
