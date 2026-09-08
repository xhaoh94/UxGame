using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using YooAsset.Editor;

namespace Ux.Editor.Timeline
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class TimelineTrackView : VisualElement
    {
#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<TimelineTrackView, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }
#endif
        const float HeaderHeight = 32f;
        readonly Dictionary<TimelineTrackAsset, TimelineTrackItem> trackItemDic = new();
        ScrollView _trackScroll;
        Label _emptyLabel;
        bool _syncingVerticalScroll;

        public event Action<float> VerticalScrollChanged;

        public TimelineTrackView()
        {
            style.flexGrow = 1;
            style.minWidth = 200;
            style.backgroundColor = new Color(0.105f, 0.105f, 0.105f);
            BuildUI();
            BuildTrackMenu();
            TimelineWindow.RefreshView = RefreshView;
        }

        void BuildUI()
        {
            root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Column;
            Add(root);

            var toolbar = new Toolbar();
            toolbar.style.height = HeaderHeight;
            toolbar.style.minHeight = HeaderHeight;
            toolbar.style.flexShrink = 0;
            btnAddTrack = new ToolbarMenu { text = "＋ 添加轨道" };
            btnAddTrack.style.flexGrow = 1;
            toolbar.Add(btnAddTrack);
            root.Add(toolbar);

            _trackScroll = new ScrollView(ScrollViewMode.Vertical);
            _trackScroll.style.flexGrow = 1;
            _trackScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _trackScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _trackScroll.verticalScroller.valueChanged += OnVerticalScrollChanged;
            root.Add(_trackScroll);

            trackContent = new VisualElement();
            trackContent.style.flexGrow = 1;
            trackContent.style.backgroundColor = new Color(0.105f, 0.105f, 0.105f);
            _trackScroll.Add(trackContent);

            _emptyLabel = new Label("选择 Timeline 后添加轨道");
            _emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _emptyLabel.style.color = new Color(0.55f, 0.55f, 0.55f);
            _emptyLabel.style.marginTop = 20;
            trackContent.Add(_emptyLabel);
        }

        void BuildTrackMenu()
        {
            var trackAssets = EditorTools.GetAssignableTypes(typeof(TimelineTrackAsset));
            foreach (var trackType in trackAssets)
            {
                var trackAttribute = trackType.GetAttribute<TLTrackAttribute>();
                if (trackType.IsAbstract || trackAttribute == null ||
                    trackType.GetAttribute<TLTrackClipTypeAttribute>() == null)
                {
                    continue;
                }

                btnAddTrack.menu.AppendAction(
                    $"添加/{trackAttribute.Lb}",
                    _ => AddTrack(trackType),
                    _ => TimelineWindow.Asset != null && !TimelineWindow.IsPlaying
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
            }
        }

        void AddTrack(Type trackType)
        {
            if (TimelineWindow.Asset == null || TimelineWindow.IsPlaying)
            {
                return;
            }

            if (Activator.CreateInstance(trackType) is not TimelineTrackAsset track)
            {
                return;
            }
            var attribute = trackType.GetAttribute<TLTrackAttribute>();
            track.trackName = attribute?.Lb ?? trackType.Name;
            track.ValidateData();

            TimelineWindow.Undo.RegUndo("timeline_add_track", TimelineWindow.Asset, RefreshView);
            TimelineWindow.Asset.tracks.Add(track);
            TimelineWindow.Asset.ValidateData();
            TimelineWindow.SaveAssets?.Invoke();
            RefreshView();
            TimelineWindow.RefreshEntity?.Invoke();
            TimelineWindow.InspectorContent?.FreshInspector(track, null);
        }

        public void SetVerticalScroll(float value)
        {
            if (_trackScroll == null || Mathf.Approximately(_trackScroll.scrollOffset.y, value))
            {
                return;
            }
            _syncingVerticalScroll = true;
            var offset = _trackScroll.scrollOffset;
            offset.y = value;
            _trackScroll.scrollOffset = offset;
            _syncingVerticalScroll = false;
        }

        void OnVerticalScrollChanged(float value)
        {
            if (!_syncingVerticalScroll)
            {
                VerticalScrollChanged?.Invoke(value);
            }
        }

        public void RefreshView()
        {
            foreach (var item in trackItemDic.Values)
            {
                item.Release();
            }
            TimelineWindow.ClipContent?.Clear();
            trackContent.Clear();
            trackItemDic.Clear();

            if (TimelineWindow.Asset?.tracks != null)
            {
                foreach (var track in TimelineWindow.Asset.tracks)
                {
                    if (track == null)
                    {
                        continue;
                    }
                    var item = new TimelineTrackItem(track);
                    trackContent.Add(item);
                    trackItemDic.Add(track, item);
                }
            }

            var hasTracks = trackItemDic.Count > 0;
            if (!hasTracks)
            {
                _emptyLabel.text = TimelineWindow.Asset == null
                    ? "请选择 Timeline 资源"
                    : "暂无轨道，点击上方“添加轨道”";
                trackContent.Add(_emptyLabel);
            }

            TimelineWindow.wnd?.clipView?.RefreshLayout();
            TimelineWindow.RefreshClip?.Invoke();
        }
    }
}
