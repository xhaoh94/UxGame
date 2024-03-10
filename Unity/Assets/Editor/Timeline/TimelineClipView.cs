using System.Collections.Generic;
using TreeEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
            var width = Mathf.Max(ScrClipViewContentWidth, ScrClipViewWidth);
            width += ScrClipView.scrollOffset.x;
            return Mathf.FloorToInt(width / FrameWidth);
        }
    }


    float FrameScale = 1;
    float FrameWidth => 20 * FrameScale;
    float ScrClipViewOffsetX => ScrClipView.scrollOffset.x;
    float ScrClipViewWidth => ScrClipView.worldBound.width;
    float ScrClipViewContentWidth => ScrClipView.contentContainer.worldBound.width;
    int FrameInterval => Mathf.CeilToInt(10 / FrameScale);

    public ScrollView ScrClipView { get; private set; }
    public VisualElement VeLineContent { get; private set; }
    public VisualElement VeMarkerContent { get; private set; }

    public ScrollView ScrInspectorView { get; private set; }

    bool m_ScrollViewPan;
    float m_ScrollViewPanDelta;
    public TimelineClipView()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/TimelineClipView.uxml");
        visualTree.CloneTree(this);

        ScrClipView = this.Q<ScrollView>("scr_clip");
        ScrClipView.RegisterCallback<PointerDownEvent>((e) =>
        {
            if (e.button == 2)
            {
                m_ScrollViewPan = true;
                m_ScrollViewPanDelta = e.localPosition.x;
                //TrackField.AddToClassList("pan");
            }
        });

        ScrClipView.RegisterCallback<PointerMoveEvent>((e) =>
        {
            if (m_ScrollViewPan)
            {
                ScrClipView.scrollOffset = new Vector2(ScrClipView.scrollOffset.x + m_ScrollViewPanDelta - e.localPosition.x, ScrClipView.scrollOffset.y);
                m_ScrollViewPanDelta = e.localPosition.x;
            }
        });
        ScrClipView.RegisterCallback<PointerOutEvent>((e) =>
        {
            m_ScrollViewPan = false;
            //TrackField.RemoveFromClassList("pan");
        });
        ScrClipView.RegisterCallback<PointerUpEvent>((e) =>
        {
            m_ScrollViewPan = false;
            //TrackField.RemoveFromClassList("pan");
        });
        //ScrClipView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        ScrClipView.horizontalScroller.valueChanged += (e) =>
        {
            //if (VeContent.worldBound.width < ScrollViewContentWidth + ScrollViewContentOffset)
            //    VeContent.style.width = ScrollViewContentWidth + ScrollViewContentOffset;
            //DrawTimeField();
        };

        VeLineContent = this.Q<VisualElement>("ve_line_content");
        VeLineContent.generateVisualContent += OnDrawLine;

        VeMarkerContent = this.Q<VisualElement>("ve_marker_content");
        VeMarkerContent.generateVisualContent += OnDrawMarker;

        RegisterCallback<WheelEvent>(OnWheel);

    }
    void OnWheel(WheelEvent wheelEvent)
    {
        //下：正  上：负        
        FrameScale -= wheelEvent.delta.y / 100;
        if (FrameScale > 20)
        {
            FrameScale = 20;
        }
        else if (FrameScale < 1f)
        {
            FrameScale = 1f;
        }
        var temWidth = ScrClipViewWidth * FrameScale;
        float targetWidth = Mathf.Max(temWidth, ScrClipViewWidth);
        if (ScrClipViewContentWidth == targetWidth)
        {
            Resize();
            //DrawTimeField();
        }
        else
        {
            ScrClipView.contentContainer.style.width = targetWidth;
            Resize();
        }
        ForceScrollViewUpdate(ScrClipView);
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
        paint2D.strokeColor = Color.white;
        paint2D.BeginPath();

        int interval = FrameInterval;
        int startFrame = StartFrame;
        int endFrame = EndFrame;

        for (int i = startFrame; i <= endFrame; i++)
        {
            if (i % (interval * 5) == 0)
            {
                var x = i * FrameWidth;
                paint2D.MoveTo(new Vector2(x, 0));
                paint2D.LineTo(new Vector2(x, ScrClipView.worldBound.height));
            }
        }
        paint2D.Stroke();
    }
    void OnDrawMarker(MeshGenerationContext mgc)
    {
        var paint2D = mgc.painter2D;
        paint2D.strokeColor = Color.white;
        paint2D.BeginPath();

        int interval = FrameInterval;
        int startFrame = StartFrame;
        int endFrame = EndFrame;

        for (int i = startFrame; i <= endFrame; i++)
        {
            var x = i * FrameWidth;
            if (i % (interval * 5) == 0)
            {
                mgc.DrawText(i.ToString(), new Vector2(x + 5, 5), 12, Color.white);
                continue;
            }
            else if (i % interval == 0)
            {
                paint2D.MoveTo(new Vector2(x, 20));
                paint2D.LineTo(new Vector2(x, 25));
                mgc.DrawText(i.ToString(), new Vector2(x + 5, 5), 12, Color.white);
            }
            //paint2D.MoveTo(new Vector2(x, 20));
            //paint2D.LineTo(new Vector2(x, 25));
            //mgc.DrawText(i.ToString(), new Vector2(x + 5, 5), 12, Color.white);

        }
        paint2D.Stroke();
    }


    void Resize()
    {

    }
}
