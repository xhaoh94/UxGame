using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;
using Ux.UI;
namespace Ux.Editor.Timeline
{
    public class TimelineClipView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TimelineClipView, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public UxmlTraits()
            {
                base.focusIndex.defaultValue = 0;
                base.focusable.defaultValue = true;
            }
        }
        public TimelineWindow window;

        List<TimelineClipItem> items = new List<TimelineClipItem>();
        int StartFrame => Mathf.CeilToInt(ScrClipViewOffsetX / FrameWidth);
        int EndFrame => Mathf.FloorToInt(ScrClipViewContentWidth + scrClipView.scrollOffset.x / FrameWidth);

        float FrameScale = 1;
        float FrameWidth => 10 * FrameScale;
        float ScrClipViewOffsetX => scrClipView.scrollOffset.x;
        //需要减10 是因为容器里面设置了边缘Border left = 10 
        float ScrClipViewWidth => scrClipView.worldBound.width - 10;
        float ScrClipViewContentWidth => scrClipView.contentContainer.worldBound.width;

        //当前所在帧
        int nowFrame = 0;

        ScrollView scrClipView;
        VisualElement scrContent;
        VisualElement veLineContent;
        VisualElement veMarkerContent;
        VisualElement veMarkerIcon;
        VisualElement veClipContent;
        Label lbMarker;


        public TimelineClipView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/TimelineClipView.uxml");
            visualTree.CloneTree(this);
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            scrClipView = this.Q<ScrollView>("scr_clip");

            ElementDrag.Add(scrClipView, this, OnScrDrag, 2);

            veLineContent = this.Q<VisualElement>("ve_line_content");
            veLineContent.generateVisualContent += OnDrawLine;

            veMarkerContent = this.Q<VisualElement>("ve_marker_content");
            veMarkerContent.generateVisualContent += OnDrawMarker;
            ElementDrag.Add(veMarkerContent, this, OnMarkerStar, OnMarkerDrag);

            veMarkerIcon = this.Q<VisualElement>("ve_marker_icon");
            veMarkerIcon.generateVisualContent += OnDrawMarkerLine;
            lbMarker = this.Q<Label>("lb_marker");

            veClipContent = this.Q<VisualElement>("ve_clip_content");
        }

        public void OnGUI()
        {
            foreach (var item in items)
            {
                item.OnGUI();
            }
        }

        public void AddItem(TimelineClipItem item)
        {
            veClipContent.Add(item);
            items.Add(item);
        }
        public void RemoveItem(TimelineClipItem item)
        {
            veClipContent.Remove(item);
            items.Remove(item);
        }

        void OnGeometryChanged(GeometryChangedEvent changedEvent)
        {
            veLineContent.MarkDirtyRepaint();
            veMarkerContent.MarkDirtyRepaint();
            veMarkerIcon.MarkDirtyRepaint();
        }

        void OnWheel(WheelEvent wheelEvent)
        {
            //下：正  上：负        
            FrameScale -= wheelEvent.delta.y / 100;
            if (FrameScale > 5)
            {
                FrameScale = 5;
            }
            else if (FrameScale < .1f)
            {
                FrameScale = .1f;
            }
            float targetWidth = Mathf.Max(ScrClipViewWidth * FrameScale, ScrClipViewWidth);
            if (ScrClipViewContentWidth != targetWidth)
            {
                scrClipView.contentContainer.style.width = targetWidth;
                scrClipView.schedule.Execute(() =>
                {
                    var fakeOldRect = Rect.zero;
                    var fakeNewRect = scrClipView.layout;

                    using var evt = GeometryChangedEvent.GetPooled(fakeOldRect, fakeNewRect);
                    evt.target = scrClipView.contentContainer;
                    scrClipView.contentContainer.SendEvent(evt);
                });
            }

            veLineContent.MarkDirtyRepaint();
            veMarkerContent.MarkDirtyRepaint();
            veMarkerIcon.MarkDirtyRepaint();
            UpdateMarkerPos();
        }
        public float GetPositionByFrame(int frame)
        {
            return frame * FrameWidth - ScrClipViewOffsetX;
        }
        public int GetFrameByMousePosition()
        {
            var pos = veMarkerContent.WorldToLocal(Event.current.mousePosition);
            var posx = pos.x;
            var offset = veMarkerIcon.worldBound.width / 2;
            if (posx < 0)
            {
                posx = 0;
            }
            if (posx > ScrClipViewWidth - offset)
            {
                posx = ScrClipViewWidth - offset;
            }
            var frame = Mathf.RoundToInt((ScrClipViewOffsetX + posx) / FrameWidth);
            return frame;
        }


        void OnScrDrag(Vector2 e)
        {
            var pos = scrClipView.scrollOffset;
            pos.x -= e.x;
            scrClipView.scrollOffset = pos;
        }
        void OnMarkerStar()
        {
            OnMarkerDrag(Vector2.zero);
        }
        void OnMarkerDrag(Vector2 e)
        {
            var frame = GetFrameByMousePosition();
            if (frame != nowFrame)
            {
                lbMarker.text = nowFrame.ToString();
                UpdateMarkerPos();
                window.SetFrame(frame - nowFrame);
                nowFrame = frame;
            }
        }

        void UpdateMarkerPos()
        {
            try
            {
                var pos = veMarkerIcon.transform.position;
                pos.x = GetPositionByFrame(nowFrame) - (veMarkerIcon.worldBound.width / 2);
                veMarkerIcon.transform.position = pos;
                foreach (var item in items)
                {
                    item.UpdateView();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        void OnDrawLine(MeshGenerationContext mgc)
        {
            var paint2D = mgc.painter2D;
            paint2D.strokeColor = new Color(1, 1, 1, 0.5f);
            paint2D.BeginPath();

            float frameWidth = FrameWidth;
            int startFrame = StartFrame;
            int endFrame = EndFrame;
            int interval = Mathf.FloorToInt(1 + (10 - frameWidth)) * 10;
            if (interval < 5) interval = 5;

            for (int i = startFrame; i <= endFrame; i++)
            {
                if (i % interval == 0)
                {
                    var x = i * frameWidth;
                    paint2D.MoveTo(new Vector2(x, 24));
                    paint2D.LineTo(new Vector2(x, scrClipView.worldBound.height));
                }
            }
            paint2D.Stroke();
        }
        void OnDrawMarker(MeshGenerationContext mgc)
        {
            var paint2D = mgc.painter2D;
            paint2D.strokeColor = Color.white;
            paint2D.BeginPath();

            int startFrame = StartFrame;
            int endFrame = EndFrame;
            float frameWidth = FrameWidth;

            int interval = Mathf.FloorToInt(1 + (10 - frameWidth)) * 10;
            if (interval < 5) interval = 5;

            for (int i = startFrame; i <= endFrame; i++)
            {
                var x = i * frameWidth;
                if (i % interval == 0)
                {
                    paint2D.MoveTo(new Vector2(x, 14));
                    paint2D.LineTo(new Vector2(x, 24));
                    var len = i.ToString().Length;
                    x -= (len * 3);
                    mgc.DrawText(i.ToString(), new Vector2(x, 2), 10, Color.white);
                }
                else if (i % (interval / 5) == 0)
                {
                    paint2D.MoveTo(new Vector2(x, 20));
                    paint2D.LineTo(new Vector2(x, 24));
                    if (frameWidth >= 30)
                    {
                        var len = i.ToString().Length;
                        x -= (len * 3);
                        mgc.DrawText(i.ToString(), new Vector2(x, 5), 10, Color.white);
                    }
                }
            }
            paint2D.Stroke();
        }
        void OnDrawMarkerLine(MeshGenerationContext mgc)
        {
            var paint2D = mgc.painter2D;
            paint2D.strokeColor = new Color(.2f, .7f, .2f, 1f);
            paint2D.BeginPath();
            var x = (veMarkerIcon.worldBound.width / 2);
            paint2D.MoveTo(new Vector2(x, 24));
            paint2D.LineTo(new Vector2(x, scrClipView.worldBound.height));
            paint2D.Stroke();
        }
    }

}
