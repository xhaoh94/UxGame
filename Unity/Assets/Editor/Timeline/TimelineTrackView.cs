using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.Timeline
{
    public partial class TimelineTrackView : VisualElement
    {
        protected VisualElement root;
        public ToolbarMenu btnAddTrack;
        public VisualElement trackContent;

        const float HeaderHeight = 32f;
        readonly Dictionary<ITimelineEditorTrack, TimelineTrackItem> trackItemDic = new();
        ScrollView _trackScroll;
        Toolbar _trackToolbar;
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

            _trackToolbar = new Toolbar();
            _trackToolbar.style.height = HeaderHeight;
            _trackToolbar.style.minHeight = HeaderHeight;
            _trackToolbar.style.flexShrink = 0;
            btnAddTrack = new ToolbarMenu { text = "＋ 添加轨道" };
            btnAddTrack.style.flexGrow = 1;
            _trackToolbar.Add(btnAddTrack);

            root.Add(_trackToolbar);

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
            if (_trackToolbar == null)
            {
                return;
            }

            var document = TimelineWindow.Document;
            var trackTypes = document?.GetTrackTypes();
            var nextButton = new ToolbarMenu { text = "＋ 添加轨道" };
            nextButton.style.flexGrow = 1;

            if (trackTypes != null && document != null)
            {
                for (var i = 0; i < trackTypes.Count; i++)
                {
                    var trackType = trackTypes[i];
                    var displayName = document.GetTrackDisplayName(trackType);
                    var category = document.GetTrackRole(trackType) == TimelineEditorSourceRole.Logic
                        ? "逻辑"
                        : "表现";
                    nextButton.menu.AppendAction(
                        $"添加/{category}/{displayName}",
                        _ => AddTrack(trackType),
                        _ => document.CanEdit
                            ? DropdownMenuAction.Status.Normal
                            : DropdownMenuAction.Status.Disabled);
                }
            }

            _trackToolbar.Remove(btnAddTrack);
            btnAddTrack = nextButton;
            _trackToolbar.Insert(0, btnAddTrack);
        }

        void AddTrack(Type trackType)
        {
            if (TimelineWindow.Document?.CanEdit != true)
            {
                return;
            }

            var track = TimelineWindow.Document.AddTrack(trackType);
            if (track == null)
            {
                return;
            }

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
            BuildTrackMenu();
            foreach (var item in trackItemDic.Values)
            {
                item.Release();
            }
            TimelineWindow.ClipContent?.Clear();
            trackContent.Clear();
            trackItemDic.Clear();

            var document = TimelineWindow.Document;
            if (document != null)
            {
                foreach (var track in document.Tracks)
                {
                    var item = new TimelineTrackItem(track);
                    trackContent.Add(item);
                    trackItemDic.Add(track, item);
                }
            }

            if (trackItemDic.Count == 0)
            {
                _emptyLabel.text = document?.HasSource != true
                    ? "请选择 Timeline 资源"
                    : "暂无轨道，点击上方“添加轨道”";
                trackContent.Add(_emptyLabel);
            }

            TimelineWindow.wnd?.clipView?.RefreshLayout();
            TimelineWindow.RefreshClip?.Invoke();
        }
    }
}
