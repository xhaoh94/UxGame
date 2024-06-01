using System;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;

public class TimelineTrackItem : VisualElement, IToolbarMenuElement
{
    TimelineTrackAsset asset;
    TimeLineWindow window;
    VisualElement content;
    Label lbType;
    TextField inputName;
    public DropdownMenu menu { get; }
    public TimelineTrackItem(TimelineTrackAsset asset,TimeLineWindow window)
    {
        this.asset = asset;
        this.window = window;
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/TimelineTrackItem.uxml");
        visualTree.CloneTree(this);
        content = this.Q<VisualElement>("content");
        lbType = this.Q<Label>("lbType");
        inputName = this.Q<TextField>("inputName");
        inputName.SetValueWithoutNotify(asset.Name);

        var attr =  asset.GetType().GetAttribute<TLTrackAttribute>();
        lbType.text = attr.Lb;
        content.style.borderTopColor = attr.Color;
        content.style.borderLeftColor = attr.Color;
        content.style.borderBottomColor = attr.Color;
        CreateClip();

        RegisterCallback<PointerDownEvent>(OnPointerDown);
        menu =new DropdownMenu();
        
    }
    void OnPointerDown(PointerDownEvent e)
    {
        if (e.button == 0)
        {

        }
        else if (e.button == 1)
        {
            menu.AppendAction("Add Clip", e => {

                var attr = asset.GetType().GetAttribute<TLTrackClipTypeAttribute>();

                var clipType = attr.ClipType;
                var clip = Activator.CreateInstance(clipType) as TimelineClipAsset;
                clip.StartFrame = 0;
                clip.EndFrame = 50;
                //clip.Name = tName;
                asset.clips.Add(clip);
                window.Component.SetTimeline(window.Component.CurTimeline.Asset);
                EditorUtility.SetDirty(window.Component.CurTimeline.Asset);
                AssetDatabase.SaveAssets();

            }, e => DropdownMenuAction.Status.Normal);
            this.ShowMenu();
        }
    }
    void CreateClip()
    {
        foreach (var asset in asset.clips)
        {
            window.clipView.veClipContent.Add(new TimelineClipItem(asset, this, window));            
        }
    }

}
