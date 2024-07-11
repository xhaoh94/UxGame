using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;
namespace Ux.Editor.Timeline
{
    public class TimelineTrackItem : VisualElement, IToolbarMenuElement
    {
        public TimelineTrackAsset asset;
        TimelineWindow window;
        VisualElement content;
        Label lbType;
        TextField inputName;
        public DropdownMenu menu { get; }
        Dictionary<TimelineClipAsset, TimelineClipItem> clipItemDic = new Dictionary<TimelineClipAsset, TimelineClipItem>();
        public TimelineTrackItem(TimelineTrackAsset asset, TimelineWindow window)
        {
            this.asset = asset;
            this.window = window;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/TimelineTrackItem.uxml");
            visualTree.CloneTree(this);
            content = this.Q<VisualElement>("content");
            lbType = this.Q<Label>("lbType");
            inputName = this.Q<TextField>("inputName");
            inputName.SetValueWithoutNotify(asset.Name);

            var attr = asset.GetType().GetAttribute<TLTrackAttribute>();
            lbType.text = attr.Lb;
            content.style.borderTopColor = attr.Color;
            content.style.borderLeftColor = attr.Color;
            content.style.borderBottomColor = attr.Color;
            RefreshView();

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            menu = new DropdownMenu();

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
            foreach (var clipAsset in asset.clips)
            {
                var item = new TimelineClipItem();
                item.Init(clipAsset, this, window);
                clipItemDic.Add(clipAsset, item);
                window.clipView.AddItem(item);
            }
        }
        public T CreateClipAsset<T>() where T : TimelineClipAsset
        {
            var attr = asset.GetType().GetAttribute<TLTrackClipTypeAttribute>();
            var clipType = attr.ClipType;
            var clip = Activator.CreateInstance(clipType) as TimelineClipAsset;
            clip.StartFrame = 0;
            clip.EndFrame = 60;
            return clip as T;
        }
        public void AddClipItem(TimelineClipAsset clipAsset)
        {
            if (clipItemDic.ContainsKey(clipAsset))
            {
                return;
            }
            asset.clips.Add(clipAsset);
            window.SaveAssets();

            var item = new TimelineClipItem();
            item.Init(clipAsset, this, window);
            window.clipView.AddItem(item);
            clipItemDic.Add(clipAsset, item);
            window.RefreshEntity();
        }
        public bool IsValid()
        {
            var clips = clipItemDic.Keys.ToArray();
            Dictionary<TimelineClipAsset, int> kvs = new Dictionary<TimelineClipAsset, int>();
            foreach (var clip1 in clips)
            {
                foreach (var clip2 in clips)
                {
                    if (clip1 == clip2) continue;
                    var t = Intersect(clip1, clip2);
                    if (t == -1000) return false;
                    if (t != 0)
                    {
                        if (kvs.TryGetValue(clip1, out var temt) && temt == t)
                        {
                            return false;
                        }
                        kvs.Add(clip1, t);
                    }
                }
            }
            return true;
        }
        public void UpdateClipData()
        {
            var clips = clipItemDic.Keys.ToArray();
            foreach (var clip1 in clips)
            {
                clip1.InFrame = 0;
                clip1.OutFrame = 0;
                foreach (var clip2 in clips)
                {
                    if (clip1 == clip2) continue;
                    var t = Intersect(clip1, clip2);
                    switch (t)
                    {
                        case 1:
                            clip1.OutFrame = clip2.StartFrame;
                            break;
                        case -1:                            
                            clip1.InFrame = clip2.EndFrame;
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
}
