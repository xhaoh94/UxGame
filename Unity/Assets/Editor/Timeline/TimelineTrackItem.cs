using System;
using System.Collections.Generic;
using System.Linq;
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
        public TimelineTrackItem TrackItem { get;}
        public List<TimelineClipItem> Items { get; } = new();
        public TrackClipContent(TimelineTrackItem trackItem)
        {
            TrackItem = trackItem;
            clipParent = new VisualElement();
            draw = new VisualElement();
            draw.generateVisualContent += OnDrawContent;
            style.height = 30;

            Timeline.ClipContent.Add(this);
            Timeline.UpdateMarkerPos += UpdateMarkerPos;
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

            foreach (var item in Items)
            {
                var asset = item.Asset;
                var sx = Timeline.GetPositionByFrame(asset.StartFrame);
                var ex = Timeline.GetPositionByFrame(asset.EndFrame);

                if (asset.InFrame > 0)
                {
                    var ix = Timeline.GetPositionByFrame(asset.InFrame);
                    paint2D.MoveTo(new Vector2(0, 0));
                    paint2D.LineTo(new Vector2(ix - sx, 0));
                    paint2D.LineTo(new Vector2(ix - sx, 20));
                    paint2D.LineTo(new Vector2(0, 0));
                }
                if (asset.OutFrame > 0)
                {
                    var ox = Timeline.GetPositionByFrame(asset.OutFrame);
                    paint2D.MoveTo(new Vector2(ox - sx, 0));
                    paint2D.LineTo(new Vector2(ox - sx, 20));
                    paint2D.LineTo(new Vector2(ex - sx, 20));
                    paint2D.LineTo(new Vector2(ox - sx, 0));
                }
            }

            paint2D.Stroke();
        }

        void UpdateMarkerPos()
        {
            ClipMarkDirtyRepaint();
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
                if (a.StartFrame < b.StartTime) return 1;
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
