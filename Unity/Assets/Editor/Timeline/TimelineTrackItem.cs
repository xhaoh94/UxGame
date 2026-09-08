using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline
{
    public partial class TimelineTrackItem : VisualElement, IToolbarMenuElement
    {
        public TimelineTrackAsset Asset { get; private set; }
        Type ClipType
        {
            get
            {
                var attr = Asset.GetType().GetAttribute<TLTrackClipTypeAttribute>();
                return attr?.ClipType;
            }
        }

        VisualElement clipContent;
        VisualElement clipParent;
        VisualElement draw;
        public List<TimelineClipItem> Items { get; } = new();
        List<VisualElement> mixVe = new List<VisualElement>();
        Queue<VisualElement> pool = new Queue<VisualElement>();
        int lastFrame;
        TimelineClipItem selectItem;
        bool menuInitialized;

        public DropdownMenu menu { get; }
        public TimelineTrackItem(TimelineTrackAsset asset)
        {
            Asset = asset;
            menu = new DropdownMenu();
            BuildVisualTree();

            var attr = asset.GetType().GetAttribute<TLTrackAttribute>();
            inputName.SetValueWithoutNotify(asset.trackName);
            lbType.text = attr?.Lb ?? asset.GetType().Name;
            var trackColor = attr?.Color ?? new Color(0.4f, 0.7f, 0.7f);
            content.style.borderLeftColor = trackColor;

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
            TimelineWindow.Bind(Asset, UpdateAsset);
            ElementDrag.Add(clipContent, TimelineWindow.ClipContent, OnStart, OnDrag, OnEnd);
            clipContent.RegisterCallback<DragUpdatedEvent>(OnDragUpd);
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
            menu.AppendAction("添加 Clip", _ => CreateClipAsset(null),
                _ => DropdownMenuAction.Status.Normal);
            menu.AppendAction("删除轨道", _ => RemoveTrack(),
                _ => DropdownMenuAction.Status.Normal);
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
            TimelineWindow.UnBind(Asset, UpdateAsset);
        }


        void OnDragUpd(DragUpdatedEvent e)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
        }
        void OnDragPerform(DragPerformEvent e)
        {
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                string retPath = DragAndDrop.paths[0];
                CreateClipAsset(retPath);
            }
        }
        void OnPointerDown(PointerDownEvent e)
        {
            if (e.button == 0)
            {
                TimelineWindow.InspectorContent.FreshInspector(Asset, null);
            }
            else if (e.button == 1)
            {
                if (!TimelineWindow.IsValid()) return;
                EnsureMenu();
                this.ShowMenu();
                e.StopPropagation();
            }
        }
        void RemoveTrack()
        {
            if (TimelineWindow.Asset == null || !TimelineWindow.Asset.tracks.Contains(Asset))
            {
                return;
            }
            TimelineWindow.Undo.RegUndo("timeline_remove_track", TimelineWindow.Asset,
                () => TimelineWindow.RefreshView?.Invoke());
            TimelineWindow.Asset.tracks.Remove(Asset);
            TimelineWindow.SaveAssets();
            TimelineWindow.RefreshView?.Invoke();
            TimelineWindow.RefreshEntity?.Invoke();
        }

        void CreateClipAsset(string retPath)
        {
            var clipAsset = TimelineWindow.CreateClipAsset(
                ClipType, TimelineWindow.GetDurationFrame(Asset), retPath);
            if (clipAsset != null)
            {
                AddClipItem(clipAsset);
            }
        }
        void UpdateAsset()
        {
            inputName.SetValueWithoutNotify(Asset.trackName);
        }
        partial void _OnInputNameChanged(ChangeEvent<string> e)
        {
            Asset.trackName = e.newValue;
            TimelineWindow.Run(Asset);
            TimelineWindow.SaveAssets?.Invoke();
        }

        void RefreshView()
        {
            if (Items.Count > 0)
            {
                foreach (var item in Items)
                {
                    item.Release();
                    clipParent.Remove(item);
                }
                Items.Clear();
            }

            foreach (var clipAsset in Asset.clips)
            {
                var item = new TimelineClipItem(clipAsset, this);
                Items.Add(item);
                clipParent.Add(item);
            }

            foreach (var item in Items)
            {
                item.UpdateView();
            }
        }

        public void AddClipItem(TimelineClipAsset clipAsset)
        {
            if (Asset.clips.Contains(clipAsset))
            {
                return;
            }
            TimelineWindow.Undo.RegUndo("track_add_clip_item", TimelineWindow.Asset, () => {
                TimelineWindow.SaveAssets();
                RefreshView();
            });
            Asset.clips.Add(clipAsset);
            TimelineWindow.SaveAssets();
            var item = new TimelineClipItem(clipAsset, this);
            Items.Add(item);
            clipParent.Add(item);
            item.UpdateView();
            TimelineWindow.RefreshEntity();
            item.FreshInspector();
        }
        public void RemoveClipItem(TimelineClipItem item)
        {
            if (!Asset.clips.Contains(item.Asset))
            {
                return;
            }
            TimelineWindow.Undo.RegUndo("track_remove_clip_item", TimelineWindow.Asset, () => {
                TimelineWindow.SaveAssets();
                RefreshView();
            });
            Asset.clips.Remove(item.Asset);
            TimelineWindow.SaveAssets();
            Items.Remove(item);
            item.Release();
            clipParent.Remove(item);
            TimelineWindow.RefreshEntity();
            if (Items.Count > 0)
            {
                Items[0].FreshInspector();
            }
        }


        void OnStart()
        {
            var _temItems = new List<TimelineClipItem>();
            selectItem = null;
            if (Items.Count == 0) return;
            lastFrame = TimelineWindow.GetFrameByMousePosition();
            foreach (var item in Items)
            {
                item.ToDown(lastFrame);
                if (item.Status != DragStatus.None)
                {
                    _temItems.Add(item);
                }
            }

            _temItems.Sort((a, b) =>
            {
                return a.Status - b.Status;
            });

            if (_temItems.Count > 0)
            {
                TimelineWindow.Undo.RegUndo("drag_track", TimelineWindow.Asset, RefreshView);
                selectItem = _temItems[0];
            }
        }
        void OnDrag(Vector2 e)
        {
            if (selectItem == null) return;
            var now = TimelineWindow.GetFrameByMousePosition();
            selectItem?.ToDrag(now, lastFrame);
            lastFrame = now;
        }
        void OnEnd()
        {
            if (selectItem == null) return;
            foreach (var item in Items)
            {
                item.ToUp();
            }
            TimelineWindow.SaveAssets();
            TimelineWindow.RefreshEntity?.Invoke();
            TimelineWindow.wnd?.clipView?.RefreshLayout();
            selectItem = null;
        }

        void OnDrawContent(MeshGenerationContext mgc)
        {
            var paint2D = mgc.painter2D;
            paint2D.strokeColor = new Color(0, 0, 0, 0.5f);
            paint2D.lineWidth = 1;
            paint2D.BeginPath();
            int minY = 3;
            int maxY = 27;
            foreach (var item in Items)
            {
                var asset = item.Asset;
                var sx = TimelineWindow.GetPositionByFrame(asset.StartFrame);
                var ex = TimelineWindow.GetPositionByFrame(asset.EndFrame);

                if (asset.InFrame > 0)
                {
                    var ix = TimelineWindow.GetPositionByFrame(asset.InFrame);
                    paint2D.MoveTo(new Vector2(sx, minY));
                    paint2D.LineTo(new Vector2(ix, maxY));
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
            foreach (var ve in mixVe)
            {
                ve.style.display = DisplayStyle.None;
                pool.Enqueue(ve);
            }
            mixVe.Clear();

            foreach (var item in Items)
            {
                var asset = item.Asset;
                var sx = TimelineWindow.GetPositionByFrame(asset.StartFrame);
                var ex = TimelineWindow.GetPositionByFrame(asset.EndFrame);

                if (asset.InFrame > 0)
                {
                    VisualElement ve;
                    if (pool.Count > 0)
                    {
                        ve = pool.Dequeue();
                    }
                    else
                    {
                        ve = new VisualElement();
                        ve.style.backgroundColor = new Color(0.2f, 0.3f, 0.3f, 0.8f);
                        clipParent.Add(ve);
                    }
                    var lineWidth = 1;
                    ve.style.borderLeftWidth = lineWidth;
                    ve.style.borderRightWidth = lineWidth;
                    ve.style.borderTopWidth = lineWidth;
                    ve.style.borderBottomWidth = lineWidth;

                    ve.style.borderLeftColor = new Color(0, 0, 0, 0.5f);
                    ve.style.borderRightColor = new Color(0, 0, 0, 0.5f);
                    ve.style.borderTopColor = new Color(0, 0, 0, 0.5f);
                    ve.style.borderBottomColor = new Color(0, 0, 0, 0.5f);

                    mixVe.Add(ve);
                    ve.style.display = DisplayStyle.Flex;
                    var ix = TimelineWindow.GetPositionByFrame(asset.InFrame);
                    ve.style.width = ix - sx;
                    ve.style.height = 34;
                    ve.style.left = sx;
                    ve.style.marginBottom = 2;
                    ve.style.marginTop = 2;
                }
            }
        }

        public bool IsValid()
        {
            Dictionary<TimelineClipAsset, int> kvs = new Dictionary<TimelineClipAsset, int>();
            foreach (var clip1 in Items)
            {
                foreach (var clip2 in Items)
                {
                    if (clip1 == clip2) continue;
                    var t = Intersect(clip1.Asset, clip2.Asset);
                    if (t == -1000) return false;
                    if (t != 0)
                    {
                        var sideMask = t > 0 ? 1 : 2;
                        if (kvs.TryGetValue(clip1.Asset, out var mask))
                        {
                            if ((mask & sideMask) != 0)
                            {
                                return false;
                            }
                            kvs[clip1.Asset] = mask | sideMask;
                        }
                        else
                        {
                            kvs.Add(clip1.Asset, sideMask);
                        }
                    }
                }
            }
            return true;
        }
        public void UpdateItemData()
        {
            foreach (var item1 in Items)
            {
                item1.Asset.InFrame = 0;
                item1.Asset.OutFrame = 0;
                AnimationClipAsset animA = null;
                if (item1.Asset is AnimationClipAsset _animA)
                {
                    animA = _animA;
                    int preFrame = 0;
                    int postFrame = int.MaxValue;
                    foreach (var item2 in Items)
                    {
                        if (item1 == item2) continue;
                        if (item1.Asset.StartFrame > item2.Asset.StartFrame && preFrame < item2.Asset.EndFrame)
                        {
                            preFrame = item2.Asset.EndFrame;
                        }
                        if (item1.Asset.StartFrame < item2.Asset.StartFrame && postFrame > item2.Asset.StartFrame)
                        {
                            postFrame = item2.Asset.StartFrame;
                        }
                    }
                    //animA.PreFrame = _animA.pre != AnimationClipAsset.PostExtrapolate.None ? preFrame : -1;
                    //animA.PostFrame = _animA.post != AnimationClipAsset.PostExtrapolate.None ? postFrame : -1;
                    animA.PreFrame = preFrame;
                    animA.PostFrame = postFrame;
                }
                foreach (var item2 in Items)
                {
                    if (item1 == item2) continue;
                    var t = Intersect(item1.Asset, item2.Asset);
                    switch (t)
                    {
                        case 1:
                            item1.Asset.OutFrame = item2.Asset.StartFrame;
                            break;
                        case -1:
                            item1.Asset.InFrame = item2.Asset.EndFrame;
                            break;
                    }
                    if (t != -1000 && animA != null && item2.Asset is AnimationClipAsset animB)
                    {
                        Extrapolate(animA, animB);
                    }
                }

                item1.RefreshWidth();
            }
            ClipMarkDirtyRepaint();
        }
        int Intersect(TimelineClipAsset a, TimelineClipAsset b)
        {
            if (a.StartFrame <= b.StartFrame && a.EndFrame >= b.EndFrame) return -1000;
            if (b.StartFrame <= a.StartFrame && b.EndFrame >= a.EndFrame) return -1000;

            if (a.EndFrame > b.StartFrame && a.StartFrame < b.EndFrame)
            {
                if (a.StartFrame < b.StartFrame)
                {
                    return 1;
                }
                return -1;
            }
            return 0;
        }

        void Extrapolate(AnimationClipAsset a, AnimationClipAsset b)
        {
            if (a.StartFrame == 0 || a.InFrame > 0 ||
                (a.StartFrame > b.StartFrame && b.post != AnimationClipAsset.PostExtrapolate.None) ||
                a.PreFrame == a.StartFrame)
            {
                a.PreFrame = -1;
            }

            if (a.OutFrame > 0)
            {
                a.PostFrame = -1;
            }
        }
    }
}
