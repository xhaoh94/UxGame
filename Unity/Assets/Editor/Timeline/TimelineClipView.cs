using System;
using System.Collections.Generic;
using TreeEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.WSA;
namespace Ux.Editor
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

        int StartFrame
        {
            get
            {
                return Mathf.CeilToInt(ScrClipViewOffsetX / FrameWidth);
            }
        }
        int EndFrame
        {
            get
            {
                var width = ScrClipViewContentWidth + scrClipView.scrollOffset.x;
                return Mathf.FloorToInt(width / FrameWidth);
            }
        }


        float FrameScale = 1;
        public float FrameWidth => 10 * FrameScale;
        public float ScrClipViewOffsetX => scrClipView.scrollOffset.x;
        //需要减10 是因为容器里面设置了边缘Border left = 10 
        float ScrClipViewWidth => scrClipView.worldBound.width - 10;
        float ScrClipViewContentWidth => scrClipView.contentContainer.worldBound.width;

        int CurFrame = 0;
        float CurPosx = 0;

        public ScrollView scrClipView { get; private set; }
        public VisualElement ScrContent { get; private set; }
        public VisualElement veLineContent { get; private set; }
        public VisualElement veMarkerContent { get; private set; }
        public VisualElement veMarkerIcon { get; private set; }
        public VisualElement veClipContent { get;private set; }
        public Label lbMarker { get; private set; }

        public ScrollView ScrInspectorView { get; private set; }

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
            var pos = veMarkerContent.WorldToLocal(Event.current.mousePosition);
            CurPosx = pos.x;
            var offset = veMarkerIcon.worldBound.width / 2;
            if (CurPosx < 0)
            {
                CurPosx = 0;
            }
            if (CurPosx > ScrClipViewWidth - offset)
            {
                CurPosx = ScrClipViewWidth - offset;
            }
            var frame = Mathf.RoundToInt((ScrClipViewOffsetX + CurPosx) / FrameWidth);
            if (frame != CurFrame)
            {
                CurFrame = frame;
                lbMarker.text = CurFrame.ToString();
                UpdateMarkerPos();
            }
        }

        void UpdateMarkerPos()
        {
            try
            {
                CurPosx = (CurFrame * FrameWidth - ScrClipViewOffsetX);
                var pos = veMarkerIcon.transform.position;
                pos.x = CurPosx - (veMarkerIcon.worldBound.width / 2);
                veMarkerIcon.transform.position = pos;
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
