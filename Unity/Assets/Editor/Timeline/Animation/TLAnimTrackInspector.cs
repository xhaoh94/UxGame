using Assets.Editor.Timeline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.Timeline.Animation
{
    public partial class TLAnimTrackInspector : TimelineInspectorBase
    {
        readonly ITimelineEditorTrack track;
        readonly AnimationTrackAsset asset;

        public TLAnimTrackInspector(
            ITimelineEditorSource source,
            ITimelineEditorTrack track,
            AnimationTrackAsset asset) : base(source, track, asset)
        {
            CreateChildren();
            Add(root);
            this.track = track;
            this.asset = asset;
            ofAnimator.objectType = typeof(Animator);
            ofAnimator.allowSceneObjects = true;
            ofAvatarMask.objectType = typeof(AvatarMask);
            OnFreshView();
        }

        partial void _OnOfAvatarMaskChanged(ChangeEvent<Object> evt)
        {
            if (!Source.CanEdit) return;
            track.RecordUndo("timeline_track_avatar_mask");
            asset.avatarMask = evt.newValue as AvatarMask;
            CommitChange();
        }

        partial void _OnTgAdditiveChanged(ChangeEvent<bool> evt)
        {
            if (!Source.CanEdit) return;
            track.RecordUndo("timeline_track_additive");
            asset.isAdditive = evt.newValue;
            CommitChange();
        }

        partial void _OnTxtNameChanged(ChangeEvent<string> evt)
        {
            track.Rename(evt.newValue);
            if (ofAnimator.value != null)
            {
                TimelineWindow.RefreshBinds?.Invoke(asset, ofAnimator.value);
            }
        }

        partial void _OnOfAnimatorChanged(ChangeEvent<Object> evt)
        {
            TimelineWindow.RefreshBinds?.Invoke(asset, evt.newValue);
            TimelineWindow.RefreshEntity?.Invoke();
        }

        protected override void OnFreshView()
        {
            txtName.SetValueWithoutNotify(track.Name);
            ofAvatarMask.SetValueWithoutNotify(asset.avatarMask);
            var animator = TimelineWindow.Timeline?.GetBinding<Animator>(asset);
            ofAnimator.SetValueWithoutNotify(animator);
            tgAdditive.SetValueWithoutNotify(asset.isAdditive);
            tgAdditive.style.display = asset.avatarMask == null
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }
}
