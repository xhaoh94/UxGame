using Assets.Editor.Timeline;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.Timeline
{
    public partial class TimelineClipView : VisualElement
    {
        protected VisualElement root;
        public ScrollView scrClipView;
        public Toolbar Toolbar;
        public VisualElement veLineContent;
        public VisualElement veClipContent;
        public VisualElement veMarkerContent;
        public VisualElement veMarkerIcon;
        public Label lbMarker;
        public VisualElement veInspector;

        const float RulerHeight = 32f;
        const float TrackHeight = 34f;
        // Give frame zero a small gutter so the playhead handle stays centered.
        const float TimeOriginPadding = 24f;
        const float PlayheadHandleWidth = 14f;
        const float MinPixelsPerFrame = 2f;
        const float MaxPixelsPerFrame = 40f;

        VisualElement _timelinePanel;
        VisualElement _rulerViewport;
        VisualElement _canvas;
        VisualElement _grid;
        VisualElement _frameEventMarkerContent;
        VisualElement _playheadLine;
        readonly Dictionary<ITimelineEditorFrameEvent, VisualElement> _frameEventMarkers = new();
        ITimelineEditorFrameEvent _draggingFrameEvent;
        VisualElement _draggingFrameEventMarker;
        int _draggingFrameEventPointerId = -1;
        ScrollView _inspectorScroll;
        bool _syncingVerticalScroll;
        bool _scrubbing;
        float _pixelsPerFrame = 7.5f;
        float _contentWidth;
        float _contentHeight;
        Vector2 _lastPointerPosition;

        public int CurFrame { get; private set; }
        public event Action<int> FrameChanged;
        public event Action<float> VerticalScrollChanged;

        float ViewportWidth => Mathf.Max(0, scrClipView?.contentViewport?.resolvedStyle.width ?? 0);
        float ViewportHeight => Mathf.Max(0, scrClipView?.contentViewport?.resolvedStyle.height ?? 0);

        public TimelineClipView()
        {
            style.flexGrow = 1;
            style.minWidth = 320;
            BuildUI();

            TimelineWindow.InspectorContent = new TimelineInspectorView(veInspector, TimelineWindow.Document);
            TimelineWindow.ClipContent = veClipContent;
            TimelineWindow.GetPositionByFrame = GetPositionByFrame;
            TimelineWindow.GetFrameByMousePosition = GetFrameByMousePosition;

            RegisterCallback<GeometryChangedEvent>(_ => RefreshLayout());
            scrClipView.horizontalScroller.valueChanged += _ => UpdateHorizontalOffset();
            scrClipView.verticalScroller.valueChanged += OnVerticalScrollChanged;
            scrClipView.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => RefreshLayout());
            scrClipView.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);

            RegisterCallback<PointerDownEvent>(evt => _lastPointerPosition = (Vector2)evt.position,
                TrickleDown.TrickleDown);
            RegisterCallback<PointerMoveEvent>(evt => _lastPointerPosition = (Vector2)evt.position,
                TrickleDown.TrickleDown);
            RegisterCallback<PointerDownEvent>(OnMiddlePointerDown);
            RegisterCallback<PointerMoveEvent>(OnMiddlePointerMove);
            RegisterCallback<PointerUpEvent>(OnMiddlePointerUp);

            _rulerViewport.RegisterCallback<PointerDownEvent>(OnRulerContextMenu);
            _rulerViewport.RegisterCallback<PointerDownEvent>(OnScrubDown);
            _rulerViewport.RegisterCallback<PointerMoveEvent>(OnScrubMove);
            _rulerViewport.RegisterCallback<PointerUpEvent>(OnScrubUp);
            _rulerViewport.RegisterCallback<PointerCaptureOutEvent>(_ => _scrubbing = false);

            TimelineWindow.RefreshClip += RefreshClipItems;
        }

        void BuildUI()
        {
            root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Row;
            Add(root);

            var split = new TwoPaneSplitView(
                1, 310, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;
            root.Add(split);

            _timelinePanel = new VisualElement();
            _timelinePanel.style.flexGrow = 1;
            _timelinePanel.style.minWidth = 240;
            _timelinePanel.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f);
            split.Add(_timelinePanel);

            Toolbar = new Toolbar();
            Toolbar.style.height = RulerHeight;
            Toolbar.style.minHeight = RulerHeight;
            Toolbar.style.flexShrink = 0;
            Toolbar.style.paddingLeft = 0;
            Toolbar.style.paddingRight = 0;
            _timelinePanel.Add(Toolbar);

            _rulerViewport = new VisualElement();
            _rulerViewport.style.flexGrow = 1;
            _rulerViewport.style.height = RulerHeight;
            _rulerViewport.style.overflow = Overflow.Hidden;
            _rulerViewport.style.position = Position.Relative;
            Toolbar.Add(_rulerViewport);

            veLineContent = new VisualElement { pickingMode = PickingMode.Ignore };
            veLineContent.style.position = Position.Absolute;
            veLineContent.style.left = 0;
            veLineContent.style.top = 0;
            veLineContent.style.height = RulerHeight;
            veLineContent.generateVisualContent += OnDrawRuler;
            _rulerViewport.Add(veLineContent);

            _frameEventMarkerContent = new VisualElement { pickingMode = PickingMode.Ignore };
            _frameEventMarkerContent.style.position = Position.Absolute;
            _frameEventMarkerContent.style.left = 0;
            _frameEventMarkerContent.style.top = 0;
            _frameEventMarkerContent.style.height = RulerHeight;
            _rulerViewport.Add(_frameEventMarkerContent);

            veMarkerContent = new VisualElement { pickingMode = PickingMode.Ignore };
            veMarkerContent.style.position = Position.Absolute;
            veMarkerContent.style.left = 0;
            veMarkerContent.style.top = 0;
            veMarkerContent.style.height = RulerHeight;
            _rulerViewport.Add(veMarkerContent);

            veMarkerIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            veMarkerIcon.style.position = Position.Absolute;
            veMarkerIcon.style.top = RulerHeight - 10;
            veMarkerIcon.style.width = PlayheadHandleWidth;
            veMarkerIcon.style.minWidth = PlayheadHandleWidth;
            veMarkerIcon.style.maxWidth = PlayheadHandleWidth;
            veMarkerIcon.style.height = 10;
            veMarkerIcon.style.minHeight = 10;
            veMarkerIcon.style.flexShrink = 0;
            veMarkerIcon.generateVisualContent += OnDrawPlayheadHandle;
            veMarkerContent.Add(veMarkerIcon);

            // The exact value already lives in the toolbar's “当前帧” field. A compact
            // handle keeps the ruler readable and matches common sequencer editors.
            lbMarker = new Label("0") { pickingMode = PickingMode.Ignore };
            lbMarker.style.display = DisplayStyle.None;
            veMarkerIcon.Add(lbMarker);

            scrClipView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scrClipView.style.flexGrow = 1;
            scrClipView.style.minHeight = 120;
            scrClipView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            scrClipView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _timelinePanel.Add(scrClipView);

            _canvas = new VisualElement();
            _canvas.style.position = Position.Relative;
            _canvas.style.flexShrink = 0;
            _canvas.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            scrClipView.Add(_canvas);

            _grid = new VisualElement { pickingMode = PickingMode.Ignore };
            _grid.style.position = Position.Absolute;
            _grid.style.left = 0;
            _grid.style.top = 0;
            _grid.generateVisualContent += OnDrawGrid;
            _canvas.Add(_grid);

            veClipContent = new VisualElement();
            veClipContent.style.position = Position.Relative;
            veClipContent.style.flexShrink = 0;
            _canvas.Add(veClipContent);

            _playheadLine = new VisualElement { pickingMode = PickingMode.Ignore };
            _playheadLine.style.position = Position.Absolute;
            _playheadLine.style.top = 0;
            _playheadLine.style.width = 1;
            _playheadLine.style.backgroundColor = new Color(0.18f, 0.85f, 0.35f);
            _canvas.Add(_playheadLine);

            var inspectorPanel = new VisualElement();
            inspectorPanel.style.flexGrow = 1;
            inspectorPanel.style.minWidth = 220;
            inspectorPanel.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            split.Add(inspectorPanel);

            var inspectorHeader = new Toolbar();
            inspectorHeader.style.height = RulerHeight;
            inspectorHeader.style.minHeight = RulerHeight;
            var title = new Label("检查器");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleLeft;
            title.style.marginLeft = 8;
            inspectorHeader.Add(title);
            inspectorPanel.Add(inspectorHeader);

            _inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
            _inspectorScroll.style.flexGrow = 1;
            _inspectorScroll.style.paddingLeft = 8;
            _inspectorScroll.style.paddingRight = 8;
            _inspectorScroll.style.paddingTop = 6;
            inspectorPanel.Add(_inspectorScroll);
            veInspector = _inspectorScroll.contentContainer;
        }

        public void Init()
        {
            UpdateMarker();
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (scrClipView == null)
            {
                return;
            }

            var frameRate = TimelineWindow.Document?.FrameRate ?? TimelineEditorDocument.DefaultFrameRate;
            var duration = TimelineWindow.Document?.DurationFrames ?? 0;
            var minimumFrames = Mathf.Max(frameRate * 2, duration + frameRate);
            _contentWidth = Mathf.Max(
                ViewportWidth,
                TimeOriginPadding * 2 + minimumFrames * _pixelsPerFrame);

            var trackCount = TimelineWindow.Document?.TrackCount ?? 0;
            var rowsHeight = Mathf.Max(TrackHeight, trackCount * TrackHeight);
            _contentHeight = Mathf.Max(ViewportHeight, rowsHeight);

            _canvas.style.width = _contentWidth;
            _canvas.style.height = _contentHeight;
            _grid.style.width = _contentWidth;
            _grid.style.height = _contentHeight;
            veClipContent.style.width = _contentWidth;
            veClipContent.style.height = rowsHeight;
            _playheadLine.style.height = _contentHeight;
            veLineContent.style.width = _contentWidth;
            _frameEventMarkerContent.style.width = _contentWidth;
            veMarkerContent.style.width = _contentWidth;

            RefreshFrameEventMarkers();
            UpdateHorizontalOffset();
            UpdateMarker();
            veLineContent.MarkDirtyRepaint();
            _grid.MarkDirtyRepaint();
        }

        public void SetVerticalScroll(float value)
        {
            if (scrClipView == null || Mathf.Approximately(scrClipView.scrollOffset.y, value))
            {
                return;
            }
            _syncingVerticalScroll = true;
            var offset = scrClipView.scrollOffset;
            offset.y = value;
            scrClipView.scrollOffset = offset;
            _syncingVerticalScroll = false;
        }

        void OnVerticalScrollChanged(float value)
        {
            if (!_syncingVerticalScroll)
            {
                VerticalScrollChanged?.Invoke(value);
            }
        }

        void UpdateHorizontalOffset()
        {
            var x = -(scrClipView?.scrollOffset.x ?? 0);
            veLineContent.style.translate = new Translate(x, 0);
            _frameEventMarkerContent.style.translate = new Translate(x, 0);
            veMarkerContent.style.translate = new Translate(x, 0);
        }

        void OnWheel(WheelEvent evt)
        {
            if (!evt.ctrlKey || TimelineWindow.Document?.HasSource != true)
            {
                return;
            }

            var viewportPosition = scrClipView.contentViewport.WorldToLocal(evt.mousePosition);
            var focusFrame = (scrClipView.scrollOffset.x + viewportPosition.x - TimeOriginPadding) /
                _pixelsPerFrame;
            var factor = evt.delta.y > 0 ? 0.85f : 1.18f;
            SetZoom(_pixelsPerFrame * factor, focusFrame, viewportPosition.x);
            evt.StopImmediatePropagation();
        }

        void SetZoom(float value, float focusFrame, float viewportX)
        {
            var next = Mathf.Clamp(value, MinPixelsPerFrame, MaxPixelsPerFrame);
            if (Mathf.Approximately(next, _pixelsPerFrame))
            {
                return;
            }

            _pixelsPerFrame = next;
            RefreshLayout();
            var offset = scrClipView.scrollOffset;
            offset.x = Mathf.Max(
                0,
                TimeOriginPadding + focusFrame * _pixelsPerFrame - viewportX);
            scrClipView.scrollOffset = offset;
            TimelineWindow.RefreshClip?.Invoke();
        }

        Vector2 _panStartPointer;
        Vector2 _panStartOffset;
        bool _panning;

        void OnMiddlePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 2)
            {
                return;
            }
            _panning = true;
            _panStartPointer = (Vector2)evt.position;
            _panStartOffset = scrClipView.scrollOffset;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnMiddlePointerMove(PointerMoveEvent evt)
        {
            if (!_panning || !this.HasPointerCapture(evt.pointerId))
            {
                return;
            }
            var delta = (Vector2)evt.position - _panStartPointer;
            scrClipView.scrollOffset = new Vector2(
                Mathf.Max(0, _panStartOffset.x - delta.x),
                Mathf.Max(0, _panStartOffset.y - delta.y));
            evt.StopPropagation();
        }

        void OnMiddlePointerUp(PointerUpEvent evt)
        {
            if (!_panning || evt.button != 2)
            {
                return;
            }
            _panning = false;
            if (this.HasPointerCapture(evt.pointerId))
            {
                this.ReleasePointer(evt.pointerId);
            }
            evt.StopPropagation();
        }

        void OnRulerContextMenu(PointerDownEvent evt)
        {
            if (evt.button != 1 || TimelineWindow.Document?.CanEdit != true)
            {
                return;
            }

            var eventTypes = TimelineWindow.Document.GetFrameEventTypes();
            var frame = GetFrameAtRulerPosition(evt.position);
            var menu = new GenericMenu();
            if (eventTypes == null || eventTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("没有可用的帧事件类型"));
            }
            else
            {
                for (var i = 0; i < eventTypes.Count; i++)
                {
                    var eventType = eventTypes[i];
                    var displayName = TimelineWindow.Document.GetFrameEventDisplayName(eventType);
                    menu.AddItem(
                        new GUIContent($"添加/{displayName}"),
                        false,
                        () => AddFrameEventAt(eventType, frame));
                }
            }
            menu.ShowAsContext();
            evt.StopPropagation();
        }

        void AddFrameEventAt(Type eventType, int frame)
        {
            var frameEvent = TimelineWindow.Document?.AddFrameEvent(eventType, frame);
            if (frameEvent != null)
            {
                TimelineWindow.InspectorContent?.FreshInspector(frameEvent, null);
            }
        }

        void OnScrubDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || !TimelineWindow.IsValid())
            {
                return;
            }
            _scrubbing = true;
            _rulerViewport.CapturePointer(evt.pointerId);
            ScrubAt(evt.position);
            evt.StopPropagation();
        }

        void OnScrubMove(PointerMoveEvent evt)
        {
            if (_scrubbing && _rulerViewport.HasPointerCapture(evt.pointerId))
            {
                ScrubAt(evt.position);
                evt.StopPropagation();
            }
        }

        void OnScrubUp(PointerUpEvent evt)
        {
            if (!_scrubbing || evt.button != 0)
            {
                return;
            }
            ScrubAt(evt.position);
            _scrubbing = false;
            if (_rulerViewport.HasPointerCapture(evt.pointerId))
            {
                _rulerViewport.ReleasePointer(evt.pointerId);
            }
            evt.StopPropagation();
        }

        void ScrubAt(Vector2 worldPosition)
        {
            SetNowFrame(GetFrameAtRulerPosition(worldPosition));
        }

        int GetFrameAtRulerPosition(Vector2 worldPosition)
        {
            var local = _rulerViewport.WorldToLocal(worldPosition);
            var frame = Mathf.RoundToInt(
                (local.x + scrClipView.scrollOffset.x - TimeOriginPadding) / _pixelsPerFrame);
            return Mathf.Clamp(frame, 0, Mathf.Max(0, TimelineWindow.Document?.DurationFrames ?? 0));
        }

        float GetPositionByFrame(int frame)
        {
            return TimeOriginPadding + Mathf.Max(0, frame) * _pixelsPerFrame;
        }

        int GetFrameByMousePosition()
        {
            var worldPosition = Event.current != null
                ? (Vector2)Event.current.mousePosition
                : _lastPointerPosition;
            var local = scrClipView.contentViewport.WorldToLocal(worldPosition);
            var x = Mathf.Max(
                0,
                local.x + scrClipView.scrollOffset.x - TimeOriginPadding);
            return Mathf.RoundToInt(x / _pixelsPerFrame);
        }

        public void SetNowFrame(int frame, bool forceEvaluate = false)
        {
            frame = Mathf.Max(0, frame);
            if (forceEvaluate || frame != CurFrame)
            {
                TimelineWindow.MarkerMove?.Invoke(frame);
                CurFrame = frame;
                FrameChanged?.Invoke(frame);
            }
            UpdateMarker();
        }

        public void ResetView()
        {
            CurFrame = 0;
            scrClipView.scrollOffset = Vector2.zero;
            RefreshLayout();
            SetNowFrame(0, true);
        }

        void UpdateMarker()
        {
            if (veMarkerIcon == null)
            {
                return;
            }
            lbMarker.text = CurFrame.ToString();
            var x = GetPositionByFrame(CurFrame);
            veMarkerIcon.style.left = Mathf.Clamp(
                x - PlayheadHandleWidth * 0.5f,
                0,
                Mathf.Max(0, _contentWidth - PlayheadHandleWidth));
            _playheadLine.style.left = x;
        }

        void RefreshClipItems()
        {
            RefreshLayout();
        }

        void RefreshFrameEventMarkers()
        {
            if (_frameEventMarkerContent == null)
            {
                return;
            }

            var events = TimelineWindow.Document?.FrameEvents;
            var active = new HashSet<ITimelineEditorFrameEvent>();
            if (events != null)
            {
                for (var i = 0; i < events.Count; i++)
                {
                    if (events[i] != null)
                    {
                        active.Add(events[i]);
                    }
                }
            }

            var stale = new List<ITimelineEditorFrameEvent>();
            foreach (var pair in _frameEventMarkers)
            {
                if (!active.Contains(pair.Key))
                {
                    _frameEventMarkerContent.Remove(pair.Value);
                    stale.Add(pair.Key);
                }
            }
            for (var i = 0; i < stale.Count; i++)
            {
                _frameEventMarkers.Remove(stale[i]);
            }

            if (events == null)
            {
                return;
            }

            for (var i = 0; i < events.Count; i++)
            {
                var frameEvent = events[i];
                if (frameEvent == null)
                {
                    continue;
                }

                if (!_frameEventMarkers.TryGetValue(frameEvent, out var marker))
                {
                    marker = CreateFrameEventMarker(frameEvent);
                    _frameEventMarkers.Add(frameEvent, marker);
                    _frameEventMarkerContent.Add(marker);
                }

                marker.tooltip = $"{frameEvent.Name} @ {frameEvent.Frame}";
                marker.style.left = GetPositionByFrame(frameEvent.Frame) - 5;
                marker.style.backgroundColor = frameEvent.Color;
            }
        }

        VisualElement CreateFrameEventMarker(ITimelineEditorFrameEvent frameEvent)
        {
            var marker = new VisualElement
            {
                pickingMode = PickingMode.Position,
            };
            marker.style.position = Position.Absolute;
            marker.style.top = 10;
            marker.style.width = 10;
            marker.style.minWidth = 10;
            marker.style.height = 16;
            marker.style.minHeight = 16;
            marker.style.borderTopLeftRadius = 5;
            marker.style.borderTopRightRadius = 5;
            marker.style.borderBottomLeftRadius = 5;
            marker.style.borderBottomRightRadius = 5;

            marker.RegisterCallback<PointerDownEvent>(evt => OnFrameEventPointerDown(marker, frameEvent, evt));
            marker.RegisterCallback<PointerMoveEvent>(evt => OnFrameEventPointerMove(marker, frameEvent, evt));
            marker.RegisterCallback<PointerUpEvent>(evt => OnFrameEventPointerUp(marker, frameEvent, evt));
            marker.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (ReferenceEquals(_draggingFrameEventMarker, marker))
                {
                    ClearFrameEventDrag(marker);
                }
            });
            return marker;
        }

        void OnFrameEventPointerDown(
            VisualElement marker,
            ITimelineEditorFrameEvent frameEvent,
            PointerDownEvent evt)
        {
            if (evt.button == 1)
            {
                ShowFrameEventContextMenu(frameEvent);
                evt.StopPropagation();
                return;
            }
            if (evt.button != 0 || TimelineWindow.Document?.CanEdit != true)
            {
                return;
            }

            TimelineWindow.InspectorContent?.FreshInspector(frameEvent, null);
            _draggingFrameEvent = frameEvent;
            _draggingFrameEventMarker = marker;
            _draggingFrameEventPointerId = evt.pointerId;
            marker.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnFrameEventPointerMove(
            VisualElement marker,
            ITimelineEditorFrameEvent frameEvent,
            PointerMoveEvent evt)
        {
            if (!ReferenceEquals(_draggingFrameEvent, frameEvent) ||
                !ReferenceEquals(_draggingFrameEventMarker, marker) ||
                !marker.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            frameEvent.SetFrame(GetFrameAtRulerPosition(evt.position));
            evt.StopPropagation();
        }

        void OnFrameEventPointerUp(
            VisualElement marker,
            ITimelineEditorFrameEvent frameEvent,
            PointerUpEvent evt)
        {
            if (!ReferenceEquals(_draggingFrameEvent, frameEvent) ||
                !ReferenceEquals(_draggingFrameEventMarker, marker) ||
                evt.button != 0)
            {
                return;
            }

            ClearFrameEventDrag(marker);
            evt.StopPropagation();
        }

        void ClearFrameEventDrag(VisualElement marker)
        {
            var pointerId = _draggingFrameEventPointerId;
            _draggingFrameEvent = null;
            _draggingFrameEventMarker = null;
            _draggingFrameEventPointerId = -1;
            if (marker != null && pointerId >= 0 && marker.HasPointerCapture(pointerId))
            {
                marker.ReleasePointer(pointerId);
            }
        }

        void ShowFrameEventContextMenu(ITimelineEditorFrameEvent frameEvent)
        {
            var menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("删除事件"),
                false,
                () => TimelineWindow.Document?.RemoveFrameEvent(frameEvent));
            menu.ShowAsContext();
        }

        void OnDrawPlayheadHandle(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            painter.fillColor = new Color(0.18f, 0.85f, 0.35f);
            painter.BeginPath();
            painter.MoveTo(Vector2.zero);
            painter.LineTo(new Vector2(PlayheadHandleWidth, 0));
            painter.LineTo(new Vector2(PlayheadHandleWidth * 0.5f, 10));
            painter.ClosePath();
            painter.Fill();
        }

        void GetTickSteps(out int minorStep, out int majorStep)
        {
            var desiredMajorFrames = Mathf.Max(1f, 90f / _pixelsPerFrame);
            var magnitude = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(desiredMajorFrames)));
            var normalized = desiredMajorFrames / magnitude;
            var nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
            majorStep = Mathf.Max(1, Mathf.RoundToInt(nice * magnitude));
            minorStep = majorStep >= 5 ? Mathf.Max(1, majorStep / 5) : 1;
        }

        void OnDrawRuler(MeshGenerationContext mgc)
        {
            if (_pixelsPerFrame <= 0 || _contentWidth <= 0)
            {
                return;
            }
            GetTickSteps(out var minorStep, out var majorStep);
            var maxFrame = Mathf.CeilToInt(
                Mathf.Max(0, _contentWidth - TimeOriginPadding) / _pixelsPerFrame);
            var painter = mgc.painter2D;
            painter.lineWidth = 1;
            painter.BeginPath();
            for (var frame = 0; frame <= maxFrame; frame += minorStep)
            {
                var x = TimeOriginPadding + frame * _pixelsPerFrame;
                var major = frame % majorStep == 0;
                painter.strokeColor = major
                    ? new Color(0.85f, 0.85f, 0.85f, 0.85f)
                    : new Color(0.65f, 0.65f, 0.65f, 0.55f);
                painter.MoveTo(new Vector2(x, major ? 17 : 23));
                painter.LineTo(new Vector2(x, RulerHeight));
                painter.Stroke();
                if (major)
                {
                    mgc.DrawText(frame.ToString(), new Vector2(x + 3, 2), 10, new Color(0.9f, 0.9f, 0.9f));
                }
                painter.BeginPath();
            }
        }

        void OnDrawGrid(MeshGenerationContext mgc)
        {
            if (_pixelsPerFrame <= 0 || _contentWidth <= 0)
            {
                return;
            }
            GetTickSteps(out _, out var majorStep);
            var maxFrame = Mathf.CeilToInt(
                Mathf.Max(0, _contentWidth - TimeOriginPadding) / _pixelsPerFrame);
            var painter = mgc.painter2D;
            painter.lineWidth = 1;
            painter.strokeColor = new Color(0.75f, 0.75f, 0.75f, 0.15f);
            painter.BeginPath();
            for (var frame = 0; frame <= maxFrame; frame += majorStep)
            {
                var x = TimeOriginPadding + frame * _pixelsPerFrame;
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, _contentHeight));
            }
            painter.Stroke();

            painter.strokeColor = new Color(0, 0, 0, 0.35f);
            painter.BeginPath();
            var trackCount = TimelineWindow.Document?.TrackCount ?? 0;
            for (var row = 0; row <= trackCount; row++)
            {
                var y = row * TrackHeight;
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(_contentWidth, y));
            }
            painter.Stroke();
        }
    }
}
