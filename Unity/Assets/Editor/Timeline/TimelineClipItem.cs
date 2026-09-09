using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.Timeline
{
    public enum DragStatus
    {
        None,
        Left,
        Right,
        Move,
    }

    struct Point
    {
        public double X, Y;

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static double CrossProduct(Point origin, Point first, Point second)
        {
            return (first.X - origin.X) * (second.Y - origin.Y) -
                   (first.Y - origin.Y) * (second.X - origin.X);
        }
    }

    public partial class TimelineClipItem : VisualElement, IToolbarMenuElement
    {
        public DragStatus Status { get; private set; }
        readonly Color color;
        public ITimelineEditorClip Clip { get; }
        public TimelineTrackItem TrackItem { get; }
        public DropdownMenu menu { get; }
        bool menuInitialized;
        int startFrame;
        int endFrame;

        public TimelineClipItem(ITimelineEditorClip clip, TimelineTrackItem track)
        {
            Clip = clip;
            TrackItem = track;
            color = track.Track.Color;

            BuildVisualTree();
            menu = new DropdownMenu();
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            clip.Bind(UpdateView);
        }

        void BuildVisualTree()
        {
            style.position = Position.Absolute;
            style.height = 34;
            style.minHeight = 34;

            root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.paddingLeft = 2;
            root.style.paddingRight = 2;
            root.style.paddingTop = 3;
            root.style.paddingBottom = 3;
            Add(root);

            content = new VisualElement();
            content.style.flexGrow = 1;
            content.style.backgroundColor = new Color(0.22f, 0.28f, 0.29f);
            content.style.borderTopLeftRadius = 3;
            content.style.borderTopRightRadius = 3;
            content.style.borderBottomLeftRadius = 3;
            content.style.borderBottomRightRadius = 3;
            root.Add(content);

            lbType = new Label();
            lbType.style.flexGrow = 1;
            lbType.style.unityTextAlign = TextAnchor.MiddleCenter;
            lbType.style.color = new Color(0.92f, 0.92f, 0.92f);
            lbType.style.overflow = Overflow.Hidden;
            content.Add(lbType);
        }

        public void Release()
        {
            Clip.Unbind(UpdateView);
        }

        void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
        }

        void OnDragPerform(DragPerformEvent evt)
        {
            if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0 ||
                !Clip.TryAssignAnimation(DragAndDrop.paths[0]))
            {
                return;
            }
            UpdateView();
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0)
            {
                FreshInspector();
            }
            else if (evt.button == 1)
            {
                if (!TimelineWindow.IsValid())
                {
                    return;
                }
                EnsureMenu();
                this.ShowMenu();
            }
        }

        void EnsureMenu()
        {
            if (menuInitialized)
            {
                return;
            }
            menu.AppendAction("适配长度", _ => FitAnimationDuration(),
                _ => Clip.CanFitAnimationDuration
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            menu.AppendAction("删除", _ => TrackItem.RemoveClipItem(this),
                _ => DropdownMenuAction.Status.Normal);
            menuInitialized = true;
        }

        void FitAnimationDuration()
        {
            if (!Clip.FitAnimationDuration())
            {
                return;
            }
            UpdateView();
            TimelineWindow.wnd?.clipView?.RefreshLayout();
        }

        public void FreshInspector()
        {
            TimelineWindow.InspectorContent?.FreshInspector(Clip, TrackItem.IsValid);
        }

        static bool IsPointInTriangle(Point point, Point first, Point second, Point third)
        {
            var side1 = Point.CrossProduct(point, first, second) < 0.0f;
            var side2 = Point.CrossProduct(point, second, third) < 0.0f;
            var side3 = Point.CrossProduct(point, third, first) < 0.0f;
            return side1 == side2 && side2 == side3;
        }

        bool IsDragging => Status != DragStatus.None;

        public void ToDown(int frame)
        {
            if (frame < Clip.StartFrame || frame > Clip.EndFrame)
            {
                Status = DragStatus.None;
                return;
            }

            var x = TimelineWindow.GetPositionByFrame(frame);
            var startX = TimelineWindow.GetPositionByFrame(Clip.StartFrame);
            var endX = TimelineWindow.GetPositionByFrame(Clip.EndFrame);
            if (x - startX < 20)
            {
                Status = DragStatus.Left;
            }
            else if (endX - x < 20)
            {
                Status = DragStatus.Right;
            }
            else
            {
                if (Clip.InFrame > 0)
                {
                    var position = this.WorldToLocal(Event.current.mousePosition);
                    var inX = TimelineWindow.GetPositionByFrame(Clip.InFrame);
                    if (IsPointInTriangle(
                        new Point(x, position.y),
                        new Point(startX, 0),
                        new Point(startX, 30),
                        new Point(inX, 30)))
                    {
                        Status = DragStatus.None;
                        return;
                    }
                }
                if (Clip.OutFrame > 0)
                {
                    var position = this.WorldToLocal(Event.current.mousePosition);
                    var outX = TimelineWindow.GetPositionByFrame(Clip.OutFrame);
                    if (IsPointInTriangle(
                        new Point(x, position.y),
                        new Point(outX, 0),
                        new Point(endX, 0),
                        new Point(endX, 30)))
                    {
                        Status = DragStatus.None;
                        return;
                    }
                }
                Status = DragStatus.Move;
            }

            startFrame = Clip.StartFrame;
            endFrame = Clip.EndFrame;
        }

        public void ToDrag(int now, int last)
        {
            if (Status == DragStatus.None)
            {
                return;
            }
            Clip.Drag(Status, now, last);
            UpdateView();
        }

        public void ToUp()
        {
            if (Status == DragStatus.None)
            {
                return;
            }
            Status = DragStatus.None;

            if (!TrackItem.IsValid())
            {
                Clip.SetFrames(startFrame, endFrame, false);
            }
            UpdateView();
        }

        public void RefreshWidth()
        {
            var startX = TimelineWindow.GetPositionByFrame(
                Clip.InFrame > 0 ? Clip.InFrame : Clip.StartFrame);
            var endX = TimelineWindow.GetPositionByFrame(
                Clip.OutFrame > 0 ? Clip.OutFrame : Clip.EndFrame);
            style.left = startX;
            style.width = Mathf.Max(2, endX - startX);
        }

        public void UpdateView()
        {
            lbType.text = string.IsNullOrEmpty(Clip.Name) ? Clip.TypeName : Clip.Name;
            tooltip = null;
            const int lineWidth = 1;
            content.style.borderLeftWidth = lineWidth;
            content.style.borderRightWidth = lineWidth;
            content.style.borderTopWidth = lineWidth;
            content.style.borderBottomWidth = lineWidth;

            if (TrackItem.IsValid())
            {
                content.style.borderLeftColor = IsDragging ? Color.white : color;
                content.style.borderRightColor = IsDragging ? Color.white : color;
                content.style.borderTopColor = IsDragging ? Color.white : color;
                content.style.borderBottomColor = IsDragging ? Color.white : color;
            }
            else
            {
                BringToFront();
                content.style.borderLeftColor = new StyleColor(Color.red);
                content.style.borderRightColor = new StyleColor(Color.red);
                content.style.borderTopColor = new StyleColor(Color.red);
                content.style.borderBottomColor = new StyleColor(Color.red);
            }
            TrackItem.UpdateItemData();
        }
    }
}
