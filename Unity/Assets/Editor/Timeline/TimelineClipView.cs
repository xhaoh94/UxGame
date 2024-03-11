using System.Collections.Generic;
using TreeEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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
        float FrameWidth => 10 * FrameScale;
        float ScrClipViewOffsetX => scrClipView.scrollOffset.x;
        float ScrClipViewWidth => scrClipView.worldBound.width;
        float ScrClipViewContentWidth => scrClipView.contentContainer.worldBound.width;

        public ScrollView scrClipView { get; private set; }
        public VisualElement veLineContent { get; private set; }
        public VisualElement veMarkerContent { get; private set; }
        public VisualElement veMarkerIcon { get; private set; }
        public Label lbMarker { get; private set; }

        public ScrollView ScrInspectorView { get; private set; }

        public TimelineClipView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/TimelineClipView.uxml");
            visualTree.CloneTree(this);

            scrClipView = this.Q<ScrollView>("scr_clip");
            new VeDrag(scrClipView, null, (e) =>
            {
                var pos = scrClipView.scrollOffset;
                pos.x -= e.x;
                scrClipView.scrollOffset = pos;
            }, null, 2);
            //ScrClipView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            scrClipView.horizontalScroller.valueChanged += (e) =>
            {
                //if (VeContent.worldBound.width < ScrollViewContentWidth + ScrollViewContentOffset)
                //    VeContent.style.width = ScrollViewContentWidth + ScrollViewContentOffset;
                //DrawTimeField();
            };

            veLineContent = this.Q<VisualElement>("ve_line_content");
            veLineContent.generateVisualContent += OnDrawLine;

            veMarkerContent = this.Q<VisualElement>("ve_marker_content");
            veMarkerContent.generateVisualContent += OnDrawMarker;


            veMarkerIcon = this.Q<VisualElement>("ve_marker_icon");            
            new VeDrag(veMarkerContent,
                (e) =>
                {
                    var pos = veMarkerIcon.transform.position;
                    pos.x = e.x;
                    veMarkerIcon.transform.position = pos;
                },
                (e) =>
                {
                    var pos = veMarkerIcon.transform.position;
                    pos.x += e.x;
                    veMarkerIcon.transform.position = pos;
                }, null);
           
            lbMarker = this.Q<Label>("lb_marker");

            RegisterCallback<WheelEvent>(OnWheel);

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
            if (ScrClipViewContentWidth == targetWidth)
            {
                Resize();
                veMarkerContent.MarkDirtyRepaint();
                veMarkerContent.MarkDirtyRepaint();
            }
            else
            {
                scrClipView.contentContainer.style.width = targetWidth;
                Resize();
                ForceScrollViewUpdate(scrClipView);
            }
        }
        public void ForceScrollViewUpdate(ScrollView view)
        {
            view.schedule.Execute(() =>
            {
                var fakeOldRect = Rect.zero;
                var fakeNewRect = view.layout;

                using var evt = GeometryChangedEvent.GetPooled(fakeOldRect, fakeNewRect);
                evt.target = view.contentContainer;
                view.contentContainer.SendEvent(evt);
            });
        }
        float m_WheelLerpSpeed = 0.2f;
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
                    var x = i * FrameWidth;
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
                    mgc.DrawText(i.ToString(), new Vector2(x + 3, 5), 10, Color.white);
                }
                else if (i % (interval / 5) == 0)
                {
                    paint2D.MoveTo(new Vector2(x, 20));
                    paint2D.LineTo(new Vector2(x, 24));
                    if (frameWidth >= 30)
                    {
                        mgc.DrawText(i.ToString(), new Vector2(x + 3, 5), 10, Color.white);
                    }
                }
            }
            paint2D.Stroke();
        }


        void Resize()
        {

        }
    }

}
