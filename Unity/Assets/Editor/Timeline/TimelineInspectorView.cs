using System;
using UnityEngine.UIElements;
using Ux;
using Ux.Editor.Timeline;
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
            TimelineWindow.Bind(asset, OnFreshView);
        }
        public bool IsSame(object obj)
        {
            return _asset == obj;
        }
        public void SetChcekValid(Func<bool> callback)
        {
            _callback = callback;
        }
        protected bool ChcekValid()
        {
            if (_callback != null)
            {
                return _callback.Invoke();
            }
            return true;
        }

        public void Release()
        {
            TimelineWindow.UnBind(_asset, OnFreshView);
        }

        protected virtual void OnFreshView() { }
    }
    public class TimelineInspectorView
    {
        VisualElement root;
        TimelineInspectorBase current;
        public TimelineInspectorView(VisualElement root)
        {
            this.root = root;
        }

        public void FreshInspector(object asset, Func<bool> chcekValid)
        {
            if (current != null)
            {
                if (current.IsSame(asset))
                {
                    return;
                }
                current.Release();
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
                current.SetChcekValid(chcekValid);
                root.Add(current);
            }
        }

    }
}