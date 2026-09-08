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

        // 计算向量叉积
        public static double CrossProduct(Point O, Point A, Point B)
        {
            return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
        }
    }
    public partial class TimelineClipItem : VisualElement, IToolbarMenuElement
    {
        public DragStatus Status { get; private set; }
        Color color;
        public TimelineClipAsset Asset { get; private set; }
        public TimelineTrackItem TrackItem { get; private set; }
        public DropdownMenu menu { get; }
        bool menuInitialized;
        public TimelineClipItem(TimelineClipAsset asset, TimelineTrackItem track)
        {
            Asset = asset;
            TrackItem = track;
            color = TrackItem.Asset.GetType().GetAttribute<TLTrackAttribute>()?.Color
                ?? new Color(0.4f, 0.75f, 0.75f);

            BuildVisualTree();
            menu = new DropdownMenu();
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<DragUpdatedEvent>(_OnDragUpd);
            RegisterCallback<DragPerformEvent>(_OnDragPerform);
            TimelineWindow.Bind(Asset, UpdateView);
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
            TimelineWindow.UnBind(Asset, UpdateView);
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
                if (Asset is not AnimationClipAsset animationAsset)
                {
                    return;
                }
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(retPath);
                if (clip == null)
                {
                    return;
                }
                animationAsset.clip = clip;
                animationAsset.clipName = clip.name;
                animationAsset.EndFrame = animationAsset.StartFrame +
                    Mathf.Max(1, Mathf.RoundToInt(clip.length * TimelineWindow.FrameRate));
                TimelineWindow.SaveAssets();
                TimelineWindow.RefreshEntity();
                UpdateView();
            }
        }
        void OnPointerDown(PointerDownEvent e)
        {            
            if (e.button == 0)
            {
                FreshInspector();
            }
            else if (e.button == 1)
            {
                if (!TimelineWindow.IsValid()) return;
                if (!menuInitialized)
                {
                    if (Asset is AnimationClipAsset)
                    {
                        menu.AppendAction("适配长度", _ => FitAnimationDuration(),
                            _ => (Asset as AnimationClipAsset)?.clip != null
                                ? DropdownMenuAction.Status.Normal
                                : DropdownMenuAction.Status.Disabled);
                    }
                    menu.AppendAction("删除", _ => TrackItem.RemoveClipItem(this),
                        _ => DropdownMenuAction.Status.Normal);
                    menuInitialized = true;
                }
                this.ShowMenu();
            }
        }
        void FitAnimationDuration()
        {
            if (Asset is not AnimationClipAsset animationAsset || animationAsset.clip == null)
            {
                return;
            }

            var oldEndFrame = Asset.EndFrame;
            Asset.EndFrame = Asset.StartFrame +
                Mathf.Max(1, Mathf.RoundToInt(animationAsset.clip.length * TimelineWindow.FrameRate));
            if (!TrackItem.IsValid())
            {
                Asset.EndFrame = oldEndFrame;
            }
            UpdateView();
            TimelineWindow.SaveAssets();
            TimelineWindow.RefreshEntity?.Invoke();
            TimelineWindow.wnd?.clipView?.RefreshLayout();
        }

        bool ChcekValid()
        {
            return TrackItem.IsValid();         
        }

        public void FreshInspector()
        {
            TimelineWindow.InspectorContent.FreshInspector(Asset, ChcekValid);
        }

        bool IsPointInTriangle(Point p, Point a, Point b, Point c)
        {
            bool b1, b2, b3;

            b1 = Point.CrossProduct(p, a, b) < 0.0f;
            b2 = Point.CrossProduct(p, b, c) < 0.0f;
            b3 = Point.CrossProduct(p, c, a) < 0.0f;

            return ((b1 == b2) && (b2 == b3));
        }
        bool isDrag => Status != DragStatus.None;
        int startFrame;
        int endFrame;
        public void ToDown(int frame)
        {
            if (frame >= Asset.StartFrame && frame <= Asset.EndFrame)
            {
                var x = TimelineWindow.GetPositionByFrame(frame);
                var sx = TimelineWindow.GetPositionByFrame(Asset.StartFrame);
                var ex = TimelineWindow.GetPositionByFrame(Asset.EndFrame);
                if (x - sx < 20)
                {
                    Status = DragStatus.Left;
                }
                else if (ex - x < 20)
                {
                    Status = DragStatus.Right;
                }
                else
                {
                    if (Asset.InFrame > 0)
                    {
                        var pos = this.WorldToLocal(Event.current.mousePosition);
                        var ix = TimelineWindow.GetPositionByFrame(Asset.InFrame);
                        var a = new Point(sx, 0);
                        var b = new Point(sx, 30);
                        var c = new Point(ix, 30);
                        var p = new Point(x, pos.y);
                        bool isInTriangle = IsPointInTriangle(p, a, b, c);
                        //不在三角形区域内
                        if (isInTriangle)
                        {
                            Status = DragStatus.None;
                            return;
                        }
                    }
                    if (Asset.OutFrame > 0)
                    {
                        var pos = this.WorldToLocal(Event.current.mousePosition);
                        var ox = TimelineWindow.GetPositionByFrame(Asset.OutFrame);
                        var a = new Point(ox, 0);
                        var b = new Point(ex, 0);
                        var c = new Point(ex, 30);
                        var p = new Point(x, pos.y);
                        bool isInTriangle = IsPointInTriangle(p, a, b, c);
                        //不在三角形区域外
                        if (isInTriangle)
                        {
                            Status = DragStatus.None;
                            return;
                        }
                    }
                    Status = DragStatus.Move;
                }

                startFrame = Asset.StartFrame;
                endFrame = Asset.EndFrame;
                return;
            }
            Status = DragStatus.None;
        }

        public void ToDrag(int now, int last)
        {
            if (Status == DragStatus.None)
            {
                return;
            }
            switch (Status)
            {
                case DragStatus.Left:
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
                case DragStatus.Right:
                    if (now < Asset.StartFrame + 1)
                    {
                        now = Asset.StartFrame + 1;
                    }
                    Asset.EndFrame = now;
                    break;
                case DragStatus.Move:
                    var offFrame = now - last;
                    if (Asset.StartFrame + offFrame < 0)
                    {
                        offFrame = 0 - Asset.StartFrame;
                    }
                    Asset.StartFrame += offFrame;
                    Asset.EndFrame += offFrame;
                    break;
            }
            TimelineWindow.Run(Asset);
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
                Asset.StartFrame = startFrame;
                Asset.EndFrame = endFrame;
                TimelineWindow.Run(Asset);
            }
            UpdateView();
        }

        public void RefreshWidth()
        {
            var sx = Asset.InFrame > 0 ?
               TimelineWindow.GetPositionByFrame(Asset.InFrame) :
               TimelineWindow.GetPositionByFrame(Asset.StartFrame);

            var ex = Asset.OutFrame > 0 ?
                TimelineWindow.GetPositionByFrame(Asset.OutFrame) :
                TimelineWindow.GetPositionByFrame(Asset.EndFrame);

            style.left = sx;
            style.width = Mathf.Max(2, ex - sx);
        }
        public void UpdateView()
        {
            lbType.text = string.IsNullOrEmpty(Asset.clipName) ? Asset.GetType().Name : Asset.clipName;
            // Detailed timing is shown in the inspector after selection. A native tooltip
            // here floats over neighbouring tracks and looks like an extra clip.
            tooltip = null;
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
