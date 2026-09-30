using System.Collections.Generic;
using Assets.Editor.Timeline;
using UnityEditor;
using UnityEngine;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    /// <summary>CombatActionAsset 中固定存在的攻击判定轨。每条窗口代表一个独立攻击判定周期。</summary>
    public sealed class CombatHitboxWindowEditorTrack : CombatWindowEditorTrackBase<ActionHitboxWindow, CombatHitboxWindowEditorClip>
    {
        internal CombatHitboxWindowEditorTrack(CombatLogicTimelineSource source) : base(source)
        {
        }

        public override string Name => "攻击判定";
        public override string TypeName => nameof(ActionHitboxWindow);
        public override Color Color => new(0.88f, 0.24f, 0.2f);
        protected override string TrackIdSuffix => "hitbox-windows";
        protected override string WindowPropertyName => "hitboxWindows";
        protected override IReadOnlyList<ActionHitboxWindow> Windows => Source.Action.HitboxWindows;

        protected override CombatHitboxWindowEditorClip MakeClip(string stableId) => new(this, stableId);

        public override TimelineInspectorBase CreateInspector() => new CombatHitboxWindowTrackInspector(Source, this);

        protected override string AddUndoKey => "combat_add_hitbox_window";
        protected override string RemoveUndoKey => "combat_remove_hitbox_window";
    }

    public sealed class CombatHitboxWindowEditorClip : CombatWindowEditorClipBase<ActionHitboxWindow>
    {
        internal CombatHitboxWindowEditorClip(CombatHitboxWindowEditorTrack track, string stableId)
            : base(track.Source, stableId)
        {
            Track = track;
        }

        public CombatHitboxWindowEditorTrack Track { get; }
        protected override ITimelineEditorTrack EditorTrack => Track;
        protected override ActionHitboxWindow Window => Source.FindHitboxWindow(Id);
        public override string TypeName => "判定";

        public override string Name
        {
            get
            {
                var window = Window;
                if (window == null)
                {
                    return "攻击判定";
                }

                var windows = Source.Action.HitboxWindows;
                for (var i = 0; i < windows.Count; i++)
                {
                    if (ReferenceEquals(windows[i], window))
                    {
                        return $"判定 {i + 1}";
                    }
                }
                return "攻击判定";
            }
        }

        public ActionHitShape Shape => Window?.Shape ?? ActionHitShape.Circle;

        public int RadiusMillimeters => Window?.RadiusMillimeters ?? 0;

        public override TimelineInspectorBase CreateInspector() => new CombatHitboxWindowClipInspector(Source, this);

        protected override string FramesUndoKey => "combat_set_hitbox_window_frames";

        internal override string DragUndoKey => "combat_drag_hitbox_window";

        /// <summary>形状与半径是资产上的私有序列化字段，只能经 SerializedProperty 写回。</summary>
        public void SetGeometry(ActionHitShape shape, int radiusMillimeters)
        {
            if (!Source.CanEdit)
            {
                return;
            }

            var index = Source.FindWindowIndex("hitboxWindows", Id);
            if (index < 0)
            {
                return;
            }

            Source.RecordEdit("combat_set_hitbox_window_geometry");
            var serialized = new SerializedObject(Source.Action);
            serialized.Update();
            var element = serialized.FindProperty("hitboxWindows").GetArrayElementAtIndex(index);
            element.FindPropertyRelative("shape").enumValueIndex = (int)shape;
            element.FindPropertyRelative("radiusMillimeters").intValue =
                Mathf.Clamp(radiusMillimeters, 1, 10000000);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Source.Save();
            Source.Run(this);
            Source.NotifyChanged();
        }
    }
}
