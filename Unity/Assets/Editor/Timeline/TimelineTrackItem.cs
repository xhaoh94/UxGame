using System;
using System.Collections.Generic;
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
        TimelineTrackAsset asset;
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

                    var attr = asset.GetType().GetAttribute<TLTrackClipTypeAttribute>();

                    var clipType = attr.ClipType;
                    var clip = Activator.CreateInstance(clipType) as TimelineClipAsset;
                    clip.StartFrame = 0;
                    clip.EndFrame = 50;
                    //clip.Name = tName;
                    AddClipItem(clip);
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

        void AddClipItem(TimelineClipAsset clipAsset)
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

    }
}
