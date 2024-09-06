using Assets.Editor.Timeline;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline.Animation
{
    public partial class TLAnimTrackInspector : TimelineInspectorBase
    {
        AnimationTrackAsset _asset;
        public TLAnimTrackInspector(AnimationTrackAsset asset):base(asset)
        {
            CreateChildren();
            Add(root);
            _asset = asset;
            OnFreshView();
        }
        partial void _OnInputLayerChanged(ChangeEvent<int> e)
        {
            _asset.layer = e.newValue;
            TimelineWindow.Run(_asset);
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
        }

        protected override void OnFreshView()
        {
            txtName.SetValueWithoutNotify(_asset.trackName);
            inputLayer.SetValueWithoutNotify(_asset.layer);
            ofAvatarMask.SetValueWithoutNotify(_asset.avatarMask);
            tgAdditive.SetValueWithoutNotify(_asset.isAdditive);
            tgAdditive.style.display = _asset.avatarMask == null ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

}
