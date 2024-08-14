using Assets.Editor.Timeline;
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
            txtName.SetValueWithoutNotify(asset.trackName);
        }
        partial void _OnTxtNameChanged(ChangeEvent<string> e)
        {
            _asset.trackName = e.newValue;
            CallBack();
        }

    }

}
