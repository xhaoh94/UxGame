using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;

public class TimelineClipItem : VisualElement
{
    //public new class UxmlFactory : UxmlFactory<TimelineClipItem, UxmlTraits> {}

    //
    // ժҪ:
    //     Defines UxmlTraits for the ToolbarMenu.
    public new class UxmlTraits : TextElement.UxmlTraits
    {
    }

    TimelineClipAsset asset;
    TimeLineWindow window;
    VisualElement content;
    VisualElement left;
    Label lbType;
    VisualElement right;
    public TimelineClipItem(TimelineClipAsset asset, TimelineTrackItem track ,TimeLineWindow window)
    {
        this.asset = asset;
        this.window = window;
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/TimelineClipItem.uxml");
        visualTree.CloneTree(this);
        content = this.Q<VisualElement>("content");
        left = this.Q<VisualElement>("left");
        lbType = this.Q<Label>("lbType");
        right = this.Q<VisualElement>("right");
        UpdateView();
    }

    void UpdateView()
    {
        var FrameWidth = window.clipView.FrameWidth;
        var ScrClipViewOffsetX = window.clipView.ScrClipViewOffsetX;
        var sx = (asset.StartFrame * FrameWidth - ScrClipViewOffsetX);
        var ex = (asset.EndFrame * FrameWidth - ScrClipViewOffsetX);
        this.style.position = new StyleEnum<Position>(Position.Absolute);
        this.style.left = sx;
        this.style.width = ex - sx;
    }
}
