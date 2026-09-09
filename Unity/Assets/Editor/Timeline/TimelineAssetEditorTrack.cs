using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux.Editor.Timeline
{
    public sealed class TimelineAssetEditorTrack : ITimelineEditorTrack
    {
        readonly List<TimelineAssetEditorClip> clips = new();

        internal TimelineAssetEditorTrack(TimelineAssetEditorSource source, TimelineTrackAsset asset)
        {
            Source = source;
            Asset = asset;
            foreach (var clip in asset.clips)
            {
                if (clip != null)
                {
                    clips.Add(new TimelineAssetEditorClip(this, clip));
                }
            }
        }

        internal TimelineTrackAsset Asset { get; }
        internal TimelineAssetEditorSource Source { get; }
        ITimelineEditorSource ITimelineEditorTrack.Source => Source;
        public string Id => Asset.Id;
        public string Name => Asset.trackName;
        public string TypeName => Asset.GetType().Name;
        public string DisplayTypeName => Asset.GetType().GetAttribute<TLTrackAttribute>()?.Lb ?? TypeName;
        public Color Color => Asset.GetType().GetAttribute<TLTrackAttribute>()?.Color ??
                              new Color(0.4f, 0.7f, 0.7f);
        public int EndFrame => Asset.GetEndFrame();
        public bool CanRename => true;
        public bool CanRemove => true;
        public bool CanCreateClip => true;
        public IReadOnlyList<TimelineAssetEditorClip> Clips => clips;
        IReadOnlyList<ITimelineEditorClip> ITimelineEditorTrack.Clips => clips;

        Type ClipType => Asset.GetType().GetAttribute<TLTrackClipTypeAttribute>()?.ClipType;

        public void Rename(string name)
        {
            Source.RenameTrack(this, name);
        }

        public bool Remove()
        {
            return Source.RemoveTrack(this);
        }

        public TimelineAssetEditorClip CreateClip(string assetPath = null)
        {
            return Source.AddClip(this, ClipType, EndFrame, assetPath);
        }

        ITimelineEditorClip ITimelineEditorTrack.CreateClip(string assetPath)
        {
            return CreateClip(assetPath);
        }

        public TimelineAssetEditorClip AddClip(Type clipType, int startFrame = 0, string assetPath = null)
        {
            return Source.AddClip(this, clipType, startFrame, assetPath);
        }

        public TimelineAssetEditorClip AddClip(TimelineClipAsset clipAsset)
        {
            return Source.AddClip(this, clipAsset);
        }

        public bool RemoveClip(TimelineAssetEditorClip clip)
        {
            return Source.RemoveClip(this, clip);
        }

        public bool RemoveClip(ITimelineEditorClip clip)
        {
            return clip is TimelineAssetEditorClip assetClip && RemoveClip(assetClip);
        }

        public void RecordUndo(string key)
        {
            Source.RecordEdit(key);
        }

        public void Bind(Action action)
        {
            Source.Bind(this, action);
        }

        public void Unbind(Action action)
        {
            Source.Unbind(this, action);
        }

        public bool IsLayoutValid()
        {
            var sideMasks = new Dictionary<TimelineAssetEditorClip, int>();
            foreach (var first in clips)
            {
                foreach (var second in clips)
                {
                    if (ReferenceEquals(first, second))
                    {
                        continue;
                    }
                    var intersection = Intersect(first.Asset, second.Asset);
                    if (intersection == InvalidIntersection)
                    {
                        return false;
                    }
                    if (intersection == 0)
                    {
                        continue;
                    }

                    var sideMask = intersection > 0 ? 1 : 2;
                    if (sideMasks.TryGetValue(first, out var mask))
                    {
                        if ((mask & sideMask) != 0)
                        {
                            return false;
                        }
                        sideMasks[first] = mask | sideMask;
                    }
                    else
                    {
                        sideMasks.Add(first, sideMask);
                    }
                }
            }
            return true;
        }

        public void UpdateMixData()
        {
            foreach (var first in clips)
            {
                first.Asset.InFrame = 0;
                first.Asset.OutFrame = 0;
                AnimationClipAsset animation = null;
                if (first.Asset is AnimationClipAsset animationClip)
                {
                    animation = animationClip;
                    var previousFrame = 0;
                    var nextFrame = int.MaxValue;
                    foreach (var second in clips)
                    {
                        if (ReferenceEquals(first, second))
                        {
                            continue;
                        }
                        if (first.StartFrame > second.StartFrame && previousFrame < second.EndFrame)
                        {
                            previousFrame = second.EndFrame;
                        }
                        if (first.StartFrame < second.StartFrame && nextFrame > second.StartFrame)
                        {
                            nextFrame = second.StartFrame;
                        }
                    }
                    animation.PreFrame = previousFrame;
                    animation.PostFrame = nextFrame;
                }

                foreach (var second in clips)
                {
                    if (ReferenceEquals(first, second))
                    {
                        continue;
                    }
                    var intersection = Intersect(first.Asset, second.Asset);
                    if (intersection == 1)
                    {
                        first.Asset.OutFrame = second.StartFrame;
                    }
                    else if (intersection == -1)
                    {
                        first.Asset.InFrame = second.EndFrame;
                    }

                    if (intersection != InvalidIntersection && animation != null &&
                        second.Asset is AnimationClipAsset otherAnimation)
                    {
                        Extrapolate(animation, otherAnimation);
                    }
                }
            }
        }

        internal TimelineAssetEditorClip AddAdapter(TimelineClipAsset clipAsset)
        {
            var clip = new TimelineAssetEditorClip(this, clipAsset);
            clips.Add(clip);
            return clip;
        }

        internal void RemoveAdapter(TimelineAssetEditorClip clip)
        {
            clips.Remove(clip);
        }

        internal TimelineAssetEditorClip FindClip(TimelineClipAsset clipAsset)
        {
            return clips.Find(clip => ReferenceEquals(clip.Asset, clipAsset));
        }

        const int InvalidIntersection = -1000;

        static int Intersect(TimelineClipAsset first, TimelineClipAsset second)
        {
            // Clip ranges are half-open: touching [a,b) and [b,c) do not overlap.
            if (first.EndFrame <= second.StartFrame || second.EndFrame <= first.StartFrame)
            {
                return 0;
            }
            if ((first.StartFrame <= second.StartFrame && first.EndFrame >= second.EndFrame) ||
                (second.StartFrame <= first.StartFrame && second.EndFrame >= first.EndFrame))
            {
                return InvalidIntersection;
            }
            return first.StartFrame < second.StartFrame ? 1 : -1;
        }

        static void Extrapolate(AnimationClipAsset first, AnimationClipAsset second)
        {
            if (first.StartFrame == 0 || first.InFrame > 0 ||
                (first.StartFrame > second.StartFrame &&
                 second.post != AnimationClipAsset.PostExtrapolate.None) ||
                first.PreFrame == first.StartFrame)
            {
                first.PreFrame = -1;
            }
            if (first.OutFrame > 0)
            {
                first.PostFrame = -1;
            }
        }
    }
}
