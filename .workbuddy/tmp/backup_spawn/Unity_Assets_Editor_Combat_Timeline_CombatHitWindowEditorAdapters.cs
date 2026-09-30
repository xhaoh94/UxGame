using System;
using System.Collections.Generic;
using UnityEngine;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    /// <summary>CombatActionAsset 中固定存在的命中激活窗口轨。</summary>
    public sealed class CombatHitWindowEditorTrack : ITimelineEditorTrack
    {
        readonly List<CombatHitWindowEditorClip> clips = new();

        internal CombatHitWindowEditorTrack(CombatLogicTimelineSource source)
        {
            Source = source;
            RebuildAdapters();
        }

        public CombatLogicTimelineSource Source { get; }
        ITimelineEditorSource ITimelineEditorTrack.Source => Source;
        public string Id => $"{Source.Id}/hit-windows";
        public string Name => "命中窗口";
        public string TypeName => nameof(ActionHitWindow);
        public string DisplayTypeName => "逻辑";
        public Color Color => new(0.88f, 0.24f, 0.2f);
        public int EndFrame
        {
            get
            {
                var endFrame = 0;
                foreach (var clip in clips)
                {
                    endFrame = Mathf.Max(endFrame, clip.EndFrame);
                }
                return endFrame;
            }
        }
        public bool CanRename => false;
        public bool CanRemove => false;
        public bool CanCreateClip => true;
        public IReadOnlyList<CombatHitWindowEditorClip> Clips => clips;
        IReadOnlyList<ITimelineEditorClip> ITimelineEditorTrack.Clips => clips;

        public void Rename(string name) { }
        public bool Remove() => false;
        public CombatHitWindowEditorClip CreateClip(string assetPath = null) =>
            Source.AddHitWindow();
        ITimelineEditorClip ITimelineEditorTrack.CreateClip(string assetPath) => CreateClip(assetPath);

        public bool RemoveClip(ITimelineEditorClip clip)
        {
            return clip is CombatHitWindowEditorClip hitClip &&
                   Source.RemoveHitWindow(hitClip);
        }

        public bool IsLayoutValid()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var window in Source.Action.HitWindows)
            {
                if (window == null || string.IsNullOrEmpty(window.StableId) ||
                    !ids.Add(window.StableId) || window.StartFrame < 0 ||
                    window.EndFrame <= window.StartFrame ||
                    window.EndFrame > Source.DurationFrames)
                {
                    return false;
                }
            }
            // 每条窗口代表一个独立命中周期；相邻或重叠均为合法的显式配置。
            return true;
        }

        public void UpdateMixData() { }
        public void RecordUndo(string key) => Source.RecordEdit(key);
        public void Bind(Action callback) => Source.Bind(this, callback);
        public void Unbind(Action callback) => Source.Unbind(this, callback);

        internal CombatHitWindowEditorClip FindClip(string stableId)
        {
            return clips.Find(clip => clip.Id == stableId);
        }

        internal void RebuildAdapters()
        {
            clips.Clear();
            foreach (var window in Source.Action.HitWindows)
            {
                if (window != null)
                {
                    clips.Add(new CombatHitWindowEditorClip(this, window.StableId));
                }
            }
        }
    }

    public sealed class CombatHitWindowEditorClip : ITimelineEditorClip
    {
        internal CombatHitWindowEditorClip(CombatHitWindowEditorTrack track, string stableId)
        {
            Track = track;
            Id = stableId;
        }

        public CombatHitWindowEditorTrack Track { get; }
        public CombatLogicTimelineSource Source => Track.Source;
        ITimelineEditorTrack ITimelineEditorClip.Track => Track;
        ActionHitWindow Window => Source.FindHitWindow(Id);
        public string Id { get; }
        public string Name => Source.GetHitWindowDisplayName(Window);
        public string TypeName => "命中";
        public int StartFrame => Window?.StartFrame ?? 0;
        public int EndFrame => Window?.EndFrame ?? 1;
        public int InFrame => 0;
        public int OutFrame => 0;
        public int DurationFrames => Mathf.Max(1, EndFrame - StartFrame);
        public ActionHitShape Shape => Window?.Shape ?? ActionHitShape.Circle;
        public int RadiusMillimeters => Window?.RadiusMillimeters ?? 0;
        public bool CanFitAnimationDuration => false;

        public void Rename(string name) { }
        public void BeginDrag() => Source.BeginHitWindowDrag(this);

        public void Drag(DragStatus status, int nowFrame, int lastFrame)
        {
            var startFrame = StartFrame;
            var endFrame = EndFrame;
            switch (status)
            {
                case DragStatus.Left:
                    startFrame = nowFrame;
                    break;
                case DragStatus.Right:
                    endFrame = nowFrame;
                    break;
                case DragStatus.Move:
                    var delta = nowFrame - lastFrame;
                    var duration = DurationFrames;
                    startFrame += delta;
                    endFrame += delta;
                    if (startFrame < 0)
                    {
                        startFrame = 0;
                        endFrame = duration;
                    }
                    else if (endFrame > Source.DurationFrames)
                    {
                        endFrame = Source.DurationFrames;
                        startFrame = Mathf.Max(0, endFrame - duration);
                    }
                    break;
            }
            Source.SetHitWindowFrames(this, startFrame, endFrame, true);
        }

        public void SetFrames(int startFrame, int endFrame, bool save = true)
        {
            if (StartFrame == startFrame && EndFrame == endFrame)
            {
                return;
            }
            if (save)
            {
                Source.RecordEdit("combat_set_hit_window_frames");
            }
            Source.SetHitWindowFrames(this, startFrame, endFrame, true);
            if (save)
            {
                CommitEdit();
            }
        }

        public void SetGeometry(ActionHitShape shape, int radiusMillimeters)
        {
            Source.RecordEdit("combat_set_hit_window_geometry");
            Source.SetHitWindowGeometry(this, shape, radiusMillimeters);
        }

        public void CommitEdit() => Source.CommitHitWindowEdit(this);
        public bool TryAssignAnimation(string assetPath) => false;
        public bool TryAssignAnimation(AnimationClip animation) => false;
        public bool FitAnimationDuration() => false;
        public void RecordUndo(string key) => Source.RecordEdit(key);
        public void Bind(Action callback) => Source.Bind(this, callback);
        public void Unbind(Action callback) => Source.Unbind(this, callback);
    }
}
