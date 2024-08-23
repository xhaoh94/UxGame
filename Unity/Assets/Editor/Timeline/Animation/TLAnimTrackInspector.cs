using Assets.Editor.Timeline;
using UnityEditor.VersionControl;
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
        partial void _OnTxtNameChanged(ChangeEvent<string> e)
        {
            _asset.trackName = e.newValue;
            TimelineWindow.Run(_asset);
        }

        protected override void OnFreshView()
        {
            txtName.SetValueWithoutNotify(_asset.trackName);
        }
    }

}
