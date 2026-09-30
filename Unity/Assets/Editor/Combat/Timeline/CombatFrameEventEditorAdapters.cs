using Assets.Editor.Timeline;
using System;
using UnityEditor;
using UnityEngine;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    /// <summary>
    /// CombatActionAsset 离散帧事件的编辑适配器。
    /// 事件不创建 Timeline 轨，只在标记区域显示一个可选中的单帧标记。
    /// </summary>
    public sealed class CombatFrameEventEditorAdapter : ITimelineEditorFrameEvent, ICombatLogicTimelineInspectorSource
    {
        readonly CombatLogicTimelineSource source;
        readonly string stableId;

        internal CombatFrameEventEditorAdapter(CombatLogicTimelineSource source, string stableId)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.stableId = stableId ?? string.Empty;
        }

        public ITimelineEditorSource Source => source;
        CombatLogicTimelineSource ICombatLogicTimelineInspectorSource.Owner => source;

        public string Id => stableId;
        public int Frame => source.FindFrameEventData(stableId)?.Frame ?? 0;
        public bool CanRemove => true;
        public Color Color => new(0.36f, 0.72f, 0.84f);
        public string DisplayName => source.FindFrameEventData(stableId)?.DisplayName ?? string.Empty;
        public string TypeName => source.FindFrameEventData(stableId) is ActionSpawnEvent
            ? "生成投射物"
            : "帧事件";

        public string Name
        {
            get
            {
                var frameEvent = source.FindFrameEventData(stableId);
                if (frameEvent == null)
                {
                    return "帧事件";
                }
                if (!string.IsNullOrEmpty(frameEvent.DisplayName))
                {
                    return frameEvent.DisplayName;
                }
                if (!(frameEvent is ActionSpawnEvent spawnEvent))
                {
                    return frameEvent.DefaultDisplayName;
                }

                var spawn = spawnEvent.SpawnProfile;
                if (spawn == null)
                {
                    return spawnEvent.DefaultDisplayName;
                }
                return string.IsNullOrEmpty(spawn.DisplayName)
                    ? spawnEvent.DefaultDisplayName
                    : spawn.DisplayName;
            }
        }

        public void SetFrame(int frame)
        {
            var current = Frame;
            if (!source.CanEdit || current == frame || source.DurationFrames <= 0)
            {
                return;
            }

            source.RecordEdit("combat_frame_event_frame");
            source.SetFrameEventFrame(stableId, frame);
            source.Save();
            source.Run(this);
            source.NotifyChanged();
        }

        public bool Remove()
        {
            return source.RemoveFrameEvent(this);
        }

        public void Bind(Action action)
        {
            source.Bind(this, action);
        }

        public void Unbind(Action action)
        {
            source.Unbind(this, action);
        }

        public TimelineInspectorBase CreateInspector()
        {
            return source.FindFrameEventData(stableId) is ActionSpawnEvent
                ? new CombatFrameEventInspector(source, this)
                : null;
        }

        public CombatSpawnProfile SpawnProfile =>
            (source.FindFrameEventData(stableId) as ActionSpawnEvent)?.SpawnProfile;

        public void SetSpawnProfile(CombatSpawnProfile spawn)
        {
            if (!source.CanEdit || !(source.FindFrameEventData(stableId) is ActionSpawnEvent) ||
                ReferenceEquals(SpawnProfile, spawn))
            {
                return;
            }

            source.RecordEdit("combat_spawn_event_profile");
            source.SetSpawnEventProfile(stableId, spawn);
            source.Save();
            source.Run(this);
            source.NotifyChanged();
        }

        public void SetDisplayName(string displayName)
        {
            displayName ??= string.Empty;
            if (!source.CanEdit || DisplayName == displayName)
            {
                return;
            }

            source.RecordEdit("combat_frame_event_name");
            source.SetFrameEventDisplayName(stableId, displayName);
            source.Save();
            source.Run(this);
            source.NotifyChanged();
        }
    }
}
