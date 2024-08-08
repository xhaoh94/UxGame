using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
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
    public class TimelineClipItem : VisualElement, IToolbarMenuElement
    {
        public Status Status { get; private set; }
        Color color;
        public TimelineClipAsset Asset { get; private set; }
        public TrackClipContent ClipContent { get; private set; }
        VisualElement content;
        VisualElement left;
        VisualElement center;
        VisualElement right;
        Label lbType;
        public DropdownMenu menu { get; }
        public TimelineClipItem()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/Uxml/TimelineClipItem.uxml");
            visualTree.CloneTree(this);
            content = this.Q<VisualElement>("content");

            left = this.Q<VisualElement>("left");
            center = this.Q<VisualElement>("center");
            right = this.Q<VisualElement>("right");
            lbType = this.Q<Label>("lbType");

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
                Timeline.SaveAssets();
                Timeline.RefreshEntity();
            }
        }
        void OnPointerDown(PointerDownEvent e)
        {
            if (e.button == 0)
            {

            }
            else if (e.button == 1)
            {
                menu.AppendAction("修正长度", e =>
                {
                    var clip = (Asset as AnimationClipAsset).clip;
                    var endFrame = clip.length * TimelineMgr.Ins.FrameRate;
                    Asset.EndFrame = Asset.StartFrame + Mathf.RoundToInt(endFrame);
                    UpdateView();
                }, e => DropdownMenuAction.Status.Normal);
                this.ShowMenu();
            }
        }



        public void Init(TimelineClipAsset asset, TrackClipContent track)
        {
            Asset = asset;
            ClipContent = track;
            color = ClipContent.TrackItem.Asset.GetType().GetAttribute<TLTrackAttribute>().Color;
            //ElementDrag.Add(left, Timeline.ClipContent, OnStart, OnLeftDrag, OnEnd);
            //ElementDrag.Add(right, Timeline.ClipContent, OnStart, OnRightDrag, OnEnd);
            //ElementDrag.Add(center, Timeline.ClipContent, OnStart, OnDrag, OnEnd);
            UpdateView();
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
            //var frame = Timeline.GetFrameByMousePosition();
            if (frame >= Asset.StartFrame && frame <= Asset.EndFrame)
            {
                var x = Timeline.GetPositionByFrame(frame);
                var sx = Timeline.GetPositionByFrame(Asset.StartFrame);
                var ex = Timeline.GetPositionByFrame(Asset.EndFrame);
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
                        var ix = Timeline.GetPositionByFrame(Asset.InFrame);
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
                        var ox = Timeline.GetPositionByFrame(Asset.OutFrame);
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
                    if (now < Asset.StartFrame)
                    {
                        now = Asset.StartFrame;
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
            ClipContent.ClipMarkDirtyRepaint();
        }

        public void ToUp()
        {
            if (Status == Status.None)
            {
                return;
            }
            Status = Status.None;

            if (!ClipContent.IsValid())
            {
                Asset.StartFrame = startFrame;
                Asset.EndFrame = endFrame;
            }
            UpdateView();
        }

        //void OnDrag(bool left)
        //{
        //    var frame = Timeline.GetFrameByMousePosition();

        //    if (left)
        //    {
        //        if (frame < 0)
        //        {
        //            frame = 0;
        //        }
        //        if (frame >= Asset.EndFrame)
        //        {
        //            frame = Asset.EndFrame - 1;
        //        }
        //        Asset.StartFrame = frame;
        //    }
        //    else
        //    {
        //        if (frame < Asset.StartFrame)
        //        {
        //            frame = Asset.StartFrame;
        //        }

        //        Asset.EndFrame = frame;
        //    }
        //    UpdateView();
        //    ClipContent.ClipMarkDirtyRepaint();
        //}


        //void OnLeftDrag(Vector2 e)
        //{
        //    OnDrag(true);
        //}
        //void OnRightDrag(Vector2 e)
        //{
        //    OnDrag(false);
        //}



        //void OnEnd()
        //{            
        //    if (!ClipContent.IsValid())
        //    {
        //        Asset.StartFrame = startFrame;
        //        Asset.EndFrame = endFrame;
        //    }
        //    UpdateView();
        //}
        //void OnDrag(Vector2 e)
        //{
        //    var dragFrame = Timeline.GetFrameByMousePosition();
        //    var offFrame = dragFrame - lastFrame;
        //    lastFrame = dragFrame;
        //    if (Asset.StartFrame + offFrame < 0)
        //    {
        //        offFrame = 0 - Asset.StartFrame;
        //    }
        //    Asset.StartFrame += offFrame;
        //    Asset.EndFrame += offFrame;
        //    UpdateView();
        //    ClipContent.ClipMarkDirtyRepaint();
        //}


        public void UpdateView()
        {
            var sx = Timeline.GetPositionByFrame(Asset.StartFrame);
            var ex = Timeline.GetPositionByFrame(Asset.EndFrame);
            var tFrame = (float)(Asset.EndFrame - Asset.StartFrame);
            lbType.text = Asset.Name;
            var width = ex - sx;
            if (Asset.InFrame > 0)
            {
                lbType.style.left = width * ((Asset.InFrame - Asset.StartFrame) / tFrame);
            }
            else
            {
                lbType.style.left = 0;
            }
            if (Asset.OutFrame > 0)
            {
                lbType.style.right = width - (width * ((Asset.OutFrame - Asset.StartFrame) / tFrame));
            }
            else
            {
                lbType.style.right = 0;
            }
            style.left = sx;
            style.width = width;

            if (ClipContent.IsValid())
            {
                //content.style.borderLeftWidth = 1;
                //content.style.borderRightWidth = 1;
                //content.style.borderTopWidth = 1;
                content.style.borderBottomWidth = 3;

                content.style.borderLeftColor = isDrag ? Color.white : color;
                content.style.borderRightColor = isDrag ? Color.white : color;
                content.style.borderTopColor = isDrag ? Color.white : color;
                content.style.borderBottomColor = isDrag ? Color.white : color;
            }
            else
            {
                //content.style.borderLeftWidth = 1;
                //content.style.borderRightWidth = 1;
                //content.style.borderTopWidth = 1;
                content.style.borderBottomWidth = 3;

                //content.style.borderLeftColor = new StyleColor(Color.red);
                //content.style.borderRightColor = new StyleColor(Color.red);
                //content.style.borderTopColor = new StyleColor(Color.red);
                content.style.borderBottomColor = new StyleColor(Color.red);
            }
            ClipContent.UpdateClipData();
        }
    }
}
