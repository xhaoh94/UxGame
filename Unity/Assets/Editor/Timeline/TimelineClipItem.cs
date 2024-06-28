using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline
{
    public class TimelineClipItem : VisualElement
    {
        TimelineClipAsset asset;
        TimelineWindow window;
        TimelineTrackItem trackItem;
        VisualElement content;
        VisualElement left;
        Label lbType;
        VisualElement right;
        public TimelineClipItem()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/TimelineClipItem.uxml");
            visualTree.CloneTree(this);
            content = this.Q<VisualElement>("content");
            left = this.Q<VisualElement>("left");

            lbType = this.Q<Label>("lbType");
            right = this.Q<VisualElement>("right");

        }
        public void Init(TimelineClipAsset asset, TimelineTrackItem track, TimelineWindow window)
        {
            this.asset = asset;
            this.trackItem = track;
            this.window = window;
            ElementDrag.Add(left, window.clipView, OnSizeStart, OnLeftDrag, OnSizeEnd);
            ElementDrag.Add(right, window.clipView, OnSizeStart, OnRightDrag, OnSizeEnd);
            ElementDrag.Add(content, window.clipView, OnStart, OnDrag, OnEnd);
            UpdateView();
        }

        void OnDrag(bool left)
        {
            var frame = window.clipView.GetFrameByMousePosition();

            if (left)
            {
                if (frame < 0)
                {
                    frame = 0;
                }
                if (frame >= asset.EndFrame)
                {
                    frame = asset.EndFrame - 1;
                }
                asset.StartFrame = frame;
            }
            else
            {
                if (frame < asset.StartFrame)
                {
                    frame = asset.StartFrame;
                }

                asset.EndFrame = frame;
            }
            UpdateView();
        }
        void OnSizeStart()
        {

        }
        void OnSizeEnd()
        {

        }

        void OnLeftDrag(Vector2 e)
        {
            OnDrag(true);
        }
        void OnRightDrag(Vector2 e)
        {
            OnDrag(false);
        }

        int startFrame;
        void OnStart()
        {
            startFrame = window.clipView.GetFrameByMousePosition();
        }
        void OnEnd()
        {
            startFrame = -1;
        }
        void OnDrag(Vector2 e)
        {
            var dragFrame = window.clipView.GetFrameByMousePosition();
            var offFrame = dragFrame - startFrame;
            if (asset.StartFrame + offFrame < 0)
            {
                offFrame = 0 - asset.StartFrame;
            }
            asset.StartFrame += offFrame;
            asset.EndFrame += offFrame;
            UpdateView();
        }



        public void UpdateView()
        {
            var sx = window.clipView.GetPositionByFrame(asset.StartFrame);
            var ex = window.clipView.GetPositionByFrame(asset.EndFrame);
            this.style.position = new StyleEnum<Position>(Position.Absolute);
            this.style.left = sx;
            this.style.width = ex - sx;
        }
    }
}
