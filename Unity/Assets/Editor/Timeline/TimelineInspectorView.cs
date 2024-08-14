using System;
using UnityEngine.UIElements;
using Ux;
using Ux.Editor.Timeline.Animation;

namespace Assets.Editor.Timeline
{

    public class TimelineInspectorBase : VisualElement
    {
        object _asset;
        Func<bool> _callback;
        public TimelineInspectorBase(object asset)
        {
            _asset = asset;
        }
        public bool IsSame(object obj)
        {
            return _asset == obj;
        }
        public void SetCallBack(Func<bool> callback)
        {
            _callback =callback;
        }
        protected bool CallBack()
        {
           return _callback.Invoke();
        }

    }
    public class TimelineInspectorView
    {
        VisualElement root;
        TimelineInspectorBase current;
        public TimelineInspectorView(VisualElement root)
        {
            this.root = root;
        }

        public void FreshInspector(object asset, Func<bool> callback)
        {
            if (current != null)
            {
                if (current.IsSame(asset))
                {
                    return;
                }
                root.Remove(current);
                current = null;
            }
            if (asset is AnimationTrackAsset ata)
            {
                current = new TLAnimTrackInspector(ata);
            }
            else if (asset is AnimationClipAsset aca)
            {
                current = new TLAnimClipInspector(aca);
            }
            if (current != null)
            {
                current.SetCallBack(callback);
                root.Add(current);
            }
        }

    }
}