using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static GluonGui.WorkspaceWindow.Views.Checkin.Operations.CheckinViewDeleteOperation;
namespace Ux.Editor.Timeline
{
    public enum Status
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

        // 计算向量叉积  
        public static double CrossProduct(Point O, Point A, Point B)
        {
            return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
        }
    }
    public partial class TimelineClipItem : VisualElement, IToolbarMenuElement
    {
        public Status Status { get; private set; }
        Color color;
        public TimelineClipAsset Asset { get; private set; }
        public TimelineTrackItem TrackItem { get; private set; }
        public DropdownMenu menu { get; }
        public TimelineClipItem()
        {
            CreateChildren();
            Add(root);

            menu = new DropdownMenu();
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<DragUpdatedEvent>(_OnDragUpd);
            RegisterCallback<DragPerformEvent>(_OnDragPerform);
            style.position = new StyleEnum<Position>(Position.Absolute);
            style.height = 30;
        }
        void _OnDragUpd(DragUpdatedEvent e)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
        }
        void _OnDragPerform(DragPerformEvent e)
        {
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                string retPath = DragAndDrop.paths[0];
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(retPath);
                if (clip == null)
                {
                    return;
                }
                (Asset as AnimationClipAsset).clip = clip;
                TimelineEditor.SaveAssets();
                TimelineEditor.RefreshEntity();
            }
        }
        void OnPointerDown(PointerDownEvent e)
        {
            if (e.button == 0)
            {
                TimelineEditor.InspectorContent.FreshInspector(Asset, UpdateAsset);
            }
            else if (e.button == 1)
            {
                menu.AppendAction("修正长度", e =>
                {
                    var clip = (Asset as AnimationClipAsset).clip;
                    var endFrame = clip.length * TimelineMgr.Ins.FrameRate;
                    var oldEndFrame = Asset.EndFrame;
                    Asset.EndFrame = Asset.StartFrame + Mathf.RoundToInt(endFrame);
                    if (!TrackItem.IsValid())
                    {
                        Asset.EndFrame = oldEndFrame;
                    }
                    else
                    {
                        UpdateView();
                    }
                }, e => DropdownMenuAction.Status.Normal);
                this.ShowMenu();
            }
        }
        bool UpdateAsset()
        {
            UpdateView();
            return TrackItem.IsValid();
        }
        public void Init(TimelineClipAsset asset, TimelineTrackItem track)
        {
            Asset = asset;
            TrackItem = track;
            color = TrackItem.Asset.GetType().GetAttribute<TLTrackAttribute>().Color;
        }

        bool IsPointInTriangle(Point p, Point a, Point b, Point c)
        {
            bool b1, b2, b3;

            b1 = Point.CrossProduct(p, a, b) < 0.0f;
            b2 = Point.CrossProduct(p, b, c) < 0.0f;
            b3 = Point.CrossProduct(p, c, a) < 0.0f;

            return ((b1 == b2) && (b2 == b3));
        }
        bool isDrag => Status != Status.None;
        int startFrame;
        int endFrame;
        public void ToDown(int frame)
        {
            if (frame >= Asset.StartFrame && frame <= Asset.EndFrame)
            {
                var x = TimelineEditor.GetPositionByFrame(frame);
                var sx = TimelineEditor.GetPositionByFrame(Asset.StartFrame);
                var ex = TimelineEditor.GetPositionByFrame(Asset.EndFrame);
                if (x - sx < 20)
                {
                    Status = Status.Left;
                }
                else if (ex - x < 20)
                {
                    Status = Status.Right;
                }
                else
                {
                    if (Asset.InFrame > 0)
                    {
                        var pos = this.WorldToLocal(Event.current.mousePosition);
                        var ix = TimelineEditor.GetPositionByFrame(Asset.InFrame);
                        var a = new Point(sx, 0);
                        var b = new Point(sx, 30);
                        var c = new Point(ix, 30);
                        var p = new Point(x, pos.y);
                        bool isInTriangle = IsPointInTriangle(p, a, b, c);
                        //交叉多边形的下部分
                        if (isInTriangle)
                        {
                            Status = Status.None;
                            return;
                        }
                    }
                    if (Asset.OutFrame > 0)
                    {
                        var pos = this.WorldToLocal(Event.current.mousePosition);
                        var ox = TimelineEditor.GetPositionByFrame(Asset.OutFrame);
                        var a = new Point(ox, 0);
                        var b = new Point(ex, 0);
                        var c = new Point(ex, 30);
                        var p = new Point(x, pos.y);
                        bool isInTriangle = IsPointInTriangle(p, a, b, c);
                        //交叉多边形的上部分
                        if (isInTriangle)
                        {
                            Status = Status.None;
                            return;
                        }
                    }
                    Status = Status.Move;
                }

                startFrame = Asset.StartFrame;
                endFrame = Asset.EndFrame;
                return;
            }
            Status = Status.None;
        }

        public void ToDrag(int now, int last)
        {
            if (Status == Status.None)
            {
                return;
            }
            switch (Status)
            {
                case Status.Left:
                    if (now < 0)
                    {
                        now = 0;
                    }
                    if (now >= Asset.EndFrame)
                    {
                        now = Asset.EndFrame - 1;
                    }
                    Asset.StartFrame = now;
                    break;
                case Status.Right:
                    if (now < Asset.StartFrame + 1)
                    {
                        now = Asset.StartFrame + 1;
                    }
                    Asset.EndFrame = now;
                    break;
                case Status.Move:
                    var offFrame = now - last;
                    if (Asset.StartFrame + offFrame < 0)
                    {
                        offFrame = 0 - Asset.StartFrame;
                    }
                    Asset.StartFrame += offFrame;
                    Asset.EndFrame += offFrame;
                    break;
            }

            UpdateView();
        }

        public void ToUp()
        {
            if (Status == Status.None)
            {
                return;
            }
            Status = Status.None;

            if (!TrackItem.IsValid())
            {
                Asset.StartFrame = startFrame;
                Asset.EndFrame = endFrame;
            }
            UpdateView();
        }

        public void RefreshWidth()
        {
            var sx = Asset.InFrame > 0 ?
               TimelineEditor.GetPositionByFrame(Asset.InFrame) :
               TimelineEditor.GetPositionByFrame(Asset.StartFrame);

            var ex = Asset.OutFrame > 0 ?
                TimelineEditor.GetPositionByFrame(Asset.OutFrame) :
                TimelineEditor.GetPositionByFrame(Asset.EndFrame);

            style.left = sx;
            style.width = ex - sx;
        }
        public void UpdateView()
        {
            lbType.text = Asset.clipName;
            var lineWidth = 1;
            if (TrackItem.IsValid())
            {
                content.style.borderLeftWidth = lineWidth;
                content.style.borderRightWidth = lineWidth;
                content.style.borderTopWidth = lineWidth;
                content.style.borderBottomWidth = lineWidth;

                content.style.borderLeftColor = isDrag ? Color.white : color;
                content.style.borderRightColor = isDrag ? Color.white : color;
                content.style.borderTopColor = isDrag ? Color.white : color;
                content.style.borderBottomColor = isDrag ? Color.white : color;
            }
            else
            {
                BringToFront();
                content.style.borderLeftWidth = lineWidth;
                content.style.borderRightWidth = lineWidth;
                content.style.borderTopWidth = lineWidth;
                content.style.borderBottomWidth = lineWidth;

                content.style.borderLeftColor = new StyleColor(Color.red);
                content.style.borderRightColor = new StyleColor(Color.red);
                content.style.borderTopColor = new StyleColor(Color.red);
                content.style.borderBottomColor = new StyleColor(Color.red);
            }
            TrackItem.UpdateItemData();
        }
    }
}
