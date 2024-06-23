using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline
{
    public class TimelineClipItem : VisualElement
    {        
        public new class UxmlTraits : TextElement.UxmlTraits
        {
        }

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
            ElementDrag.Add(left, window.clipView, OnLeftStar, OnLeftDrag);
            ElementDrag.Add(right, window.clipView, OnRightStar, OnRightDrag);
            UpdateView();
        }
        
        void OnLeftStar()
        {
            EditorGUIUtility.AddCursorRect(
            GUILayoutUtility.GetLastRect(), MouseCursor.ResizeHorizontal);
            OnLeftDrag(Vector2.zero);             
        }
        
        void OnLeftDrag(Vector2 e)
        {
            var pos = window.clipView.veMarkerContent.WorldToLocal(Event.current.mousePosition);
            var CurPosx = pos.x;
            var offset = window.clipView.veMarkerIcon.worldBound.width / 2;
            if (CurPosx < 0)
            {
                CurPosx = 0;
            }
            if (CurPosx > window.clipView.ScrClipViewWidth - offset)
            {
                CurPosx = window.clipView.ScrClipViewWidth - offset;
            }
            var frame = Mathf.RoundToInt((window.clipView.ScrClipViewOffsetX + CurPosx) / window.clipView.FrameWidth);

            if (frame < 0)
            {
                frame = 0;
            }
            if(frame >= asset.EndFrame)
            {
                frame = asset.EndFrame - 1;
            }
            Log.Debug(frame);
            asset.StartFrame = frame;
            UpdateView();
        }
        void OnRightStar()
        {
            EditorGUIUtility.AddCursorRect(
           GUILayoutUtility.GetLastRect(), MouseCursor.ResizeHorizontal);
            OnLeftDrag(Vector2.zero);
        }
        void OnRightDrag(Vector2 e)
        {
            var pos = window.clipView.WorldToLocal(Event.current.mousePosition);
            var CurPosx = pos.x;
            var offset = window.clipView.veMarkerIcon.worldBound.width / 2;
            if (CurPosx < 0)
            {
                CurPosx = 0;
            }
            if (CurPosx > window.clipView.ScrClipViewWidth - offset)
            {
                CurPosx = window.clipView.ScrClipViewWidth - offset;
            }
            var frame = Mathf.RoundToInt((window.clipView.ScrClipViewOffsetX + CurPosx) / window.clipView.FrameWidth);
            Log.Debug(frame);
            //asset.EndFrame = frame;
            //UpdateView();
        }
        public void UpdateView()
        {
            var FrameWidth = window.clipView.FrameWidth;
            var ScrClipViewOffsetX = window.clipView.ScrClipViewOffsetX;
            var sx = (asset.StartFrame * FrameWidth - ScrClipViewOffsetX);
            var ex = (asset.EndFrame * FrameWidth - ScrClipViewOffsetX);
            this.style.position = new StyleEnum<Position>(Position.Absolute);
            this.style.left = sx;
            this.style.width = ex - sx;
        }
    }
}
