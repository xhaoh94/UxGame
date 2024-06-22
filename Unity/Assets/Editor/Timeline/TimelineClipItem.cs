using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;
using Ux.Editor;

public class TimelineClipItem : VisualElement
{
    public new class UxmlFactory : UxmlFactory<TimelineClipItem, UxmlTraits> {}

    public new class UxmlTraits : TextElement.UxmlTraits
    {
    }

    TimelineClipAsset asset;
    TimeLineWindow window;
    TimelineTrackItem trackItem;
    VisualElement content;
    VisualElement left;
    Label lbType;
    VisualElement right;
    public TimelineClipItem()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/TimelineClipItem.uxml");
        visualTree.CloneTree(this);
        content = this.Q<VisualElement>("content");
        left = this.Q<VisualElement>("left");
        ElementDrag.Add(left, window.clipView.veClipContent, OnLeftStar, OnLeftDrag);
        lbType = this.Q<Label>("lbType");
        right = this.Q<VisualElement>("right");
        ElementDrag.Add(right, window.clipView.veClipContent, OnRightStar, OnRightDrag);
    }
    public void Init(TimelineClipAsset asset, TimelineTrackItem track, TimeLineWindow window)
    {
        this.asset = asset;
        this.trackItem = track;
        this.window = window;
        UpdateView();
    }
    void OnLeftStar()
    {
        OnLeftDrag(Vector2.zero);
    }
    void OnLeftDrag(Vector2 e)
    {
        var pos = window.clipView.WorldToLocal(Event.current.mousePosition);
        var CurPosx = pos.x;
        var offset = window.clipView.veMarkerIcon.worldBound.width / 2;
        if (CurPosx < 0)
        {
            CurPosx = 0;
        }
        if (CurPosx > window.clipView.ScrClipViewWidth - offset)
        {
            CurPosx = window.clipView.ScrClipViewWidth - offset;
        }
        var frame = Mathf.RoundToInt((window.clipView.ScrClipViewOffsetX + CurPosx) / window.clipView.FrameWidth);
        asset.StartFrame = frame;
        UpdateView();
    }
    void OnRightStar()
    {                        
        EditorGUIUtility.AddCursorRect(
       GUILayoutUtility.GetLastRect(), MouseCursor.ResizeHorizontal);
        OnLeftDrag(Vector2.zero);
    }
    void OnRightDrag(Vector2 e)
    {
        var pos = window.clipView.WorldToLocal(Event.current.mousePosition);
        var CurPosx = pos.x;
        var offset = window.clipView.veMarkerIcon.worldBound.width / 2;
        if (CurPosx < 0)
        {
            CurPosx = 0;
        }
        if (CurPosx > window.clipView.ScrClipViewWidth - offset)
        {
            CurPosx = window.clipView.ScrClipViewWidth - offset;
        }
        var frame = Mathf.RoundToInt((window.clipView.ScrClipViewOffsetX + CurPosx) / window.clipView.FrameWidth);
        asset.EndFrame = frame;
        UpdateView();
    }
    public void UpdateView()
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
