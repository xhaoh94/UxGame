using Assets.Editor.Timeline;
using UnityEngine;
using UnityEngine.UIElements;
using static Ux.AnimationClipAsset;

namespace Ux.Editor.Timeline.Animation
{
    public partial class TLAnimClipInspector : TimelineInspectorBase
    {
        readonly ITimelineEditorClip clip;
        readonly AnimationClipAsset asset;
        int startFrame;
        int endFrame;

        public TLAnimClipInspector(
            ITimelineEditorSource source,
            ITimelineEditorClip clip,
            AnimationClipAsset asset) : base(source, clip, asset)
        {
            CreateChildren();
            Add(root);
            this.clip = clip;
            this.asset = asset;
            txtStartFrame.RegisterCallback<FocusInEvent>(_TxtFBlur);
            txtStartFrame.RegisterCallback<FocusOutEvent>(_TxtSBlur);
            txtStartFrame.RegisterCallback<MouseUpEvent>(_TxtUpEvent);

            txtStartFrame.labelElement.style.minWidth = 15;
            txtEndFrame.labelElement.style.minWidth = 15;
            ofClip.objectType = typeof(AnimationClip);
            OnFreshView();
        }

        protected override void OnFreshView()
        {
            txtName.SetValueWithoutNotify(clip.Name);
            ofClip.SetValueWithoutNotify(asset.clip);

            lbStartTime.text = $" 秒 {clip.StartFrame / (float)Source.FrameRate}";
            txtStartFrame.SetValueWithoutNotify(clip.StartFrame);

            lbEndTime.text = $" 秒 {clip.EndFrame / (float)Source.FrameRate}";
            txtEndFrame.SetValueWithoutNotify(clip.EndFrame);

            lbInTime.text = $"秒  {clip.InFrame / (float)Source.FrameRate}";
            lbInFrame.text = $"帧  {clip.InFrame}";

            lbOutTime.text = $"秒  {clip.OutFrame / (float)Source.FrameRate}";
            lbOutFrame.text = $"帧  {clip.OutFrame}";

            lbDurationTime.text = $"秒  {clip.DurationFrames / (float)Source.FrameRate}";
            lbDurationFrame.text = $"帧  {clip.DurationFrames}";

            pre.Init(asset.pre);
            post.Init(asset.post);
            pre.style.display = asset.PreFrame >= 0 && asset.PreFrame < clip.StartFrame
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            post.style.display = asset.PostFrame >= 0 ? DisplayStyle.Flex : DisplayStyle.None;

            btnDuration.style.display = DisplayStyle.None;
            if (asset.clip != null)
            {
                var targetFrames = asset.clip.length * Source.FrameRate;
                if (Mathf.RoundToInt(targetFrames) != clip.DurationFrames)
                {
                    btnDuration.style.display = DisplayStyle.Flex;
                }
            }
        }

        void _TxtFBlur(FocusInEvent evt)
        {
            startFrame = clip.StartFrame;
            endFrame = clip.EndFrame;
        }

        void _TxtUpEvent(MouseUpEvent evt)
        {
            CheckValid();
        }

        void _TxtSBlur(FocusOutEvent evt)
        {
            CheckValid();
        }

        void CheckValid()
        {
            if (!ChcekValid())
            {
                clip.SetFrames(startFrame, endFrame, false);
                CommitChange();
            }
        }

        partial void _OnTxtStartFrameChanged(ChangeEvent<int> evt)
        {
            if (!Source.CanEdit) return;
            var oldStart = clip.StartFrame;
            var oldEnd = clip.EndFrame;
            var nextStart = Mathf.Max(0, evt.newValue);
            var nextEnd = clip.EndFrame;
            if (tgMove.value)
            {
                nextEnd += nextStart - clip.StartFrame;
            }

            clip.RecordUndo("timeline_clip_start_frame");
            clip.SetFrames(nextStart, nextEnd, false);
            if (!ChcekValid())
            {
                clip.SetFrames(oldStart, oldEnd, false);
            }
            CommitChange();
        }

        partial void _OnTxtEndFrameChanged(ChangeEvent<int> evt)
        {
            if (!Source.CanEdit) return;
            var oldStart = clip.StartFrame;
            var oldEnd = clip.EndFrame;
            clip.RecordUndo("timeline_clip_end_frame");
            if (tgMove.value)
            {
                var offset = evt.newValue - clip.EndFrame;
                if (clip.StartFrame + offset < 0)
                {
                    offset = -clip.StartFrame;
                }
                clip.SetFrames(clip.StartFrame + offset, clip.EndFrame + offset, false);
            }
            else
            {
                clip.SetFrames(clip.StartFrame, evt.newValue, false);
            }

            if (!ChcekValid())
            {
                clip.SetFrames(oldStart, oldEnd, false);
            }
            CommitChange();
        }

        partial void _OnTxtNameChanged(ChangeEvent<string> evt)
        {
            clip.Rename(evt.newValue);
        }

        partial void _OnOfClipChanged(ChangeEvent<Object> evt)
        {
            if (Source.CanEdit && evt.newValue is AnimationClip animation &&
                clip.TryAssignAnimation(animation))
            {
                TimelineWindow.RefreshEntity?.Invoke();
                TimelineWindow.wnd?.clipView?.RefreshLayout();
            }
            else
            {
                OnFreshView();
            }
        }

        partial void _OnBtnDurationClick()
        {
            if (clip.FitAnimationDuration())
            {
                TimelineWindow.RefreshEntity?.Invoke();
                TimelineWindow.wnd?.clipView?.RefreshLayout();
            }
        }

        partial void _OnPreChanged(ChangeEvent<System.Enum> evt)
        {
            if (!Source.CanEdit) return;
            clip.RecordUndo("timeline_clip_pre_extrapolate");
            asset.pre = (PostExtrapolate)evt.newValue;
            CommitChange();
        }

        partial void _OnPostChanged(ChangeEvent<System.Enum> evt)
        {
            if (!Source.CanEdit) return;
            clip.RecordUndo("timeline_clip_post_extrapolate");
            asset.post = (PostExtrapolate)evt.newValue;
            CommitChange();
        }
    }
}
