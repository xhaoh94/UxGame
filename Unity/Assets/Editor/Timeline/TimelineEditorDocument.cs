using Assets.Editor.Timeline;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux.Editor.Timeline
{
    /// <summary>
    /// Timeline 窗口与具体数据源之间的会话文档。窗口负责装配，View/Item 只消费该文档与适配器接口。
    /// 一个文档可同时组合表现 Timeline 与战斗逻辑轨道，但各 source 仍独立 Undo、校验和保存。
    /// </summary>
    public sealed class TimelineEditorDocument
    {
        public const int DefaultFrameRate = 60;

        readonly Func<bool> isPlaying;
        readonly List<ITimelineEditorSource> sources = new();
        readonly List<ITimelineEditorTrack> tracks = new();
        readonly Dictionary<ITimelineEditorSource, Action> structureHandlers = new();
        readonly Dictionary<ITimelineEditorSource, Action> changedHandlers = new();

        public TimelineEditorDocument(Func<bool> isPlaying = null)
        {
            this.isPlaying = isPlaying;
        }

        /// <summary>兼容单源调用方；多源时优先返回表现 source。</summary>
        public ITimelineEditorSource Source => PresentationSource ??
            (sources.Count > 0 ? sources[0] : null);
        public ITimelineEditorSource PresentationSource =>
            sources.Find(source => source.Role == TimelineEditorSourceRole.Presentation);
        public IReadOnlyList<ITimelineEditorSource> Sources => sources;
        public bool HasSource => sources.Count > 0;
        public bool CanEdit => !(isPlaying?.Invoke() ?? false) &&
                               sources.Exists(source => source.CanEdit);
        public int FrameRate => Source?.FrameRate ?? DefaultFrameRate;
        public int DurationFrames
        {
            get
            {
                var duration = 0;
                foreach (var source in sources)
                {
                    duration = Mathf.Max(duration, source.DurationFrames);
                }
                return duration;
            }
        }
        public int TrackCount => tracks.Count;
        public IReadOnlyList<ITimelineEditorTrack> Tracks => tracks;

        public event Action StructureChanged;
        public event Action Changed;
        public event Action<ITimelineEditorSource> SourceStructureChanged;
        public event Action<ITimelineEditorSource> SourceChanged;

        public void SetSource(ITimelineEditorSource source)
        {
            SetSources(source == null
                ? Array.Empty<ITimelineEditorSource>()
                : new[] { source });
        }

        public void SetSources(params ITimelineEditorSource[] nextSources)
        {
            nextSources ??= Array.Empty<ITimelineEditorSource>();
            if (HasSameSources(nextSources))
            {
                return;
            }

            UnsubscribeSources();
            sources.Clear();
            foreach (var source in nextSources)
            {
                if (source == null || sources.Contains(source))
                {
                    continue;
                }
                sources.Add(source);
            }
            SubscribeSources();
            RebuildTracks();
            StructureChanged?.Invoke();
            Changed?.Invoke();
        }

        public IReadOnlyList<Type> GetTrackTypes()
        {
            var result = new List<Type>();
            foreach (var source in sources)
            {
                foreach (var trackType in source.GetTrackTypes())
                {
                    if (trackType != null && !result.Contains(trackType))
                    {
                        result.Add(trackType);
                    }
                }
            }
            return result;
        }

        public string GetTrackDisplayName(Type trackType)
        {
            foreach (var source in sources)
            {
                if (ContainsTrackType(source, trackType))
                {
                    return source.GetTrackDisplayName(trackType);
                }
            }
            return trackType?.Name ?? string.Empty;
        }

        public ITimelineEditorTrack AddTrack(Type trackType)
        {
            if (!CanEdit)
            {
                return null;
            }

            ITimelineEditorSource owner = null;
            foreach (var source in sources)
            {
                if (!source.CanEdit || !ContainsTrackType(source, trackType))
                {
                    continue;
                }
                if (owner != null)
                {
                    // 裸 Type 在多个 source 中重复时无法确定保存 owner，拒绝歧义路由。
                    return null;
                }
                owner = source;
            }
            return owner?.AddTrack(trackType);
        }

        public bool SetFrameRate(int frameRate)
        {
            if (!CanEdit)
            {
                return false;
            }
            foreach (var source in sources)
            {
                if (source.CanEdit && source.CanSetFrameRate)
                {
                    return source.SetFrameRate(frameRate);
                }
            }
            return false;
        }

        public TimelineInspectorBase CreateInspector(object selection)
        {
            var owner = selection switch
            {
                ITimelineEditorTrack track => track.Source,
                ITimelineEditorClip clip => clip.Track?.Source,
                _ => null,
            };
            return owner != null && sources.Contains(owner)
                ? owner.CreateInspector(selection)
                : null;
        }

        public void SaveAll()
        {
            var savedOwners = new HashSet<UnityEngine.Object>();
            foreach (var source in sources)
            {
                if (source.UndoOwner != null && savedOwners.Add(source.UndoOwner))
                {
                    source.Save();
                }
            }
        }

        public bool RefreshAfterUndo(UnityEngine.Object owner)
        {
            if (owner == null)
            {
                return false;
            }
            foreach (var source in sources)
            {
                if (!ReferenceEquals(source.UndoOwner, owner))
                {
                    continue;
                }
                source.RefreshAfterUndo();
                return true;
            }
            return false;
        }

        bool HasSameSources(IReadOnlyList<ITimelineEditorSource> nextSources)
        {
            if (sources.Count != nextSources.Count)
            {
                return false;
            }
            for (var i = 0; i < sources.Count; i++)
            {
                if (!ReferenceEquals(sources[i], nextSources[i]))
                {
                    return false;
                }
            }
            return true;
        }

        void SubscribeSources()
        {
            foreach (var source in sources)
            {
                var captured = source;
                Action structureHandler = () => OnStructureChanged(captured);
                Action changedHandler = () => OnChanged(captured);
                structureHandlers.Add(source, structureHandler);
                changedHandlers.Add(source, changedHandler);
                source.StructureChanged += structureHandler;
                source.Changed += changedHandler;
            }
        }

        void UnsubscribeSources()
        {
            foreach (var pair in structureHandlers)
            {
                pair.Key.StructureChanged -= pair.Value;
            }
            foreach (var pair in changedHandlers)
            {
                pair.Key.Changed -= pair.Value;
            }
            structureHandlers.Clear();
            changedHandlers.Clear();
        }

        void OnStructureChanged(ITimelineEditorSource source)
        {
            RebuildTracks();
            SourceStructureChanged?.Invoke(source);
            StructureChanged?.Invoke();
        }

        void OnChanged(ITimelineEditorSource source)
        {
            SourceChanged?.Invoke(source);
            Changed?.Invoke();
        }

        void RebuildTracks()
        {
            tracks.Clear();
            foreach (var source in sources)
            {
                foreach (var track in source.Tracks)
                {
                    if (track != null)
                    {
                        tracks.Add(track);
                    }
                }
            }
        }

        static bool ContainsTrackType(ITimelineEditorSource source, Type trackType)
        {
            if (source == null || trackType == null)
            {
                return false;
            }
            foreach (var candidate in source.GetTrackTypes())
            {
                if (candidate == trackType)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
