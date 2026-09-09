using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.Timeline
{
    public partial class TimelineTrackItem : VisualElement, IToolbarMenuElement
    {
        public ITimelineEditorTrack Track { get; }
        VisualElement clipContent;
        VisualElement clipParent;
        VisualElement draw;
        public List<TimelineClipItem> Items { get; } = new();
        readonly List<VisualElement> mixVe = new();
        readonly Queue<VisualElement> pool = new();
        int lastFrame;
        TimelineClipItem selectItem;
        bool menuInitialized;

        public DropdownMenu menu { get; }

        public TimelineTrackItem(ITimelineEditorTrack track)
        {
            Track = track;
            menu = new DropdownMenu();
            BuildVisualTree();

            inputName.SetValueWithoutNotify(track.Name);
            inputName.SetEnabled(track.CanRename);
            lbType.text = track.DisplayTypeName;
            content.style.borderLeftColor = track.Color;

            RegisterCallback<PointerDownEvent>(OnPointerDown);

            clipContent = new VisualElement();
            clipContent.style.height = 34;
            clipContent.style.minHeight = 34;
            clipContent.style.flexShrink = 0;
            clipContent.style.left = 0;
            clipContent.style.right = 0;
            clipContent.style.backgroundColor = new Color(0.17f, 0.17f, 0.17f, 0.85f);
            clipContent.style.borderBottomWidth = 1;
            clipContent.style.borderBottomColor = new Color(0, 0, 0, 0.45f);
            clipParent = new VisualElement();
            clipParent.style.position = new StyleEnum<Position>(Position.Absolute);
            clipParent.style.top = 0;
            clipParent.style.bottom = 0;
            clipParent.style.left = 0;
            clipParent.style.right = 0;
            clipContent.Add(clipParent);
            draw = new VisualElement();
            draw.pickingMode = PickingMode.Ignore;
            draw.style.position = new StyleEnum<Position>(Position.Absolute);
            draw.style.top = 0;
            draw.style.bottom = 0;
            draw.style.left = 0;
            draw.style.right = 0;
            draw.generateVisualContent += OnDrawContent;
            clipContent.Add(draw);
            TimelineWindow.ClipContent?.Add(clipContent);
            TimelineWindow.RefreshClip += OnWheelChanged;
            Track.Bind(UpdateTrack);
            ElementDrag.Add(clipContent, TimelineWindow.ClipContent, OnStart, OnDrag, OnEnd);
            clipContent.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            clipContent.RegisterCallback<DragPerformEvent>(OnDragPerform);
            RefreshView();
        }

        void BuildVisualTree()
        {
            style.height = 34;
            style.minHeight = 34;
            style.flexShrink = 0;

            root = new VisualElement();
            root.style.flexGrow = 1;
            Add(root);

            content = new VisualElement();
            content.style.flexGrow = 1;
            content.style.flexDirection = FlexDirection.Row;
            content.style.alignItems = Align.Center;
            content.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            content.style.borderLeftWidth = 4;
            content.style.borderBottomWidth = 1;
            content.style.borderBottomColor = new Color(0, 0, 0, 0.5f);
            root.Add(content);

            lbType = new Label();
            lbType.style.width = 58;
            lbType.style.minWidth = 58;
            lbType.style.unityTextAlign = TextAnchor.MiddleCenter;
            lbType.style.color = new Color(0.82f, 0.82f, 0.82f);
            content.Add(lbType);

            inputName = new TextField();
            inputName.style.flexGrow = 1;
            inputName.style.marginLeft = 0;
            inputName.style.marginRight = 2;
            inputName.RegisterValueChangedCallback(_OnInputNameChanged);
            content.Add(inputName);

            var menuButton = new Button(ShowContextMenu) { text = "⋮" };
            menuButton.style.width = 24;
            menuButton.style.minWidth = 24;
            menuButton.tooltip = "轨道菜单";
            content.Add(menuButton);
        }

        void ShowContextMenu()
        {
            if (!TimelineWindow.IsValid())
            {
                return;
            }
            EnsureMenu();
            this.ShowMenu();
        }

        void EnsureMenu()
        {
            if (menuInitialized)
            {
                return;
            }
            menu.AppendAction("添加 Clip", _ => CreateClip(null),
                _ => Track.Source.CanEdit && Track.CanCreateClip
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            menu.AppendAction("删除轨道", _ => RemoveTrack(),
                _ => Track.Source.CanEdit && Track.CanRemove
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            menuInitialized = true;
        }

        public void Release()
        {
            foreach (var item in Items)
            {
                item.Release();
            }
            Items.Clear();
            TimelineWindow.ClipContent?.Remove(clipContent);
            TimelineWindow.RefreshClip -= OnWheelChanged;
            Track.Unbind(UpdateTrack);
        }

        void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
        }

        void OnDragPerform(DragPerformEvent evt)
        {
            if (!Track.CanCreateClip)
            {
                return;
            }
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                CreateClip(DragAndDrop.paths[0]);
            }
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0)
            {
                TimelineWindow.InspectorContent?.FreshInspector(Track, null);
            }
            else if (evt.button == 1)
            {
                if (!TimelineWindow.IsValid())
                {
                    return;
                }
                EnsureMenu();
                this.ShowMenu();
                evt.StopPropagation();
            }
        }

        void RemoveTrack()
        {
            if (Track.CanRemove)
            {
                Track.Remove();
            }
        }

        void CreateClip(string assetPath)
        {
            if (!Track.CanCreateClip)
            {
                return;
            }
            var clip = Track.CreateClip(assetPath);
            if (clip == null)
            {
                return;
            }
            RefreshView();
            TimelineWindow.InspectorContent?.FreshInspector(clip, IsValid);
        }

        void UpdateTrack()
        {
            inputName.SetValueWithoutNotify(Track.Name);
        }

        partial void _OnInputNameChanged(ChangeEvent<string> evt)
        {
            Track.Rename(evt.newValue);
        }

        void RefreshView()
        {
            foreach (var item in Items)
            {
                item.Release();
                clipParent.Remove(item);
            }
            Items.Clear();

            foreach (var clip in Track.Clips)
            {
                var item = new TimelineClipItem(clip, this);
                Items.Add(item);
                clipParent.Add(item);
            }

            foreach (var item in Items)
            {
                item.UpdateView();
            }
        }

        public void RemoveClipItem(TimelineClipItem item)
        {
            if (item == null || !Track.RemoveClip(item.Clip))
            {
                return;
            }

            Items.Remove(item);
            item.Release();
            clipParent.Remove(item);
            if (Items.Count > 0)
            {
                Items[0].FreshInspector();
            }
            else
            {
                TimelineWindow.InspectorContent?.FreshInspector(Track, null);
            }
        }

        void OnStart()
        {
            var candidates = new List<TimelineClipItem>();
            selectItem = null;
            if (Items.Count == 0)
            {
                return;
            }

            lastFrame = TimelineWindow.GetFrameByMousePosition();
            foreach (var item in Items)
            {
                item.ToDown(lastFrame);
                if (item.Status != DragStatus.None)
                {
                    candidates.Add(item);
                }
            }
            candidates.Sort((first, second) => first.Status - second.Status);
            if (candidates.Count > 0)
            {
                selectItem = candidates[0];
                selectItem.Clip.BeginDrag();
            }
        }

        void OnDrag(Vector2 delta)
        {
            if (selectItem == null)
            {
                return;
            }
            var now = TimelineWindow.GetFrameByMousePosition();
            selectItem.ToDrag(now, lastFrame);
            lastFrame = now;
        }

        void OnEnd()
        {
            if (selectItem == null)
            {
                return;
            }
            foreach (var item in Items)
            {
                item.ToUp();
            }
            selectItem.Clip.CommitEdit();
            TimelineWindow.wnd?.clipView?.RefreshLayout();
            selectItem = null;
        }

        void OnDrawContent(MeshGenerationContext context)
        {
            var paint2D = context.painter2D;
            paint2D.strokeColor = new Color(0, 0, 0, 0.5f);
            paint2D.lineWidth = 1;
            paint2D.BeginPath();
            const int minY = 3;
            const int maxY = 27;
            foreach (var item in Items)
            {
                var clip = item.Clip;
                var startX = TimelineWindow.GetPositionByFrame(clip.StartFrame);
                if (clip.InFrame > 0)
                {
                    var inX = TimelineWindow.GetPositionByFrame(clip.InFrame);
                    paint2D.MoveTo(new Vector2(startX, minY));
                    paint2D.LineTo(new Vector2(inX, maxY));
                }
            }
            paint2D.Stroke();
        }

        void OnWheelChanged()
        {
            foreach (var item in Items)
            {
                item.RefreshWidth();
            }
            ClipMarkDirtyRepaint();
        }

        public void ClipMarkDirtyRepaint()
        {
            draw.MarkDirtyRepaint();
            foreach (var element in mixVe)
            {
                element.style.display = DisplayStyle.None;
                pool.Enqueue(element);
            }
            mixVe.Clear();

            foreach (var item in Items)
            {
                var clip = item.Clip;
                if (clip.InFrame <= 0)
                {
                    continue;
                }

                VisualElement element;
                if (pool.Count > 0)
                {
                    element = pool.Dequeue();
                }
                else
                {
                    element = new VisualElement();
                    element.style.backgroundColor = new Color(0.2f, 0.3f, 0.3f, 0.8f);
                    clipParent.Add(element);
                }
                const int lineWidth = 1;
                element.style.borderLeftWidth = lineWidth;
                element.style.borderRightWidth = lineWidth;
                element.style.borderTopWidth = lineWidth;
                element.style.borderBottomWidth = lineWidth;
                element.style.borderLeftColor = new Color(0, 0, 0, 0.5f);
                element.style.borderRightColor = new Color(0, 0, 0, 0.5f);
                element.style.borderTopColor = new Color(0, 0, 0, 0.5f);
                element.style.borderBottomColor = new Color(0, 0, 0, 0.5f);

                mixVe.Add(element);
                element.style.display = DisplayStyle.Flex;
                var startX = TimelineWindow.GetPositionByFrame(clip.StartFrame);
                var inX = TimelineWindow.GetPositionByFrame(clip.InFrame);
                element.style.width = inX - startX;
                element.style.height = 34;
                element.style.left = startX;
                element.style.marginBottom = 2;
                element.style.marginTop = 2;
            }
        }

        public bool IsValid()
        {
            return Track.IsLayoutValid();
        }

        public void UpdateItemData()
        {
            Track.UpdateMixData();
            foreach (var item in Items)
            {
                item.RefreshWidth();
            }
            ClipMarkDirtyRepaint();
        }
    }
}
