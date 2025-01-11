using Assets.Editor.Timeline;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline.Animation
{
    public partial class TLAnimTrackInspector : TimelineInspectorBase
    {
        AnimationTrackAsset _asset;
        public TLAnimTrackInspector(AnimationTrackAsset asset) : base(asset)
        {
            CreateChildren();
            Add(root);
            _asset = asset;
            ofAnimator.objectType = typeof(Animator);
            ofAvatarMask.objectType = typeof(AvatarMask);
            OnFreshView();
        }
        partial void _OnOfAvatarMaskChanged(ChangeEvent<Object> e)
        {
            _asset.avatarMask = e.newValue as AvatarMask;
            TimelineWindow.Run(_asset);
        }
        partial void _OnTgAdditiveChanged(ChangeEvent<bool> e)
        {
            _asset.isAdditive = e.newValue;
            TimelineWindow.Run(_asset);
        }
        partial void _OnTxtNameChanged(ChangeEvent<string> e)
        {
            _asset.trackName = e.newValue;
            TimelineWindow.Run(_asset);
            if (!string.IsNullOrEmpty(_asset.trackName) && ofAnimator.value != null)
            {
                TimelineWindow.RefreshBinds(_asset.trackName, ofAnimator.value);
            }
        }
        partial void _OnOfAnimatorChanged(ChangeEvent<Object> e)
        {            
            if (!string.IsNullOrEmpty(_asset.trackName) && ofAnimator.value != null)
            {
                TimelineWindow.RefreshBinds(_asset.trackName, ofAnimator.value);
            }
            TimelineWindow.RefreshEntity();
        }

        protected override void OnFreshView()
        {
            txtName.SetValueWithoutNotify(_asset.trackName);
            ofAvatarMask.SetValueWithoutNotify(_asset.avatarMask);
            var animator = TimelineWindow.Timeline.GetBindObj<Animator>(_asset.trackName);
            if (animator != null)
            {
                ofAnimator.SetValueWithoutNotify(animator);
            }
            tgAdditive.SetValueWithoutNotify(_asset.isAdditive);
            tgAdditive.style.display = _asset.avatarMask == null ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

}
