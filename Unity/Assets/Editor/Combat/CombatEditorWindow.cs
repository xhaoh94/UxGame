using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    /// <summary>
    /// 角色战斗内容入口。
    ///
    /// 这里负责角色 Profile 参数、宏观状态到 Timeline 的映射和技能资源列表。
    /// 技能的逻辑轨道与表现 Clip 默认以内嵌双源时间轴编辑，仍保留独立 TimelineWindow 入口。
    /// </summary>
    public sealed class CombatEditorWindow : EditorWindow
    {
        private enum Page
        {
            Profile,
            States,
            Skills,
        }

        private const int DefaultActionId = 1001;
        private const int DefaultActionDuration = 30;

        // 左栏宽度可拖动调整：默认 205，最小 150，并为右侧内容区保留 300。
        private const float MinLeftPanelWidth = 150f;
        private const float DefaultLeftPanelWidth = 205f;
        private const float LeftPanelReserve = 300f;
        private const string LeftPanelWidthKey = "Ux.CombatEditor.LeftPanelWidth";
        private const string LastProfileKey = "Ux.CombatEditor.LastProfile";
        private const string LastPreviewObjectKey = "Ux.CombatEditor.LastPreviewObject";
        private const string PreviewObjectKeyPrefix = "Ux.CombatEditor.PreviewObject.";
        private const string RightPanelRatioKey = "Ux.CombatEditor.RightPanelRatio";

        // 右栏分区折叠状态：按分区独立记忆，避免每次打开都要重新展开。
        private const string LogicSectionKey = "Ux.CombatEditor.Fold.Logic";
        private const string PresentationSectionKey = "Ux.CombatEditor.Fold.Presentation";
        private const string ValidationSectionKey = "Ux.CombatEditor.Fold.Validation";
        private static GUIStyle _sectionFoldoutStyle;
        private static GUIStyle _skillItemStyle;
        private static GUIStyle _skillDeleteStyle;

        private static GUIStyle SectionFoldoutStyle
        {
            get
            {
                if (_sectionFoldoutStyle == null)
                {
                    _sectionFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold,
                    };
                }
                return _sectionFoldoutStyle;
            }
        }

        private static GUIStyle SkillItemStyle
        {
            get
            {
                if (_skillItemStyle == null)
                {
                    _skillItemStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(12, 6, 0, 0),
                        fontSize = 12,
                    };
                    _skillItemStyle.normal.textColor = new Color(0.88f, 0.88f, 0.88f);
                    _skillItemStyle.hover.textColor = Color.white;
                    _skillItemStyle.active.textColor = Color.white;
                }
                return _skillItemStyle;
            }
        }

        private static GUIStyle SkillDeleteStyle
        {
            get
            {
                if (_skillDeleteStyle == null)
                {
                    _skillDeleteStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        padding = new RectOffset(0, 0, 0, 0),
                    };
                }
                return _skillDeleteStyle;
            }
        }

        private static readonly StateLayer[] PresentationLayers =
        {
            StateLayer.Locomotion,
            StateLayer.Control,
            StateLayer.Life,
        };

        private CharacterCombatProfile _profile;
        private CombatActionAsset _selectedAction;
        private GameObject _previewObject;
        private Page _page;
        private Vector2 _leftScroll;
        private Vector2 _contentScroll;
        private SerializedObject _profileSerialized;
        private SerializedObject _actionSerialized;
        private List<CombatValidationIssue> _issues = new List<CombatValidationIssue>();
        private bool _showIssues;
        private float _leftPanelWidth = DefaultLeftPanelWidth;
        private IMGUIContainer _configContainer;
        private IMGUIContainer _navigationContainer;
        private VisualElement _rightPanel;
        private ResizeSplitter _leftSplitter;
        private ResizeSplitter _rightSplitter;
        private VisualElement _embeddedTimelineContainer;
        private TimelineWindow _embeddedTimeline;
        private CombatActionAsset _embeddedAction;
        private TimelineAsset _embeddedTimelineAsset;
        private GameObject _embeddedPreviewObject;
        private bool _embeddedRefreshPending;
        private bool _timelineDetachedToStandalone;
        private bool _rightPanelManuallySized;
        private float _rightPanelRatio = 0.42f;
        private float _rightPanelContentHeight;

        private sealed class ResizeSplitter : VisualElement
        {
            private readonly bool _vertical;
            private bool _dragging;
            private int _pointerId;
            private Vector2 _startPosition;

            public event Action<Vector2> Dragged;

            public ResizeSplitter(bool vertical)
            {
                _vertical = vertical;
                style.flexShrink = 0;
                if (vertical)
                {
                    style.width = 3;
                }
                else
                {
                    style.height = 3;
                }
                style.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f);
                tooltip = vertical ? "拖动调整左侧宽度" : "拖动调整上方信息高度";
                RegisterCallback<PointerDownEvent>(OnPointerDown);
                RegisterCallback<PointerMoveEvent>(OnPointerMove);
                RegisterCallback<PointerUpEvent>(OnPointerUp);
                RegisterCallback<PointerCaptureOutEvent>(_ => StopDragging());
                RegisterCallback<MouseEnterEvent>(_ => SetHighlight(true));
                RegisterCallback<MouseLeaveEvent>(_ => SetHighlight(_dragging));
            }

            private void OnPointerDown(PointerDownEvent evt)
            {
                if (evt.button != 0 || _dragging)
                {
                    return;
                }

                _dragging = true;
                _pointerId = evt.pointerId;
                _startPosition = (Vector2)evt.position;
                this.CapturePointer(_pointerId);
                SetHighlight(true);
                evt.StopPropagation();
            }

            private void OnPointerMove(PointerMoveEvent evt)
            {
                if (!_dragging || evt.pointerId != _pointerId)
                {
                    return;
                }

                var position = (Vector2)evt.position;
                var delta = position - _startPosition;
                _startPosition = position;
                Dragged?.Invoke(_vertical
                    ? new Vector2(delta.x, 0)
                    : new Vector2(0, delta.y));
                evt.StopPropagation();
            }

            private void OnPointerUp(PointerUpEvent evt)
            {
                if (evt.pointerId != _pointerId)
                {
                    return;
                }

                StopDragging();
                evt.StopPropagation();
            }

            private void StopDragging()
            {
                if (!_dragging)
                {
                    return;
                }

                _dragging = false;
                if (this.HasPointerCapture(_pointerId))
                {
                    this.ReleasePointer(_pointerId);
                }
                SetHighlight(false);
            }

            private void SetHighlight(bool highlighted)
            {
                style.backgroundColor = highlighted
                    ? new Color(0.32f, 0.52f, 0.82f, 0.85f)
                    : new Color(0.16f, 0.16f, 0.16f, 1f);
            }
        }

        [MenuItem("UxGame/工具/战斗/角色配置", false, 520)]
        public static void ShowWindow()
        {
            var window = GetWindow<CombatEditorWindow>();
            window.titleContent = new GUIContent("角色战斗配置");
            window.minSize = new Vector2(960, 680);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.flexGrow = 1;

            var mainArea = new VisualElement();
            mainArea.style.flexDirection = FlexDirection.Row;
            mainArea.style.flexGrow = 1;

            _navigationContainer = new IMGUIContainer(DrawNavigationGUI)
            {
                name = "CombatEditorNavigation",
            };
            _navigationContainer.style.minWidth = MinLeftPanelWidth;
            _navigationContainer.style.width = _leftPanelWidth;
            _navigationContainer.style.flexShrink = 0;
            mainArea.Add(_navigationContainer);

            _leftSplitter = new ResizeSplitter(true);
            _leftSplitter.Dragged += OnLeftSplitterDragged;
            mainArea.Add(_leftSplitter);

            _rightPanel = new VisualElement();
            _rightPanel.style.flexDirection = FlexDirection.Column;
            _rightPanel.style.flexGrow = 1;
            _rightPanel.RegisterCallback<GeometryChangedEvent>(_ => ApplyRightPanelLayout());

            _configContainer = new IMGUIContainer(DrawRightEditorGUI)
            {
                name = "CombatEditorConfig",
            };
            _configContainer.style.flexGrow = 1;
            _configContainer.style.minHeight = 0;
            _rightPanel.Add(_configContainer);

            _rightSplitter = new ResizeSplitter(false);
            _rightSplitter.Dragged += OnRightSplitterDragged;
            _rightSplitter.style.display = DisplayStyle.None;
            _rightPanel.Add(_rightSplitter);

            _embeddedTimelineContainer = new VisualElement
            {
                name = "EmbeddedCombatTimeline",
            };
            _embeddedTimelineContainer.style.flexGrow = 1;
            _embeddedTimelineContainer.style.minHeight = 260;
            _embeddedTimelineContainer.style.display = DisplayStyle.None;
            _rightPanel.Add(_embeddedTimelineContainer);
            mainArea.Add(_rightPanel);
            rootVisualElement.Add(mainArea);
            ScheduleEmbeddedTimelineRefresh();
        }

        private void OnLeftSplitterDragged(Vector2 delta)
        {
            _leftPanelWidth = Mathf.Clamp(
                _leftPanelWidth + delta.x,
                MinLeftPanelWidth,
                GetMaxLeftPanelWidth());
            if (_navigationContainer != null)
            {
                _navigationContainer.style.width = _leftPanelWidth;
            }
            EditorPrefs.SetFloat(LeftPanelWidthKey, _leftPanelWidth);
        }

        private void OnRightSplitterDragged(Vector2 delta)
        {
            if (_rightPanel == null || _configContainer == null)
            {
                return;
            }

            var totalHeight = _rightPanel.resolvedStyle.height;
            if (totalHeight <= 0)
            {
                return;
            }

            var currentHeight = _configContainer.resolvedStyle.height;
            var minimumHeight = Mathf.Max(150, _rightPanelContentHeight);
            var maximumHeight = Mathf.Max(minimumHeight, totalHeight - 180);
            var nextHeight = Mathf.Clamp(
                currentHeight + delta.y,
                Mathf.Min(minimumHeight, maximumHeight),
                maximumHeight);
            _rightPanelManuallySized = true;
            _rightPanelRatio = Mathf.Clamp(nextHeight / totalHeight, 0.15f, 0.85f);
            _configContainer.style.flexGrow = 0;
            _configContainer.style.height = nextHeight;
            EditorPrefs.SetFloat(RightPanelRatioKey, _rightPanelRatio);
        }

        private void ApplyRightPanelLayout()
        {
            if (_rightPanel == null || _configContainer == null || _rightSplitter == null)
            {
                return;
            }

            var timelineVisible = _embeddedTimelineContainer != null &&
                _embeddedTimelineContainer.style.display != DisplayStyle.None;
            _rightSplitter.style.display = timelineVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (!timelineVisible)
            {
                _configContainer.style.flexGrow = 1;
                _configContainer.style.height = StyleKeyword.Auto;
                return;
            }

            _configContainer.style.flexGrow = 0;
            if (!_rightPanelManuallySized)
            {
                // 没有手动拖动过时使用 IMGUI 自身的内容高度，折叠分区后 Timeline 会自动上移。
                _configContainer.style.height = StyleKeyword.Auto;
                var measuredHeight = _configContainer.resolvedStyle.height;
                if (measuredHeight > 0 && !float.IsNaN(measuredHeight))
                {
                    _rightPanelContentHeight = measuredHeight;
                }
                return;
            }

            var totalHeight = _rightPanel.resolvedStyle.height;
            var minimumHeight = Mathf.Max(150, _rightPanelContentHeight);
            var maximumHeight = Mathf.Max(minimumHeight, totalHeight - 180);
            var nextHeight = Mathf.Clamp(
                totalHeight * _rightPanelRatio,
                Mathf.Min(minimumHeight, maximumHeight),
                maximumHeight);
            _configContainer.style.height = nextHeight;
        }

        public static CombatEditorWindow Open(CharacterCombatProfile profile, GameObject previewObject = null)
        {
            var window = GetWindow<CombatEditorWindow>();
            window.titleContent = new GUIContent("角色战斗配置");
            window.minSize = new Vector2(960, 680);
            window.SetProfile(profile);
            if (previewObject != null)
            {
                window._previewObject = previewObject;
                SavePreviewObject(profile, previewObject);
            }
            window.Show();
            window.Repaint();
            return window;
        }

        public static CombatEditorWindow OpenAction(CharacterCombatProfile profile, CombatActionAsset action, GameObject previewObject = null)
        {
            var window = Open(profile, previewObject);
            if (action != null && window.IsActionInProfile(action))
            {
                window._selectedAction = action;
                window._page = Page.Skills;
                window._actionSerialized = null;
            }
            return window;
        }

        private void OnEnable()
        {
            TimelineWindow.StandaloneWindowClosed -= OnStandaloneTimelineClosed;
            TimelineWindow.StandaloneWindowOpened -= OnStandaloneTimelineOpened;
            TimelineWindow.StandaloneWindowClosed += OnStandaloneTimelineClosed;
            TimelineWindow.StandaloneWindowOpened += OnStandaloneTimelineOpened;
            Undo.undoRedoEvent -= OnUndoRedo;
            Undo.undoRedoEvent += OnUndoRedo;
            titleContent = new GUIContent("角色战斗配置");
            minSize = new Vector2(960, 680);
            _leftPanelWidth = EditorPrefs.GetFloat(LeftPanelWidthKey, DefaultLeftPanelWidth);
            _rightPanelRatio = Mathf.Clamp(
                EditorPrefs.GetFloat(RightPanelRatioKey, _rightPanelRatio),
                0.15f,
                0.85f);
            var selected = Selection.activeObject as CharacterCombatProfile;
            var restored = selected ?? LoadAssetByGuid<CharacterCombatProfile>(LastProfileKey);
            if (restored != null)
            {
                SetProfile(restored);
            }
            else
            {
                _previewObject = null;
            }
        }

        private void OnSelectionChange()
        {
            var selected = Selection.activeObject as CharacterCombatProfile;
            if (selected != null && selected != _profile)
            {
                SetProfile(selected);
            }
            Repaint();
        }

        private void OnUndoRedo(in UndoRedoInfo undoRedoInfo)
        {
            if (this == null)
            {
                return;
            }

            if (_profileSerialized != null && _profileSerialized.targetObject != null)
            {
                _profileSerialized.Update();
            }
            if (_actionSerialized != null && _actionSerialized.targetObject != null)
            {
                _actionSerialized.Update();
            }
            if (_profile != null && !IsActionInProfile(_selectedAction))
            {
                ClosePreviewTimeline();
                _selectedAction = FindFirstAction();
                _actionSerialized = null;
            }
            _issues.Clear();
            TimelineWindow.RefreshCurrentDocumentAfterUndo();
            _navigationContainer?.MarkDirtyRepaint();
            _configContainer?.MarkDirtyRepaint();
            _embeddedTimeline?.Repaint();
            ScheduleEmbeddedTimelineRefresh();
            Repaint();
        }

        private void DrawNavigationGUI()
        {
            _leftPanelWidth = Mathf.Clamp(
                _leftPanelWidth,
                MinLeftPanelWidth,
                GetMaxLeftPanelWidth());
            if (_navigationContainer != null)
            {
                _navigationContainer.style.width = _leftPanelWidth;
            }
            DrawNavigation();
        }

        private void DrawRightEditorGUI()
        {
            DrawHeader();
            if (_profile == null)
            {
                DrawEmptyState();
                ScheduleEmbeddedTimelineRefresh();
                return;
            }

            if (!IsActionInProfile(_selectedAction))
            {
                _selectedAction = FindFirstAction();
            }

            if (_page == Page.Skills)
            {
                // 技能页需要把完整信息高度交给外层布局，避免 ScrollView 把 Timeline 顶出大段空白。
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    DrawSkillsPage();
                }
            }
            else
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(_contentScroll))
                {
                    _contentScroll = scroll.scrollPosition;
                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    {
                        switch (_page)
                        {
                            case Page.Profile:
                                DrawProfilePage();
                                break;
                            case Page.States:
                                DrawStatesPage();
                                break;
                        }
                    }
                }
            }
            ScheduleEmbeddedTimelineRefresh();
        }

        private void ScheduleEmbeddedTimelineRefresh()
        {
            if (_embeddedRefreshPending)
            {
                return;
            }

            _embeddedRefreshPending = true;
            EditorApplication.delayCall += RefreshEmbeddedTimeline;
        }

        private void RefreshEmbeddedTimeline()
        {
            _embeddedRefreshPending = false;
            if (this == null || _embeddedTimelineContainer == null)
            {
                return;
            }

            if (_timelineDetachedToStandalone ||
                _page != Page.Skills || !IsActionInProfile(_selectedAction))
            {
                DestroyEmbeddedTimeline();
                return;
            }

            var timeline = GetActionTimeline(_selectedAction);
            if (timeline == null)
            {
                DestroyEmbeddedTimeline();
                return;
            }

            if (_embeddedTimeline != null &&
                _embeddedAction == _selectedAction &&
                _embeddedTimelineAsset == timeline &&
                _embeddedPreviewObject == _previewObject)
            {
                return;
            }

            DestroyEmbeddedTimeline();
            _embeddedAction = _selectedAction;
            _embeddedTimelineAsset = timeline;
            _embeddedPreviewObject = _previewObject;
            _embeddedTimelineContainer.style.display = DisplayStyle.Flex;
            ApplyRightPanelLayout();
            _embeddedTimeline = TimelineWindow.CreateEmbedded(
                _embeddedTimelineContainer,
                _embeddedAction,
                _embeddedTimelineAsset,
                _profile,
                _previewObject);
            if (_embeddedTimeline == null)
            {
                DestroyEmbeddedTimeline();
            }
            Repaint();
        }

        private void DestroyEmbeddedTimeline()
        {
            EditorApplication.delayCall -= RefreshEmbeddedTimeline;
            _embeddedRefreshPending = false;
            if (_embeddedTimeline != null)
            {
                UnityEngine.Object.DestroyImmediate(_embeddedTimeline);
                _embeddedTimeline = null;
            }

            _embeddedAction = null;
            _embeddedTimelineAsset = null;
            _embeddedPreviewObject = null;
            _embeddedTimelineContainer?.Clear();
            if (_embeddedTimelineContainer != null)
            {
                _embeddedTimelineContainer.style.display = DisplayStyle.None;
            }
            ApplyRightPanelLayout();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("角色", EditorStyles.miniLabel, GUILayout.Width(36));
                    EditorGUI.BeginChangeCheck();
                    var profile = (CharacterCombatProfile)EditorGUILayout.ObjectField(
                        _profile,
                        typeof(CharacterCombatProfile),
                        false,
                        GUILayout.MinWidth(220));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetProfile(profile);
                    }

                    if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(48)))
                    {
                        CreateProfile();
                    }
                    using (new EditorGUI.DisabledScope(_profile == null))
                    {
                        if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(48)))
                        {
                            SaveAll();
                        }
                        if (GUILayout.Button("校验", EditorStyles.toolbarButton, GUILayout.Width(48)))
                        {
                            ValidateAndReport();
                        }
                    }

                    GUILayout.FlexibleSpace();
                    if (_profile != null)
                    {
                        var states = _profile.StatePresentations == null ? 0 : _profile.StatePresentations.Count;
                        var actions = _profile.Actions == null ? 0 : _profile.Actions.Count;
                        GUILayout.Label($"状态 {states}  技能 {actions}", EditorStyles.miniLabel);
                    }
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("预览对象", EditorStyles.miniLabel, GUILayout.Width(60));
                    EditorGUI.BeginChangeCheck();
                    var previewObject = (GameObject)EditorGUILayout.ObjectField(
                        _previewObject,
                        typeof(GameObject),
                        false,
                        GUILayout.MinWidth(220));
                    if (EditorGUI.EndChangeCheck())
                    {
                        _previewObject = previewObject;
                        SavePreviewObject(_profile, _previewObject);
                        ScheduleEmbeddedTimelineRefresh();
                    }
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(_profile == null))
                    {
                        if (GUILayout.Button("同步全部帧率", EditorStyles.toolbarButton, GUILayout.Width(96)))
                        {
                            SyncAllTimelineFrameRates();
                        }
                    }
                }
            }
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(430)))
                {
                    GUILayout.Label("角色战斗配置", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "选择 CharacterCombatProfile 开始编辑。\n\n" +
                        "状态表现只做状态到 Timeline 的映射；技能列表管理主动动作；" +
                        "技能的逻辑轨道和表现 Timeline 默认在当前窗口内编辑，也可以按需打开独立窗口。",
                        MessageType.Info);
                    if (GUILayout.Button("新建角色战斗配置", GUILayout.Height(30)))
                    {
                        CreateProfile();
                    }
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        private void DrawNavigation()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(_leftPanelWidth)))
            {
                GUILayout.Label("配置", EditorStyles.boldLabel);
                DrawPageButton("基础参数", Page.Profile);
                DrawPageButton("状态表现映射", Page.States);

                EditorGUILayout.Space(10);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("技能列表", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(_profile == null))
                    {
                        if (GUILayout.Button("+", GUILayout.Width(24)))
                        {
                            AddAction(false);
                        }
                    }
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(_leftScroll))
                {
                    _leftScroll = scroll.scrollPosition;
                    if (_profile?.Actions == null || _profile.Actions.Count == 0)
                    {
                        EditorGUILayout.HelpBox("暂无技能", MessageType.Info);
                    }
                    else
                    {
                        var rowWidth = MeasureLeftRowWidth();
                        for (var i = 0; i < _profile.Actions.Count; i++)
                        {
                            DrawActionButton(_profile.Actions[i], i, rowWidth);
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox(
                    "代码触发技能，Profile 提供数据，双源时间轴在当前窗口内编辑表现。",
                    MessageType.None);
            }

        }

        private float GetMaxLeftPanelWidth()
        {
            return Mathf.Max(MinLeftPanelWidth, position.width - LeftPanelReserve);
        }

        /// <summary>
        /// 左栏技能行的可用宽度：直接问布局要一个整行矩形（零高度、可拉伸）。
        /// 这样拿到的宽度就是滚动视图内容区的真实宽度（已扣除纵向滚动条），
        /// 行宽不会超出视口（不出现横向滚动条），也能铺满（右侧不留空白）。
        /// </summary>
        private float MeasureLeftRowWidth()
        {
            var probe = GUILayoutUtility
                .GetRect(0f, 0f, GUILayout.ExpandWidth(true))
                .width - 2f;

            if (probe <= 60f)
            {
                // 兜底：布局没给出可用宽度时按面板宽度估算。
                probe = _leftPanelWidth
                    - EditorStyles.helpBox.padding.horizontal
                    - GUI.skin.verticalScrollbar.fixedWidth
                    - 2f;
            }

            return Mathf.Clamp(probe, 60f, Mathf.Max(60f, _leftPanelWidth));
        }

        private void OnDisable()
        {
            Undo.undoRedoEvent -= OnUndoRedo;
            TimelineWindow.StandaloneWindowClosed -= OnStandaloneTimelineClosed;
            TimelineWindow.StandaloneWindowOpened -= OnStandaloneTimelineOpened;
            EditorPrefs.SetFloat(LeftPanelWidthKey, _leftPanelWidth);
            SaveEditorState();
            ClosePreviewTimeline();
        }

        private void OnStandaloneTimelineOpened()
        {
            if (this == null)
            {
                return;
            }

            _timelineDetachedToStandalone = true;
            DestroyEmbeddedTimeline();
            Repaint();
        }

        private void OnStandaloneTimelineClosed()
        {
            if (this == null)
            {
                return;
            }

            var shouldRestore = _timelineDetachedToStandalone;
            _timelineDetachedToStandalone = false;
            if (shouldRestore && _page == Page.Skills)
            {
                ScheduleEmbeddedTimelineRefresh();
            }
            Repaint();
        }

        private void ClosePreviewTimeline()
        {
            var closeStandalone = _timelineDetachedToStandalone;
            _timelineDetachedToStandalone = false;
            if (closeStandalone)
            {
                TimelineWindow.CloseStandalone();
            }
            DestroyEmbeddedTimeline();
        }

        private void OnDestroy()
        {
            Undo.undoRedoEvent -= OnUndoRedo;
            TimelineWindow.StandaloneWindowClosed -= OnStandaloneTimelineClosed;
            TimelineWindow.StandaloneWindowOpened -= OnStandaloneTimelineOpened;
            ClosePreviewTimeline();
        }

        private void DrawPageButton(string text, Page page)
        {
            var oldColor = GUI.backgroundColor;
            if (_page == page)
            {
                GUI.backgroundColor = new Color(0.32f, 0.52f, 0.82f, 1f);
            }
            if (GUILayout.Button(text, EditorStyles.toolbarButton, GUILayout.Height(28)))
            {
                if (page != _page || _timelineDetachedToStandalone)
                {
                    ClosePreviewTimeline();
                }
                _page = page;
            }
            GUI.backgroundColor = oldColor;
        }

        private void DrawActionButton(CombatActionAsset action, int index, float rowWidth)
        {
            if (action == null)
            {
                EditorGUILayout.LabelField($"[{index}] 空引用", EditorStyles.miniLabel);
                return;
            }

            var row = GUILayoutUtility.GetRect(
                Mathf.Max(60, rowWidth),
                32,
                GUILayout.ExpandWidth(true));
            var selected = _page == Page.Skills && _selectedAction == action;
            var hovered = row.Contains(Event.current.mousePosition);
            var background = selected
                ? new Color(0.19f, 0.34f, 0.58f, 1f)
                : hovered
                    ? new Color(0.22f, 0.22f, 0.22f, 1f)
                    : new Color(0.16f, 0.16f, 0.16f, 1f);
            EditorGUI.DrawRect(row, background);
            if (selected)
            {
                EditorGUI.DrawRect(
                    new Rect(row.x, row.y, 3, row.height),
                    new Color(0.35f, 0.65f, 1f, 1f));
            }

            const float deleteButtonHeight = 22;
            var deleteRect = new Rect(
                row.xMax - 28,
                row.y + (row.height - deleteButtonHeight) * 0.5f,
                24,
                deleteButtonHeight);
            var labelRect = new Rect(
                row.x + 3,
                row.y,
                Mathf.Max(30, deleteRect.x - row.x - 3),
                row.height);
            var label = $"{action.ActionId}    {GetActionName(action)}";
            if (GetActionTimeline(action) == null)
            {
                label += "  *";
            }

            if (GUI.Button(labelRect, label, SkillItemStyle))
            {
                if (_selectedAction != action || _timelineDetachedToStandalone)
                {
                    ClosePreviewTimeline();
                }
                _selectedAction = action;
                _page = Page.Skills;
                _actionSerialized = null;
                _timelineDetachedToStandalone = false;
            }
            if (GUI.Button(deleteRect, "-", SkillDeleteStyle))
            {
                RemoveAction(action);
            }
            GUILayout.Space(3);
        }

        private void DrawProfilePage()
        {
            GUILayout.Label("基础参数", EditorStyles.boldLabel);

            var serialized = GetProfileSerialized();
            serialized.Update();
            Undo.RecordObject(_profile, "修改角色战斗配置");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProperty(serialized, "group", "角色分组");
                DrawProperty(serialized, "frameRate", "逻辑帧率");
                DrawProperty(serialized, "moveSpeedPerSecond", "移动速度 / 秒");
                DrawProperty(serialized, "turnDegreesPerSecond", "转向速度 / 秒");
            }
            ApplyProfile(serialized);

            EditorGUILayout.Space(8);            

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("创建基础普攻示例", GUILayout.Width(170)))
                {
                    AddAction(true);
                }
                if (GUILayout.Button("创建缺失技能 Timeline", GUILayout.Width(170)))
                {
                    CreateMissingActionTimelines();
                }
                if (GUILayout.Button("创建缺失状态 Timeline", GUILayout.Width(170)))
                {
                    CreateMissingStateTimelines();
                }
            }
            DrawValidationPanel();
        }

        private void DrawStatesPage()
        {
            var serialized = GetProfileSerialized();
            serialized.Update();
            Undo.RecordObject(_profile, "修改状态表现配置");
            var presentations = serialized.FindProperty("statePresentations");
            if (presentations == null)
            {
                EditorGUILayout.HelpBox("找不到 statePresentations 字段。", MessageType.Error);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"已配置 {presentations.arraySize} 条映射", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("添加默认映射", GUILayout.Width(110)))
                {
                    AddNextDefaultPresentation();
                    return;
                }
                if (GUILayout.Button("补齐空 Timeline", GUILayout.Width(110)))
                {
                    CreateMissingStateTimelines();
                }
            }

            var changed = false;
            for (var i = 0; i < presentations.arraySize; i++)
            {
                changed |= DrawPresentationRow(presentations, i);
            }
            if (presentations.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "还没有状态表现映射。建议至少配置 Locomotion/Idle 和 Locomotion/Move。",
                    MessageType.Warning);
            }

            if (serialized.ApplyModifiedPropertiesWithoutUndo() || changed)
            {
                _profile.ValidateData();
                EditorUtility.SetDirty(_profile);
                _issues.Clear();
            }
            DrawValidationPanel();
        }

        private bool DrawPresentationRow(SerializedProperty presentations, int index)
        {
            var element = presentations.GetArrayElementAtIndex(index);
            var layer = element.FindPropertyRelative("layer");
            var stateId = element.FindPropertyRelative("stateId");
            var variant = element.FindPropertyRelative("variantId");
            var displayName = element.FindPropertyRelative("displayName");
            var priority = element.FindPropertyRelative("priority");
            var timelineProperty = element.FindPropertyRelative("timeline");
            var stableId = element.FindPropertyRelative("stableId");
            if (layer == null || stateId == null || variant == null || timelineProperty == null)
            {
                EditorGUILayout.HelpBox($"第 {index + 1} 条状态映射字段不完整。", MessageType.Error);
                return false;
            }

            var changed = false;
            var currentLayer = (StateLayer)layer.enumValueIndex;
            var layerIndex = Array.IndexOf(PresentationLayers, currentLayer);
            var ids = CombatEditorUtility.GetDefinedStateIds(currentLayer);
            var stateName = CombatStateId.GetDisplayName(currentLayer, stateId.intValue);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(
                        $"{CombatEditorUtility.GetStateLayerDisplayName(currentLayer)} / {stateName}",
                        EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("打开", GUILayout.Width(48)) && timelineProperty.objectReferenceValue != null)
                    {
                        OpenTimeline(timelineProperty.objectReferenceValue as TimelineAsset);
                    }
                    if (GUILayout.Button("移除", GUILayout.Width(48)))
                    {
                        presentations.DeleteArrayElementAtIndex(index);
                        return true;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (layerIndex >= 0)
                    {
                        var names = new string[PresentationLayers.Length];
                        for (var i = 0; i < names.Length; i++)
                        {
                            names[i] = CombatEditorUtility.GetStateLayerDisplayName(PresentationLayers[i]);
                        }
                        var nextLayer = EditorGUILayout.Popup("层", layerIndex, names);
                        if (nextLayer != layerIndex)
                        {
                            layer.enumValueIndex = (int)PresentationLayers[nextLayer];
                            stateId.intValue = CombatEditorUtility.GetDefinedStateIds(PresentationLayers[nextLayer])[0];
                            changed = true;
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(layer, new GUIContent("层"));
                    }

                    ids = CombatEditorUtility.GetDefinedStateIds((StateLayer)layer.enumValueIndex);
                    var stateNames = new string[ids.Length];
                    for (var i = 0; i < ids.Length; i++)
                    {
                        stateNames[i] = CombatStateId.GetDisplayName((StateLayer)layer.enumValueIndex, ids[i]);
                    }
                    if (ids.Length > 0)
                    {
                        var nextState = EditorGUILayout.IntPopup("状态", stateId.intValue, stateNames, ids);
                        if (nextState != stateId.intValue)
                        {
                            stateId.intValue = nextState;
                            changed = true;
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(stateId, new GUIContent("状态 ID"));
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(variant, new GUIContent("variantId"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        variant.stringValue = CombatStatePresentation.NormalizeVariantId(variant.stringValue);
                        changed = true;
                    }
                    if (priority != null)
                    {
                        EditorGUILayout.PropertyField(priority, new GUIContent("优先级"), GUILayout.Width(130));
                    }
                }

                if (displayName != null)
                {
                    EditorGUILayout.PropertyField(displayName, new GUIContent("显示名称"));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(timelineProperty, new GUIContent("Timeline"));
                    changed |= EditorGUI.EndChangeCheck();
                    var timeline = timelineProperty.objectReferenceValue as TimelineAsset;
                    if (GUILayout.Button(timeline == null ? "新建" : "编辑", GUILayout.Width(52)))
                    {
                        if (timeline == null)
                        {
                            Undo.IncrementCurrentGroup();
                            var timelineUndoGroup = Undo.GetCurrentGroup();
                            Undo.SetCurrentGroupName("创建状态 Timeline");
                            timeline = CombatEditorUtility.CreateTimelineAsset(
                                _profile,
                                CombatEditorUtility.GetStatePresentationTimelineSuffix(
                                    (StateLayer)layer.enumValueIndex,
                                    stateId.intValue,
                                    variant.stringValue),
                                true);
                            if (timeline != null)
                            {
                                Undo.RecordObject(_profile, "关联状态 Timeline");
                            }
                            timelineProperty.objectReferenceValue = timeline;
                            changed = timeline != null;
                            Undo.CollapseUndoOperations(timelineUndoGroup);
                        }
                        else
                        {
                            OpenTimeline(timeline);
                        }
                    }
                    using (new EditorGUI.DisabledScope(timeline == null))
                    {
                        if (GUILayout.Button("定位", GUILayout.Width(48)))
                        {
                            Selection.activeObject = timeline;
                            EditorGUIUtility.PingObject(timeline);
                        }
                        if (GUILayout.Button("同步", GUILayout.Width(48)))
                        {
                            SetTimelineFrameRate(timeline, _profile.FrameRate);
                        }
                    }
                }

                if (stableId != null)
                {
                    EditorGUILayout.LabelField("StableId", stableId.stringValue, EditorStyles.miniLabel);
                }
                var currentTimeline = timelineProperty.objectReferenceValue as TimelineAsset;
                if (currentTimeline == null)
                {
                    EditorGUILayout.HelpBox("未关联 Timeline。", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Timeline",
                        $"{currentTimeline.FrameRate} fps / {currentTimeline.DurationFrames} 帧",
                        EditorStyles.miniLabel);
                }
            }
            return changed;
        }

        private void DrawSkillsPage()
        {
            if (!IsActionInProfile(_selectedAction))
            {
                EditorGUILayout.HelpBox("请从左侧技能列表选择技能。", MessageType.Info);
                return;
            }

            var action = _selectedAction;
            var timeline = GetActionTimeline(action);
            GUILayout.Label($"技能：{GetActionName(action)}", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "技能逻辑保存在 CombatActionAsset；表现 Timeline 通过 Profile 关联。下方双源时间轴共用同一帧标尺。",
                MessageType.Info);

            var serialized = GetActionSerialized();
            serialized.Update();
            Undo.RecordObject(action, "修改技能逻辑配置");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (DrawSection("逻辑数据（CombatActionAsset）", LogicSectionKey))
                {
                    EditorGUILayout.ObjectField("逻辑资产", action, typeof(CombatActionAsset), false);
                    DrawProperty(serialized, "stableId", "StableId");
                    DrawProperty(serialized, "actionId", "技能 ID");
                    DrawProperty(serialized, "displayName", "显示名称");
                    DrawProperty(serialized, "durationFrames", "逻辑持续帧数");
                    DrawProperty(serialized, "movementPolicy", "移动策略");
                }
            }
            if (serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                action.ValidateData();
                EditorUtility.SetDirty(action);
                _issues.Clear();
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (DrawSection("客户端表现映射（CharacterCombatProfile）", PresentationSectionKey))
                {
                    timeline = DrawActionPresentation(action);
                    if (timeline == null)
                    {
                        EditorGUILayout.HelpBox("尚未关联技能 Timeline。", MessageType.Warning);
                        if (GUILayout.Button("创建技能 Timeline", GUILayout.Width(150)))
                        {
                            CreateActionTimeline(action);
                            timeline = GetActionTimeline(action);
                        }
                    }
                    else
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var clip = CombatEditorUtility.GetPrimaryAnimationClip(timeline);
                            var nextClip = (AnimationClip)EditorGUILayout.ObjectField(
                                "首个动画 Clip",
                                clip,
                                typeof(AnimationClip),
                                false);
                            if (nextClip != clip)
                            {
                                Undo.IncrementCurrentGroup();
                                var undoGroup = Undo.GetCurrentGroup();
                                Undo.SetCurrentGroupName("设置技能动画");
                                Undo.RecordObject(timeline, "设置技能动画");
                                if (CombatEditorUtility.SetPrimaryAnimationClip(timeline, nextClip, out _))
                                {
                                    CombatEditorUtility.SyncActionDurationToTimeline(action, timeline);
                                    _actionSerialized = null;
                                    EditorUtility.SetDirty(timeline);
                                    AssetDatabase.SaveAssets();
                                }
                                Undo.CollapseUndoOperations(undoGroup);
                            }
                        }
                        EditorGUILayout.LabelField(
                            "Timeline",
                            $"{timeline.FrameRate} fps / {timeline.DurationFrames} 帧",
                            EditorStyles.miniLabel);
                    }
                }
            }

            DrawValidationPanel();
        }

        private TimelineAsset DrawActionPresentation(CombatActionAsset action)
        {
            var current = GetActionTimeline(action);
            var next = (TimelineAsset)EditorGUILayout.ObjectField(
                "表现 Timeline",
                current,
                typeof(TimelineAsset),
                false);
            if (next != current)
            {
                if (CombatEditorUtility.TrySetActionTimeline(
                        _profile,
                        action,
                        next,
                        out var error))
                {
                    AssetDatabase.SaveAssets();
                    _profileSerialized = null;
                    _issues.Clear();
                    current = next;
                }
                else
                {
                    Debug.LogError(error, _profile);
                    ShowNotification(new GUIContent(error));
                }
            }

            if (current != null && current.DurationFrames != action.DurationFrames)
            {
                EditorGUILayout.HelpBox(
                    $"逻辑时长 {action.DurationFrames} 帧，表现 Timeline {current.DurationFrames} 帧。" +
                    "两者独立保存，请确认差异符合设计。",
                    MessageType.Warning);
            }
            return current;
        }

        /// <summary>
        /// 右栏分区折叠头：展开状态按分区记在 EditorPrefs，避免每次打开都要重新展开。
        /// </summary>
        private bool DrawSection(string title, string foldKey, bool defaultExpanded = true)
        {
            var expanded = EditorPrefs.GetBool(foldKey, defaultExpanded);
            var next = EditorGUILayout.Foldout(expanded, title, true, SectionFoldoutStyle);
            if (next != expanded)
            {
                EditorPrefs.SetBool(foldKey, next);
                _rightPanelManuallySized = false;
                _rightPanelContentHeight = 0;
                ApplyRightPanelLayout();
            }
            return next;
        }

        private void DrawValidationPanel()
        {
            if (!_showIssues)
            {
                return;
            }

            EditorGUILayout.Space(8);
            var errors = CountIssues(CombatValidationSeverity.Error);
            var warnings = CountIssues(CombatValidationSeverity.Warning);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (DrawSection($"诊断（错误 {errors} / 警告 {warnings}）", ValidationSectionKey))
                {
                    if (_issues.Count == 0)
                    {
                        EditorGUILayout.HelpBox("没有发现问题。", MessageType.Info);
                    }
                    else
                    {
                        foreach (var issue in _issues)
                        {
                            var prefix = issue.Severity == CombatValidationSeverity.Error
                                ? "错误"
                                : issue.Severity == CombatValidationSeverity.Warning ? "警告" : "提示";
                            EditorGUILayout.LabelField($"[{prefix}] {issue.Message}", EditorStyles.miniLabel);
                        }
                    }
                }
            }
        }

        private void CreateProfile()
        {
            CombatEditorUtility.EnsureFolder(CombatEditorUtility.CombatRoot);
            var path = EditorUtility.SaveFilePanelInProject(
                "创建角色战斗配置",
                "CharacterCombatProfile",
                "asset",
                "请选择保存位置。",
                CombatEditorUtility.CombatRoot);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            path = CombatEditorUtility.NormalizeAssetPath(path);
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建角色战斗配置");
            var profile = ScriptableObject.CreateInstance<CharacterCombatProfile>();
            var serialized = new SerializedObject(profile);
            var group = serialized.FindProperty("group");
            if (group != null)
            {
                group.stringValue = GetProfileGroupFromPath(path);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            profile.ValidateData();
            AssetDatabase.CreateAsset(profile, path);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            var persistedProfile = AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(path);
            Undo.RegisterCreatedObjectUndo(persistedProfile ?? profile, "创建角色战斗配置");
            Undo.CollapseUndoOperations(undoGroup);
            SetProfile(persistedProfile ?? profile);
            Selection.activeObject = _profile;
        }

        private void AddAction(bool basicAttack)
        {
            if (_profile == null)
            {
                return;
            }
            var profilePath = AssetDatabase.GetAssetPath(_profile);
            if (string.IsNullOrEmpty(profilePath))
            {
                EditorUtility.DisplayDialog("无法创建技能", "请先保存 CharacterCombatProfile。", "确定");
                return;
            }

            if (basicAttack)
            {
                var existingAttack = _profile.FindAction(DefaultActionId);
                if (existingAttack != null)
                {
                    _selectedAction = existingAttack;
                    _page = Page.Skills;
                    ShowNotification(new GUIContent("当前 Profile 已有 Action 1001"));
                    return;
                }
            }

            var actionId = basicAttack ? DefaultActionId : GetNextActionId();
            var displayName = basicAttack ? "普通攻击" : $"技能 {actionId}";
            var sampleClip = basicAttack ? FindHeroZsAttackClip() : null;
            if (!CombatEditorUtility.TryCreateActionAssets(
                    _profile,
                    actionId,
                    BuildActionStableId(actionId),
                    displayName,
                    DefaultActionDuration,
                    ActionMovementPolicy.Block,
                    GetActionTimelineSuffix(actionId),
                    sampleClip,
                    out var action,
                    out _,
                    out var error))
            {
                EditorUtility.DisplayDialog("无法创建技能", error, "确定");
                return;
            }

            _profileSerialized = null;
            _selectedAction = action;
            _page = Page.Skills;
            _actionSerialized = null;
            _timelineDetachedToStandalone = false;
            Selection.activeObject = _selectedAction;
            ShowNotification(new GUIContent($"已创建：{displayName}（Action {actionId}）"));
        }

        private void AddNextDefaultPresentation()
        {
            if (_profile == null)
            {
                return;
            }

            // 候选集合来自枚举反射（CombatStateId.GetMappableStateIds），新增枚举状态会自动纳入，无需手改。
            foreach (var layer in PresentationLayers)
            {
                var stateIds = CombatStateId.GetMappableStateIds(layer);
                foreach (var stateId in stateIds)
                {
                    if (FindPresentation(layer, stateId, CombatStatePresentation.DefaultVariantId) != null)
                    {
                        continue;
                    }

                    Undo.IncrementCurrentGroup();
                    var undoGroup = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("添加状态表现映射");
                    var serialized = GetProfileSerialized();
                    serialized.Update();
                    Undo.RecordObject(_profile, "添加状态表现映射");
                    var list = serialized.FindProperty("statePresentations");
                    var index = list.arraySize;
                    list.InsertArrayElementAtIndex(index);
                    var element = list.GetArrayElementAtIndex(index);
                    SetRelativeString(element, "stableId", CombatStatePresentation.BuildStableId(
                        layer, stateId, CombatStatePresentation.DefaultVariantId));
                    SetRelativeEnum(element, "layer", (int)layer);
                    SetRelativeInt(element, "stateId", stateId);
                    SetRelativeString(element, "variantId", CombatStatePresentation.DefaultVariantId);
                    SetRelativeString(element, "displayName", CombatStateId.GetDisplayName(layer, stateId));
                    SetRelativeInt(element, "priority", 0);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    _profile.ValidateData();
                    EditorUtility.SetDirty(_profile);
                    Undo.CollapseUndoOperations(undoGroup);
                    _page = Page.States;
                    Repaint();
                    return;
                }
            }
            ShowNotification(new GUIContent("默认状态映射已经全部存在"));
        }

        private void CreateMissingStateTimelines()
        {
            if (_profile?.StatePresentations == null)
            {
                return;
            }
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("补齐状态 Timeline");
            var serialized = GetProfileSerialized();
            serialized.Update();
            Undo.RecordObject(_profile, "补齐状态 Timeline");
            var list = serialized.FindProperty("statePresentations");
            var created = 0;
            for (var i = 0; i < list.arraySize; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                var timelineProperty = element.FindPropertyRelative("timeline");
                if (timelineProperty == null || timelineProperty.objectReferenceValue != null)
                {
                    continue;
                }
                var layer = (StateLayer)element.FindPropertyRelative("layer").enumValueIndex;
                var stateId = element.FindPropertyRelative("stateId").intValue;
                var variant = element.FindPropertyRelative("variantId").stringValue;
                if (!CombatStateId.IsStatePresentationMappable(layer, stateId))
                {
                    continue;
                }
                var timeline = CombatEditorUtility.CreateTimelineAsset(
                    _profile,
                    CombatEditorUtility.GetStatePresentationTimelineSuffix(layer, stateId, variant),
                    true);
                if (timeline != null)
                {
                    timelineProperty.objectReferenceValue = timeline;
                    created++;
                }
            }
            if (created > 0)
            {
                Undo.RecordObject(_profile, "补齐状态 Timeline");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                _profile.ValidateData();
                EditorUtility.SetDirty(_profile);
                AssetDatabase.SaveAssets();
            }
            Undo.CollapseUndoOperations(undoGroup);
            ShowNotification(new GUIContent(created == 0 ? "没有需要创建的状态 Timeline" : $"已创建 {created} 条状态 Timeline"));
        }

        private TimelineAsset CreateActionTimeline(CombatActionAsset action)
        {
            if (_profile == null || action == null)
            {
                return null;
            }

            var existing = GetActionTimeline(action);
            if (existing != null)
            {
                return existing;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建技能 Timeline");
            var timeline = CombatEditorUtility.CreateTimelineAsset(
                _profile,
                GetActionTimelineSuffix(action.ActionId),
                true);
            if (timeline == null)
            {
                Undo.CollapseUndoOperations(undoGroup);
                return null;
            }

            if (!CombatEditorUtility.TrySetActionTimeline(
                    _profile,
                    action,
                    timeline,
                    out var error))
            {
                Debug.LogError(error, _profile);
                DeleteAsset(timeline);
                Undo.CollapseUndoOperations(undoGroup);
                return null;
            }

            _profileSerialized = null;
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            _issues.Clear();
            return timeline;
        }

        private void CreateMissingActionTimelines()
        {
            if (_profile?.Actions == null)
            {
                return;
            }
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("补齐技能 Timeline");
            var created = 0;
            foreach (var action in _profile.Actions)
            {
                if (action == null || GetActionTimeline(action) != null)
                {
                    continue;
                }
                if (CreateActionTimeline(action) != null)
                {
                    created++;
                }
            }
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            ShowNotification(new GUIContent(created == 0 ? "没有需要创建的技能 Timeline" : $"已创建 {created} 条技能 Timeline"));
        }

        private void SyncAllTimelineFrameRates()
        {
            if (_profile == null)
            {
                return;
            }
            var timelines = new HashSet<TimelineAsset>();
            if (_profile.StatePresentations != null)
            {
                foreach (var presentation in _profile.StatePresentations)
                {
                    if (presentation?.Timeline != null)
                    {
                        timelines.Add(presentation.Timeline);
                    }
                }
            }
            if (_profile.ActionPresentations != null)
            {
                foreach (var presentation in _profile.ActionPresentations)
                {
                    if (presentation?.Timeline != null)
                    {
                        timelines.Add(presentation.Timeline);
                    }
                }
            }
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("同步全部 Timeline 帧率");
            var changed = 0;
            foreach (var timeline in timelines)
            {
                if (timeline.FrameRate == _profile.FrameRate)
                {
                    continue;
                }
                Undo.RecordObject(timeline, "同步全部 Timeline 帧率");
                if (timeline.SetFrameRate(_profile.FrameRate))
                {
                    timeline.ValidateData();
                    EditorUtility.SetDirty(timeline);
                    changed++;
                }
            }
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            ShowNotification(new GUIContent(changed == 0 ? "帧率已经一致" : $"已同步 {changed} 条 Timeline"));
        }

        private void SetTimelineFrameRate(TimelineAsset timeline, int frameRate)
        {
            if (timeline == null || timeline.FrameRate == frameRate)
            {
                return;
            }
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("同步 Timeline 帧率");
            Undo.RecordObject(timeline, "同步 Timeline 帧率");
            timeline.SetFrameRate(frameRate);
            timeline.ValidateData();
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
        }

        private void SaveAll()
        {
            if (_profile == null)
            {
                return;
            }
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("保存角色战斗配置");
            Undo.RecordObject(_profile, "保存角色战斗配置");
            _profile.ValidateData();
            EditorUtility.SetDirty(_profile);
            if (_profile.Actions != null)
            {
                foreach (var action in _profile.Actions)
                {
                    if (action == null)
                    {
                        continue;
                    }
                    Undo.RecordObject(action, "保存角色战斗配置");
                    action.ValidateData();
                    EditorUtility.SetDirty(action);
                }
            }
            if (_profile.ActionPresentations != null)
            {
                foreach (var presentation in _profile.ActionPresentations)
                {
                    if (presentation?.Timeline != null)
                    {
                        Undo.RecordObject(presentation.Timeline, "保存角色战斗配置");
                        presentation.Timeline.ValidateData();
                        EditorUtility.SetDirty(presentation.Timeline);
                    }
                }
            }
            if (_profile.StatePresentations != null)
            {
                foreach (var presentation in _profile.StatePresentations)
                {
                    if (presentation?.Timeline != null)
                    {
                        Undo.RecordObject(presentation.Timeline, "保存角色战斗配置");
                        presentation.Timeline.ValidateData();
                        EditorUtility.SetDirty(presentation.Timeline);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            ShowNotification(new GUIContent("已保存"));
        }

        private void ValidateAndReport()
        {
            if (_profile == null)
            {
                return;
            }
            _issues = CombatEditorUtility.ValidateProfile(_profile);
            _showIssues = true;
            foreach (var issue in _issues)
            {
                if (issue.Severity == CombatValidationSeverity.Error)
                {
                    Debug.LogError(issue.Message, issue.Context);
                }
                else if (issue.Severity == CombatValidationSeverity.Warning)
                {
                    Debug.LogWarning(issue.Message, issue.Context);
                }
            }
            ShowNotification(new GUIContent(
                $"校验完成：错误 {CountIssues(CombatValidationSeverity.Error)}，警告 {CountIssues(CombatValidationSeverity.Warning)}"));
        }

        private void RemoveAction(CombatActionAsset action)
        {
            if (_profile == null || action == null)
            {
                return;
            }
            if (!EditorUtility.DisplayDialog(
                    "移除技能",
                    $"从 {_profile.name} 的技能列表中移除“{GetActionName(action)}”？\n资源文件不会被删除。",
                    "移除",
                    "取消"))
            {
                return;
            }

            var wasSelected = _selectedAction == action;
            if (wasSelected)
            {
                ClosePreviewTimeline();
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("从角色移除技能");
            var serialized = GetProfileSerialized();
            serialized.Update();
            Undo.RecordObject(_profile, "从角色移除技能");
            var actions = serialized.FindProperty("actions");
            var presentations = serialized.FindProperty("actionPresentations");
            if (presentations != null)
            {
                for (var i = presentations.arraySize - 1; i >= 0; i--)
                {
                    var presentation = presentations.GetArrayElementAtIndex(i);
                    if (presentation.FindPropertyRelative("action")?.objectReferenceValue == action)
                    {
                        presentations.DeleteArrayElementAtIndex(i);
                    }
                }
            }
            if (actions != null)
            {
                for (var i = actions.arraySize - 1; i >= 0; i--)
                {
                    if (actions.GetArrayElementAtIndex(i).objectReferenceValue == action)
                    {
                        actions.DeleteArrayElementAtIndex(i);
                    }
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _profile.ValidateData();
            EditorUtility.SetDirty(_profile);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            if (wasSelected)
            {
                _selectedAction = FindFirstAction();
                _actionSerialized = null;
                _timelineDetachedToStandalone = false;
            }
            ScheduleEmbeddedTimelineRefresh();
        }

        private void OpenTimeline(TimelineAsset timeline, bool autoPlay = false)
        {
            if (timeline != null)
            {
                ClosePreviewTimeline();
                var standalone = TimelineWindow.Open(timeline, _previewObject, autoPlay);
                _timelineDetachedToStandalone = standalone != null;
            }
        }

        private void SetProfile(CharacterCombatProfile profile)
        {
            if (_profile != profile)
            {
                ClosePreviewTimeline();
            }
            _profile = profile;
            _timelineDetachedToStandalone = false;
            _profileSerialized = null;
            _actionSerialized = null;
            _selectedAction = FindFirstAction();
            _page = Page.Profile;
            _issues.Clear();
            _showIssues = false;
            _contentScroll = Vector2.zero;

            if (_profile != null)
            {
                SaveProfile(_profile);
                var profilePreview = LoadPreviewObject(_profile);
                if (profilePreview != null || _previewObject == null)
                {
                    _previewObject = profilePreview ?? LoadAssetByGuid<GameObject>(
                        LastPreviewObjectKey);
                }

            }
        }

        private void SaveEditorState()
        {
            SaveProfile(_profile);
            SavePreviewObject(_profile, _previewObject);
        }

        private static void SaveProfile(CharacterCombatProfile profile)
        {
            var guid = GetAssetGuid(profile);
            if (!string.IsNullOrEmpty(guid))
            {
                EditorPrefs.SetString(LastProfileKey, guid);
            }
        }

        private static void SavePreviewObject(CharacterCombatProfile profile, GameObject previewObject)
        {
            var guid = GetAssetGuid(previewObject);
            if (string.IsNullOrEmpty(guid))
            {
                EditorPrefs.DeleteKey(LastPreviewObjectKey);
            }
            else
            {
                EditorPrefs.SetString(LastPreviewObjectKey, guid);
            }

            var profileGuid = GetAssetGuid(profile);
            if (!string.IsNullOrEmpty(profileGuid))
            {
                var key = $"{PreviewObjectKeyPrefix}{profileGuid}";
                if (string.IsNullOrEmpty(guid))
                {
                    EditorPrefs.DeleteKey(key);
                }
                else
                {
                    EditorPrefs.SetString(key, guid);
                }
            }
        }

        private static GameObject LoadPreviewObject(CharacterCombatProfile profile)
        {
            var profileGuid = GetAssetGuid(profile);
            if (string.IsNullOrEmpty(profileGuid))
            {
                return null;
            }

            return LoadAssetByGuid<GameObject>($"{PreviewObjectKeyPrefix}{profileGuid}");
        }

        private static string GetAssetGuid(UnityEngine.Object asset)
        {
            return asset != null &&
                   AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long _)
                ? guid
                : string.Empty;
        }

        private static T LoadAssetByGuid<T>(string key) where T : UnityEngine.Object
        {
            var guid = EditorPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                EditorPrefs.DeleteKey(key);
            }
            return asset;
        }

        private SerializedObject GetProfileSerialized()
        {
            if (_profileSerialized == null || _profileSerialized.targetObject != _profile)
            {
                _profileSerialized = new SerializedObject(_profile);
            }
            return _profileSerialized;
        }

        private SerializedObject GetActionSerialized()
        {
            if (_actionSerialized == null || _actionSerialized.targetObject != _selectedAction)
            {
                _actionSerialized = new SerializedObject(_selectedAction);
            }
            return _actionSerialized;
        }

        private void ApplyProfile(SerializedObject serialized)
        {
            if (serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                _profile.ValidateData();
                EditorUtility.SetDirty(_profile);
                _issues.Clear();
            }
        }

        private CombatStatePresentation FindPresentation(StateLayer layer, int stateId, string variant)
        {
            if (_profile?.StatePresentations == null)
            {
                return null;
            }
            var normalized = CombatStatePresentation.NormalizeVariantId(variant);
            foreach (var presentation in _profile.StatePresentations)
            {
                if (presentation != null && presentation.Layer == layer &&
                    presentation.StateId == stateId &&
                    string.Equals(presentation.VariantId, normalized, StringComparison.Ordinal))
                {
                    return presentation;
                }
            }
            return null;
        }

        private TimelineAsset GetActionTimeline(CombatActionAsset action)
        {
            return _profile?.GetActionTimeline(action);
        }

        private CombatActionAsset FindFirstAction()
        {
            if (_profile?.Actions == null)
            {
                return null;
            }
            foreach (var action in _profile.Actions)
            {
                if (action != null)
                {
                    return action;
                }
            }
            return null;
        }

        private bool IsActionInProfile(CombatActionAsset action)
        {
            if (action == null || _profile?.Actions == null)
            {
                return false;
            }
            foreach (var candidate in _profile.Actions)
            {
                if (candidate == action)
                {
                    return true;
                }
            }
            return false;
        }

        private int GetNextActionId()
        {
            var next = DefaultActionId;
            if (_profile?.Actions != null)
            {
                foreach (var action in _profile.Actions)
                {
                    if (action != null)
                    {
                        next = Mathf.Max(next, action.ActionId + 1);
                    }
                }
            }
            return GetAvailableActionId(next);
        }

        private int GetAvailableActionId(int start)
        {
            var candidate = Mathf.Max(1, start);
            while (_profile?.FindAction(candidate) != null)
            {
                candidate++;
            }
            return candidate;
        }

        private string BuildActionStableId(int actionId)
        {
            var prefix = string.IsNullOrWhiteSpace(_profile?.Group) ? _profile?.name : _profile.Group;
            return $"{CombatEditorUtility.SanitizePathSegment(prefix)}.action.{actionId}";
        }

        private static AnimationClip FindHeroZsAttackClip()
        {
            const string path =
                "Assets/Data/Art/Model/Unit/Hero_ZS/Hero_ZS/Animations/Hero_ZS@Attack.FBX";
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }
            return null;
        }

        private static string GetActionName(CombatActionAsset action)
        {
            if (action == null)
            {
                return "空技能";
            }
            return string.IsNullOrEmpty(action.DisplayName) ? action.name : action.DisplayName;
        }

        private static string GetActionTimelineSuffix(int actionId)
        {
            return actionId == DefaultActionId
                ? "Attack01"
                : $"Action{Mathf.Max(1, actionId):000}";
        }

        private int CountIssues(CombatValidationSeverity severity)
        {
            var count = 0;
            foreach (var issue in _issues)
            {
                if (issue.Severity == severity)
                {
                    count++;
                }
            }
            return count;
        }

        private static string GetProfileGroupFromPath(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
            const string suffix = "CombatProfile";
            if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - suffix.Length);
            }
            return CombatEditorUtility.SanitizePathSegment(
                string.IsNullOrEmpty(fileName) ? "Character" : fileName);
        }

        private static void DrawProperty(SerializedObject serialized, string name, string label)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
            }
        }

        private static void SetProperty(SerializedObject serialized, string name, string value)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
            {
                property.stringValue = value ?? string.Empty;
            }
        }

        private static void SetProperty(SerializedObject serialized, string name, int value, bool enumValue = false)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
            {
                if (enumValue)
                {
                    property.enumValueIndex = value;
                }
                else
                {
                    property.intValue = value;
                }
            }
        }

        private static void SetRelativeString(SerializedProperty parent, string name, string value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null)
            {
                property.stringValue = value ?? string.Empty;
            }
        }

        private static void SetRelativeInt(SerializedProperty parent, string name, int value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetRelativeEnum(SerializedProperty parent, string name, int value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }

        private static void DeleteAsset(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }
            var path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
