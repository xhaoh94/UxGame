using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline
{
    public class TrackClipContent : VisualElement
    {
        VisualElement clipParent;
        VisualElement draw;
        public TimelineTrackItem TrackItem { get; }
        public List<TimelineClipItem> Items { get; } = new();
        public TrackClipContent(TimelineTrackItem trackItem)
        {
            style.height = 30;
            style.left = 0;
            style.right = 0;
            TrackItem = trackItem;
            clipParent = new VisualElement();
            clipParent.style.position = new StyleEnum<Position>(Position.Absolute);
            clipParent.style.top = 0;
            clipParent.style.bottom = 0;
            clipParent.style.left = 0;
            clipParent.style.right = 0;
            Add(clipParent);
            draw = new VisualElement();
            draw.pickingMode = PickingMode.Ignore;
            draw.style.position = new StyleEnum<Position>(Position.Absolute);
            draw.style.top = 0;
            draw.style.bottom = 0;
            draw.style.left = 0;
            draw.style.right = 0;
            Add(draw);
            draw.generateVisualContent += OnDrawContent;

            Timeline.ClipContent.Add(this);
            Timeline.UpdateMarkerPos += UpdateMarkerPos;

            ElementDrag.Add(this, Timeline.ClipContent, OnStart, OnDrag, OnEnd);
        }
        int lastFrame;
        TimelineClipItem selectItem;
        List<TimelineClipItem> _temItems = new List<TimelineClipItem>();
        void OnStart()
        {
            _temItems.Clear();
            selectItem = null;
            if (Items.Count == 0) return;
            lastFrame = Timeline.GetFrameByMousePosition();
            foreach (var item in Items)
            {
                item.ToDown(lastFrame);
                if (item.Status != Status.None)
                {
                    _temItems.Add(item);
                }
            }

            _temItems.Sort((a, b) =>
            {
                return a.Status - b.Status;
            });

            selectItem = _temItems[0];
        }
        void OnDrag(Vector2 e)
        {
            var now = Timeline.GetFrameByMousePosition();
            selectItem?.ToDrag(now, lastFrame);
            lastFrame = now;
        }
        void OnEnd()
        {
            foreach (var item in Items)
            {
                item.ToUp();
            }
        }
        public void Release()
        {
            Timeline.ClipContent.Remove(this);
            Timeline.UpdateMarkerPos -= UpdateMarkerPos;
        }
        void OnDrawContent(MeshGenerationContext mgc)
        {
            var paint2D = mgc.painter2D;
            paint2D.strokeColor = Color.black;
            paint2D.BeginPath();
            int minY = 2;
            int maxY = 28;
            foreach (var item in Items)
            {
                var asset = item.Asset;
                var sx = Timeline.GetPositionByFrame(asset.StartFrame);
                var ex = Timeline.GetPositionByFrame(asset.EndFrame);

                if (asset.InFrame > 0)
                {
                    var ix = Timeline.GetPositionByFrame(asset.InFrame);
                    paint2D.MoveTo(new Vector2(ix, minY));
                    paint2D.LineTo(new Vector2(sx, minY));
                    paint2D.LineTo(new Vector2(ix, maxY));
                    paint2D.LineTo(new Vector2(ix, minY));
                }
                if (asset.OutFrame > 0)
                {
                    var ox = Timeline.GetPositionByFrame(asset.OutFrame);
                    paint2D.MoveTo(new Vector2(ox, minY));
                    paint2D.LineTo(new Vector2(ox, maxY));
                    paint2D.LineTo(new Vector2(ex, maxY));
                    paint2D.LineTo(new Vector2(ox, minY));
                }
            }

            paint2D.Stroke();
        }

        void UpdateMarkerPos()
        {
            ClipMarkDirtyRepaint();
            foreach (var item in Items)
            {
                item.UpdateView();
            }
        }
        public void ClipMarkDirtyRepaint()
        {
            draw.MarkDirtyRepaint();
        }
        public void AddItem(TimelineClipAsset asset)
        {
            var item = new TimelineClipItem();
            item.Init(asset, this);
            Items.Add(item);
            clipParent.Add(item);
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
                        if (kvs.TryGetValue(clip1.Asset, out var temt) && temt == t)
                        {
                            return false;
                        }
                        kvs.Add(clip1.Asset, t);
                    }
                }
            }
            return true;
        }
        public void UpdateClipData()
        {
            foreach (var clip1 in Items)
            {
                clip1.Asset.InFrame = 0;
                clip1.Asset.OutFrame = 0;
                foreach (var clip2 in Items)
                {
                    if (clip1 == clip2) continue;
                    var t = Intersect(clip1.Asset, clip2.Asset);
                    switch (t)
                    {
                        case 1:
                            clip1.Asset.OutFrame = clip2.Asset.StartFrame;
                            break;
                        case -1:
                            clip1.Asset.InFrame = clip2.Asset.EndFrame;
                            break;
                    }
                }
            }
        }
        int Intersect(TimelineClipAsset a, TimelineClipAsset b)
        {
            if (a.StartFrame < b.StartFrame && a.EndFrame > b.EndFrame) return -1000;
            if (b.StartFrame < a.StartFrame && b.EndFrame > a.EndFrame) return -1000;

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
    }
    public partial class TimelineTrackItem : VisualElement, IToolbarMenuElement
    {
        public TimelineTrackAsset Asset { get; private set; }
        TrackClipContent clipContent;
        public DropdownMenu menu { get; }
        public TimelineTrackItem(TimelineTrackAsset asset)
        {
            CreateChildren();
            Add(root);
            style.height = 30;

            Asset = asset;
            clipContent = new TrackClipContent(this);
            inputName.SetValueWithoutNotify(asset.Name);

            var attr = asset.GetType().GetAttribute<TLTrackAttribute>();
            lbType.text = attr.Lb;
            content.style.borderTopColor = attr.Color;
            content.style.borderLeftColor = attr.Color;
            content.style.borderBottomColor = attr.Color;
            RefreshView();

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            menu = new DropdownMenu();

            clipContent.RegisterCallback<DragUpdatedEvent>(OnDragUpd);
            clipContent.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }
        public void Release()
        {
            clipContent.Release();
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
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(retPath);
                if (clip == null)
                {
                    return;
                }
                var clipAsset = CreateClipAsset<AnimationClipAsset>();
                clipAsset.StartFrame = Mathf.CeilToInt(Asset.MaxTime / TimelineMgr.Ins.FrameRate);
                clipAsset.EndFrame = clipAsset.StartFrame + Mathf.RoundToInt(clip.length * TimelineMgr.Ins.FrameRate);
                clipAsset.clip = clip;
                clipAsset.Name = clip.name;
                AddClipItem(clipAsset);
            }
        }
        void OnPointerDown(PointerDownEvent e)
        {
            if (e.button == 0)
            {

            }
            else if (e.button == 1)
            {
                menu.AppendAction("Add Clip", e =>
                {
                    AddClipItem(CreateClipAsset<TimelineClipAsset>());
                }, e => DropdownMenuAction.Status.Normal);
                this.ShowMenu();
            }
        }
        void RefreshView()
        {
            foreach (var clipAsset in Asset.clips)
            {
                clipContent.AddItem(clipAsset);
            }
        }
        public T CreateClipAsset<T>() where T : TimelineClipAsset
        {
            var attr = Asset.GetType().GetAttribute<TLTrackClipTypeAttribute>();
            var clipType = attr.ClipType;
            var clip = Activator.CreateInstance(clipType) as TimelineClipAsset;
            clip.StartFrame = 0;
            clip.EndFrame = 60;
            return clip as T;
        }
        public void AddClipItem(TimelineClipAsset clipAsset)
        {
            if (Asset.clips.Contains(clipAsset))
            {
                return;
            }
            Asset.clips.Add(clipAsset);
            Timeline.SaveAssets();
            clipContent.AddItem(clipAsset);
            Timeline.RefreshEntity();
        }
        public bool IsValid()
        {
            return clipContent.IsValid();
        }
        public void UpdateClipData()
        {
            clipContent.UpdateClipData();
        }
    }
}
