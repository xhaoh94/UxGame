using Assets.Editor.Timeline;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ux.Editor.Timeline.Animation;

namespace Ux.Editor.Timeline
{
    public sealed class TimelineAssetEditorSource : ITimelineEditorSource
    {
        readonly TimelineAsset asset;
        readonly string id;
        readonly Action<string, UnityEngine.Object, Action> registerUndo;
        readonly Action afterSave;
        readonly Action completeUndo;
        readonly Action beforeSave;
        readonly Func<bool> canEdit;
        readonly List<TimelineAssetEditorTrack> tracks = new();
        readonly Dictionary<object, HashSet<Action>> bindings = new();

        public TimelineAssetEditorSource(TimelineAsset asset, Action<string, UnityEngine.Object, Action> registerUndo = null, Action save = null, Func<bool> canEdit = null, Action completeUndo = null, Action beforeSave = null)
        {
            this.asset = asset ?? throw new ArgumentNullException(nameof(asset));
            this.registerUndo = registerUndo;
            afterSave = save;
            this.canEdit = canEdit;
            this.completeUndo = completeUndo;
            this.beforeSave = beforeSave;
            asset.ValidateData();
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var guid = string.IsNullOrEmpty(assetPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(assetPath);
            id = string.IsNullOrEmpty(guid)
                ? $"timeline-instance:{asset.GetInstanceID()}"
                : $"timeline:{guid}";
            RebuildAdapters();
        }

        public string Id => id;
        public TimelineEditorSourceRole Role => TimelineEditorSourceRole.Presentation;
        public UnityEngine.Object UndoOwner => asset;
        public bool CanEdit => canEdit?.Invoke() ?? true;
        public bool CanSetFrameRate => true;
        public int FrameRate => asset.FrameRate;
        public int DurationFrames => asset.DurationFrames;
        public int TrackCount => tracks.Count;
        public IReadOnlyList<TimelineAssetEditorTrack> Tracks => tracks;
        IReadOnlyList<ITimelineEditorTrack> ITimelineEditorSource.Tracks => tracks;

        public event Action StructureChanged;
        public event Action Changed;

        public string GetTrackDisplayName(Type trackType)
        {
            return trackType?.GetAttribute<TLTrackAttribute>()?.Lb ?? trackType?.Name ?? string.Empty;
        }

        public IReadOnlyList<Type> GetTrackTypes()
        {
            var result = new List<Type>();
            foreach (var trackType in YooAsset.Editor.EditorTools.GetAssignableTypes(typeof(TimelineTrackAsset)))
            {
                if (trackType.IsAbstract ||
                    trackType.GetAttribute<TLTrackAttribute>() == null ||
                    trackType.GetAttribute<TLTrackClipTypeAttribute>() == null)
                {
                    continue;
                }
                result.Add(trackType);
            }
            return result;
        }

        public TimelineAssetEditorTrack AddTrack(Type trackType)
        {
            if (!CanEdit || trackType == null || !typeof(TimelineTrackAsset).IsAssignableFrom(trackType) ||
                Activator.CreateInstance(trackType) is not TimelineTrackAsset trackAsset)
            {
                return null;
            }

            var attribute = trackType.GetAttribute<TLTrackAttribute>();
            trackAsset.trackName = attribute?.Lb ?? trackType.Name;
            trackAsset.ValidateData();
            RegisterUndo("timeline_add_track");
            asset.tracks.Add(trackAsset);
            asset.ValidateData();

            var track = new TimelineAssetEditorTrack(this, trackAsset);
            tracks.Add(track);
            Save();
            StructureChanged?.Invoke();
            Changed?.Invoke();
            return track;
        }

        ITimelineEditorTrack ITimelineEditorSource.AddTrack(Type trackType)
        {
            return AddTrack(trackType);
        }

        public bool SetFrameRate(int frameRate)
        {
            if (!CanEdit || frameRate == asset.FrameRate)
            {
                return false;
            }

            RegisterUndo("timeline_frame_rate");
            if (!asset.SetFrameRate(frameRate))
            {
                completeUndo?.Invoke();
                return false;
            }

            Save();
            Changed?.Invoke();
            return true;
        }

        public bool RemoveTrack(TimelineAssetEditorTrack track)
        {
            if (!CanEdit || track == null || !ReferenceEquals(track.Source, this) ||
                !asset.tracks.Contains(track.Asset))
            {
                return false;
            }

            RegisterUndo("timeline_remove_track");
            asset.tracks.Remove(track.Asset);
            tracks.Remove(track);
            Save();
            StructureChanged?.Invoke();
            Changed?.Invoke();
            return true;
        }

        internal void RenameTrack(TimelineAssetEditorTrack track, string name)
        {
            if (!CanEdit || track == null || track.Asset.trackName == name)
            {
                return;
            }

            RegisterUndo("timeline_rename_track");
            track.Asset.trackName = name;
            Save();
            Run(track);
            Changed?.Invoke();
        }

        internal TimelineAssetEditorClip AddClip(TimelineAssetEditorTrack track, Type clipType, int startFrame, string assetPath)
        {
            if (!CanEdit)
            {
                return null;
            }
            var clipAsset = CreateClipAsset(clipType, startFrame, assetPath);
            return clipAsset == null ? null : AddClip(track, clipAsset);
        }

        public TimelineAssetEditorClip AddClip(TimelineAssetEditorTrack track, TimelineClipAsset clipAsset)
        {
            if (!CanEdit || track == null || clipAsset == null || !ReferenceEquals(track.Source, this) ||
                track.Asset.clips.Contains(clipAsset))
            {
                return null;
            }

            clipAsset.ValidateData();
            RegisterUndo("track_add_clip_item");
            track.Asset.clips.Add(clipAsset);
            asset.ValidateData();

            var clip = track.AddAdapter(clipAsset);
            track.UpdateMixData();
            Save();
            Changed?.Invoke();
            return clip;
        }

        internal bool RemoveClip(TimelineAssetEditorTrack track, TimelineAssetEditorClip clip)
        {
            if (!CanEdit || track == null || clip == null || !ReferenceEquals(track.Source, this) ||
                !ReferenceEquals(clip.Track, track) || !track.Asset.clips.Contains(clip.Asset))
            {
                return false;
            }

            RegisterUndo("track_remove_clip_item");
            track.Asset.clips.Remove(clip.Asset);
            track.RemoveAdapter(clip);
            track.UpdateMixData();
            Save();
            Changed?.Invoke();
            return true;
        }

        internal void RenameClip(TimelineAssetEditorClip clip, string name)
        {
            if (!CanEdit || clip == null || clip.Asset.clipName == name)
            {
                return;
            }

            RegisterUndo("timeline_rename_clip");
            clip.Asset.clipName = name;
            Save();
            Run(clip);
            Changed?.Invoke();
        }

        internal void BeginClipDrag(TimelineAssetEditorClip clip)
        {
            if (!CanEdit || clip == null)
            {
                return;
            }
            RegisterUndo("drag_track");
        }

        internal bool RecordEdit(string key)
        {
            if (!CanEdit)
            {
                return false;
            }
            RegisterUndo(key);
            return true;
        }

        internal void CommitClipEdit(TimelineAssetEditorClip clip)
        {
            if (!CanEdit)
            {
                return;
            }
            clip?.Asset.ValidateData();
            clip?.Track.UpdateMixData();
            Save();
            if (clip != null)
            {
                Run(clip);
            }
            Changed?.Invoke();
        }

        public void CommitInspectorChange(object assetObject)
        {
            if (!CanEdit)
            {
                return;
            }
            switch (assetObject)
            {
                case TimelineTrackAsset track:
                    track.ValidateData();
                    Run(FindTrack(track));
                    break;
                case TimelineClipAsset clip:
                    clip.ValidateData();
                    var editorClip = FindClip(clip);
                    editorClip?.Track.UpdateMixData();
                    Run(editorClip);
                    break;
            }
            asset.ValidateData();
            Save();
            Changed?.Invoke();
        }

        public void Save()
        {
            SaveInternal(true, true);
        }

        void SaveInternal(bool runBeforeSave, bool completePendingUndo)
        {
            asset.ValidateData();
            // beforeSave 必须在 completeUndo 之前记录其它 owner，才能与当前 Timeline 编辑共享同一 Undo group。
            if (runBeforeSave)
            {
                beforeSave?.Invoke();
            }
            if (completePendingUndo)
            {
                completeUndo?.Invoke();
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            afterSave?.Invoke();
        }

        public TimelineInspectorBase CreateInspector(object selection)
        {
            switch (selection)
            {
                case TimelineAssetEditorTrack track when track.Asset is AnimationTrackAsset animationTrack:
                    return new TLAnimTrackInspector(this, track, animationTrack);
                case TimelineAssetEditorClip clip when clip.Asset is AnimationClipAsset animationClip:
                    return new TLAnimClipInspector(this, clip, animationClip);
                case TimelineAssetEditorTrack track when track.Asset is ParticleAssetTrack particleTrack:
                    return new ParticleTrackInspector(this, track, particleTrack);
                case TimelineAssetEditorClip clip:
                    return new BasicClipInspector(this, clip, clip.Asset);
                case TimelineAssetEditorTrack track:
                    return new BasicTrackInspector(this, track, track.Asset);
                default:
                    return null;
            }
        }

        public void Bind(object selection, Action action)
        {
            if (selection == null || action == null)
            {
                return;
            }
            if (!bindings.TryGetValue(selection, out var actions))
            {
                actions = new HashSet<Action>();
                bindings.Add(selection, actions);
            }
            actions.Add(action);
        }

        public void Unbind(object selection, Action action)
        {
            if (selection != null && bindings.TryGetValue(selection, out var actions))
            {
                actions.Remove(action);
            }
        }

        internal void Run(object selection)
        {
            if (selection != null && bindings.TryGetValue(selection, out var actions))
            {
                foreach (var action in new List<Action>(actions))
                {
                    action.Invoke();
                }
            }
        }

        TimelineClipAsset CreateClipAsset(Type clipType, int startFrame, string assetPath)
        {
            if (clipType == null || !typeof(TimelineClipAsset).IsAssignableFrom(clipType) ||
                Activator.CreateInstance(clipType) is not TimelineClipAsset clipAsset)
            {
                return null;
            }

            clipAsset.StartFrame = Mathf.Max(0, startFrame);
            clipAsset.EndFrame = clipAsset.StartFrame + FrameRate;
            clipAsset.clipName = clipType.Name;

            if (clipAsset is AnimationClipAsset animationClipAsset && !string.IsNullOrEmpty(assetPath))
            {
                var animation = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (animation != null)
                {
                    animationClipAsset.clip = animation;
                    animationClipAsset.EndFrame = clipAsset.StartFrame +
                        Mathf.Max(1, Mathf.RoundToInt(animation.length * FrameRate));
                    animationClipAsset.clipName = animation.name;
                }
            }

            clipAsset.ValidateData();
            return clipAsset;
        }

        void RegisterUndo(string key)
        {
            if (registerUndo != null)
            {
                registerUndo.Invoke(key, asset, RefreshAfterUndo);
            }
            else if (key.IndexOf("drag", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Undo.RegisterCompleteObjectUndo(asset, key);
            }
            else
            {
                Undo.RecordObject(asset, key);
            }
        }

        public void RefreshAfterUndo()
        {
            asset.ValidateData();
            RebuildAdapters();
            SaveInternal(false, false);
            StructureChanged?.Invoke();
            Changed?.Invoke();
        }

        void RebuildAdapters()
        {
            bindings.Clear();
            tracks.Clear();
            if (asset.tracks == null)
            {
                return;
            }
            foreach (var track in asset.tracks)
            {
                if (track != null)
                {
                    tracks.Add(new TimelineAssetEditorTrack(this, track));
                }
            }
        }

        TimelineAssetEditorTrack FindTrack(TimelineTrackAsset trackAsset)
        {
            return tracks.Find(track => ReferenceEquals(track.Asset, trackAsset));
        }

        TimelineAssetEditorClip FindClip(TimelineClipAsset clipAsset)
        {
            foreach (var track in tracks)
            {
                var clip = track.FindClip(clipAsset);
                if (clip != null)
                {
                    return clip;
                }
            }
            return null;
        }
    }
}
