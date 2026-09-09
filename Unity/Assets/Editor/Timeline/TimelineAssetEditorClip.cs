using System;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Timeline
{
    public sealed class TimelineAssetEditorClip : ITimelineEditorClip
    {
        internal TimelineAssetEditorClip(TimelineAssetEditorTrack track, TimelineClipAsset asset)
        {
            Track = track;
            Asset = asset;
        }

        internal TimelineClipAsset Asset { get; }
        public TimelineAssetEditorTrack Track { get; }
        ITimelineEditorTrack ITimelineEditorClip.Track => Track;
        public string Id => Asset.Id;
        public string Name => Asset.clipName;
        public string TypeName => Asset.GetType().Name;
        public int StartFrame => Asset.StartFrame;
        public int EndFrame => Asset.EndFrame;
        public int InFrame => Asset.InFrame;
        public int OutFrame => Asset.OutFrame;
        public int DurationFrames => Asset.DurationFrames;
        public bool CanFitAnimationDuration =>
            Asset is AnimationClipAsset animation && animation.clip != null;

        public void Rename(string name)
        {
            Track.Source.RenameClip(this, name);
        }

        public void BeginDrag()
        {
            Track.Source.BeginClipDrag(this);
        }

        public void Drag(DragStatus status, int nowFrame, int lastFrame)
        {
            if (!Track.Source.CanEdit)
            {
                return;
            }
            switch (status)
            {
                case DragStatus.Left:
                    Asset.StartFrame = Mathf.Clamp(nowFrame, 0, Asset.EndFrame - 1);
                    break;
                case DragStatus.Right:
                    Asset.EndFrame = Mathf.Max(Asset.StartFrame + 1, nowFrame);
                    break;
                case DragStatus.Move:
                    var offset = nowFrame - lastFrame;
                    if (Asset.StartFrame + offset < 0)
                    {
                        offset = -Asset.StartFrame;
                    }
                    Asset.StartFrame += offset;
                    Asset.EndFrame += offset;
                    break;
            }
            Track.Source.Run(this);
        }

        public void SetFrames(int startFrame, int endFrame, bool save = true)
        {
            if (!Track.Source.CanEdit)
            {
                return;
            }
            if (save)
            {
                RecordUndo("timeline_clip_frames");
            }
            Asset.StartFrame = Mathf.Max(0, startFrame);
            Asset.EndFrame = Mathf.Max(Asset.StartFrame + 1, endFrame);
            if (save)
            {
                Track.Source.CommitClipEdit(this);
            }
            else
            {
                Track.Source.Run(this);
            }
        }

        public void CommitEdit()
        {
            Track.Source.CommitClipEdit(this);
        }

        public bool TryAssignAnimation(string assetPath)
        {
            return TryAssignAnimation(AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath));
        }

        public bool TryAssignAnimation(AnimationClip animation)
        {
            if (!Track.Source.CanEdit || animation == null ||
                Asset is not AnimationClipAsset animationAsset)
            {
                return false;
            }

            var oldClip = animationAsset.clip;
            var oldName = animationAsset.clipName;
            var oldEndFrame = animationAsset.EndFrame;
            var targetEndFrame = animationAsset.StartFrame +
                Mathf.Max(1, Mathf.RoundToInt(animation.length * Track.Source.FrameRate));

            // 先做无通知的布局探测，避免把非法重叠写入资产；确认后再登记 Undo 并正式提交。
            animationAsset.clip = animation;
            animationAsset.clipName = animation.name;
            animationAsset.EndFrame = targetEndFrame;
            var layoutValid = Track.IsLayoutValid();
            animationAsset.clip = oldClip;
            animationAsset.clipName = oldName;
            animationAsset.EndFrame = oldEndFrame;
            if (!layoutValid)
            {
                return false;
            }

            RecordUndo("timeline_clip_animation");
            animationAsset.clip = animation;
            animationAsset.clipName = animation.name;
            animationAsset.EndFrame = targetEndFrame;
            CommitEdit();
            return true;
        }

        public void RecordUndo(string key)
        {
            Track.Source.RecordEdit(key);
        }

        public void Bind(Action action)
        {
            Track.Source.Bind(this, action);
        }

        public void Unbind(Action action)
        {
            Track.Source.Unbind(this, action);
        }

        public bool FitAnimationDuration()
        {
            if (!Track.Source.CanEdit || Asset is not AnimationClipAsset animationAsset || animationAsset.clip == null)
            {
                return false;
            }

            var oldEndFrame = Asset.EndFrame;
            var targetEndFrame = Asset.StartFrame +
                Mathf.Max(1, Mathf.RoundToInt(animationAsset.clip.length * Track.Source.FrameRate));
            Asset.EndFrame = targetEndFrame;
            var layoutValid = Track.IsLayoutValid();
            Asset.EndFrame = oldEndFrame;
            if (!layoutValid)
            {
                return false;
            }

            RecordUndo("timeline_clip_fit_duration");
            Asset.EndFrame = targetEndFrame;
            CommitEdit();
            return true;
        }
    }
}
