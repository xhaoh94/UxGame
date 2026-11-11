using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
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
        private const string ValidationSectionKey = "Ux.CombatEditor.Fold.Validation";
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
        private VisualElement _configContainer;
        private VisualElement _navigationContainer;
        private ScrollView _configScroll;
        private TimelineWindow _prewarmedTimeline;
        private VisualElement _leftPanel;
        private bool _rebuildPending;
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

        [MenuItem("UxGame/工具/战斗", false,520)]
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

            _navigationContainer = new VisualElement
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

            _configContainer = new VisualElement
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
            RebuildNavigation();
            RebuildConfig();
            EditorApplication.delayCall += PrewarmTimeline;
            EditorApplication.delayCall += WarmupTrackTypes;
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
            RequestRebuild();
            _embeddedTimeline?.Repaint();
            ScheduleEmbeddedTimelineRefresh();
            Repaint();
        }

        private void RebuildNavigation()
        {
            if (_navigationContainer == null) return;

            _navigationContainer.style.width = _leftPanelWidth;
            _navigationContainer.Clear();
            BuildNavigation(_navigationContainer);
        }

        private void RebuildConfig()
        {
            if (_configContainer == null) return;

            _configContainer.Clear();
            BuildHeader(_configContainer);

            if (_profile == null)
            {
                BuildEmptyState(_configContainer);
                ScheduleEmbeddedTimelineRefresh();
                return;
            }

            if (!IsActionInProfile(_selectedAction))
            {
                _selectedAction = FindFirstAction();
            }

            _configScroll = new ScrollView();
            _configScroll.style.flexGrow = 1;
            _configScroll.style.minHeight = 0;
            _configScroll.scrollOffset = _contentScroll;
            _configContainer.Add(_configScroll);

            switch (_page)
            {
                case Page.Profile:
                    BuildProfilePage(_configScroll);
                    break;
                case Page.States:
                    BuildStatesPage(_configScroll);
                    break;
                case Page.Skills:
                    BuildSkillsPage(_configScroll);
                    break;
            }

            ScheduleEmbeddedTimelineRefresh();
        }

        /// <summary>把重建推迟到下一帧末尾，避免在回调里同步改 UI 树。</summary>
        private void RequestRebuild()
        {
            if (_rebuildPending) return;
            _rebuildPending = true;
            EditorApplication.delayCall += () =>
            {
                _rebuildPending = false;
                if (this == null) return;
                RebuildNavigation();
                RebuildConfig();
            };
        }

        private static HelpBox CreateHelpBox(string text, MessageType type)
        {
            return new HelpBox(text, ToHelpBoxType(type));
        }

        private static HelpBoxMessageType ToHelpBoxType(MessageType type)
        {
            switch (type)
            {
                case MessageType.Error: return HelpBoxMessageType.Error;
                case MessageType.Warning: return HelpBoxMessageType.Warning;
                case MessageType.Info: return HelpBoxMessageType.Info;
                default: return HelpBoxMessageType.None;
            }
        }

        private static VisualElement Space(float height)
        {
            var element = new VisualElement();
            element.style.height = height;
            return element;
        }

        private static VisualElement FlexibleSpace()
        {
            var element = new VisualElement();
            element.style.flexGrow = 1;
            return element;
        }

        private static Button ToolbarButton(string text, Action onClick, float width = 0)
        {
            var button = new Button(onClick) { text = text };
            if (width > 0) button.style.width = width;
            return button;
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

            // 首次进入 Skills 页：优先复用预创建的窗口，省掉 CreateGUI 的 500ms+。
            if (_embeddedTimeline == null && _prewarmedTimeline != null)
            {
                _embeddedTimeline = _prewarmedTimeline;
                _prewarmedTimeline = null;
                _embeddedTimeline.AttachTo(_embeddedTimelineContainer);
                _embeddedTimelineContainer.style.display = DisplayStyle.Flex;
                ApplyRightPanelLayout();

                if (_embeddedTimeline.TryRebind(timeline, _selectedAction, _profile, _previewObject))
                {
                    _embeddedAction = _selectedAction;
                    _embeddedTimelineAsset = timeline;
                    _embeddedPreviewObject = _previewObject;
                    Repaint();
                    return;
                }

                DestroyEmbeddedTimeline();
            }

            // 复用已存在的嵌入窗口：只切换数据源，不重建整个 Timeline UI。
            if (_embeddedTimeline != null)
            {
                _embeddedAction = _selectedAction;
                _embeddedTimelineAsset = timeline;
                _embeddedPreviewObject = _previewObject;
                if (_embeddedTimeline.TryRebind(timeline, _selectedAction, _profile, _previewObject))
                {
                    Repaint();
                    return;
                }
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

        private void PrewarmTimeline()
        {
            if (this == null || _prewarmedTimeline != null || _embeddedTimeline != null)
            {
                return;
            }

            _prewarmedTimeline = TimelineWindow.CreateDetached();
        }

        private void WarmupTrackTypes()
        {
            if (this == null) return;

            TimelineAssetEditorSource.Warmup();
        }

        private void DiscardPrewarmedTimeline()
        {
            if (_prewarmedTimeline == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(_prewarmedTimeline);
            _prewarmedTimeline = null;
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

        private void BuildHeader(VisualElement parent)
        {
            var box = new VisualElement();
            box.style.marginBottom = 6;

            var row1 = new VisualElement();
            row1.style.flexDirection = FlexDirection.Row;

            var roleLabel = new Label("角色");
            roleLabel.style.width = 36;
            row1.Add(roleLabel);

            var profileField = new ObjectField
            {
                objectType = typeof(CharacterCombatProfile),
                allowSceneObjects = false,
            };
            profileField.SetValueWithoutNotify(_profile);
            profileField.style.flexGrow = 1;
            profileField.RegisterValueChangedCallback(e =>
            {
                SetProfile(e.newValue as CharacterCombatProfile);
                RequestRebuild();
            });
            row1.Add(profileField);

            row1.Add(ToolbarButton("新建", () => { CreateProfile(); RequestRebuild(); }, 48));

            var saveButton = ToolbarButton("保存", SaveAll, 48);
            saveButton.SetEnabled(_profile != null);
            row1.Add(saveButton);

            var validateButton = ToolbarButton("校验", () => { ValidateAndReport(); RequestRebuild(); }, 48);
            validateButton.SetEnabled(_profile != null);
            row1.Add(validateButton);

            row1.Add(FlexibleSpace());

            if (_profile != null)
            {
                var states = _profile.StatePresentations == null ? 0 : _profile.StatePresentations.Count;
                var actions = _profile.Actions == null ? 0 : _profile.Actions.Count;
                row1.Add(new Label($"状态 {states}  技能 {actions}"));
            }
            box.Add(row1);

            var row2 = new VisualElement();
            row2.style.flexDirection = FlexDirection.Row;

            var previewLabel = new Label("预览对象");
            previewLabel.style.width = 60;
            row2.Add(previewLabel);

            var previewField = new ObjectField
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
            };
            previewField.SetValueWithoutNotify(_previewObject);
            previewField.style.flexGrow = 1;
            previewField.RegisterValueChangedCallback(e =>
            {
                _previewObject = e.newValue as GameObject;
                SavePreviewObject(_profile, _previewObject);
                ScheduleEmbeddedTimelineRefresh();
            });
            row2.Add(previewField);

            row2.Add(FlexibleSpace());

            var syncButton = ToolbarButton("同步全部帧率", SyncAllTimelineFrameRates, 96);
            syncButton.SetEnabled(_profile != null);
            row2.Add(syncButton);
            box.Add(row2);

            parent.Add(box);
        }

        private void BuildEmptyState(VisualElement parent)
        {
            var center = new VisualElement();
            center.style.flexGrow = 1;
            center.style.justifyContent = Justify.Center;
            center.style.alignItems = Align.Center;

            var box = new VisualElement();
            box.style.width = 430;

            var title = new Label("角色战斗配置");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            box.Add(title);

            box.Add(CreateHelpBox(
                "选择 CharacterCombatProfile 开始编辑。\n\n" +
                "状态表现只做状态到 Timeline 的映射；技能列表管理主动动作；" +
                "技能的逻辑轨道和表现 Timeline 默认在当前窗口内编辑，也可以按需打开独立窗口。",
                MessageType.Info));

            var createButton = new Button(() => { CreateProfile(); RequestRebuild(); })
            {
                text = "新建角色战斗配置",
            };
            createButton.style.height = 30;
            box.Add(createButton);

            center.Add(box);
            parent.Add(center);
        }

        private void BuildNavigation(VisualElement parent)
        {
            var box = new VisualElement();
            box.style.flexGrow = 1;

            var title = new Label("配置");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            box.Add(title);

            box.Add(BuildPageButton("基础参数", Page.Profile));
            box.Add(BuildPageButton("状态表现映射", Page.States));
            box.Add(Space(10));

            var skillHeader = new VisualElement();
            skillHeader.style.flexDirection = FlexDirection.Row;

            var skillTitle = new Label("技能列表");
            skillTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            skillHeader.Add(skillTitle);
            skillHeader.Add(FlexibleSpace());

            var addButton = new Button(() => { AddAction(false); RequestRebuild(); }) { text = "+" };
            addButton.style.width = 24;
            addButton.SetEnabled(_profile != null);
            skillHeader.Add(addButton);
            box.Add(skillHeader);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.scrollOffset = _leftScroll;

            if (_profile?.Actions == null || _profile.Actions.Count == 0)
            {
                scroll.Add(CreateHelpBox("暂无技能", MessageType.Info));
            }
            else
            {
                for (var i = 0; i < _profile.Actions.Count; i++)
                {
                    scroll.Add(BuildActionRow(_profile.Actions[i], i));
                }
            }
            box.Add(scroll);

            box.Add(CreateHelpBox(
                "代码触发技能，Profile 提供数据，双源时间轴在当前窗口内编辑表现。",
                MessageType.None));

            parent.Add(box);
        }

        private float GetMaxLeftPanelWidth()
        {
            return Mathf.Max(MinLeftPanelWidth, position.width - LeftPanelReserve);
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
            EditorApplication.delayCall -= PrewarmTimeline;
            ClosePreviewTimeline();
            DiscardPrewarmedTimeline();
        }

        private Button BuildPageButton(string text, Page page)
        {
            var button = new Button(() =>
            {
                if (page != _page || _timelineDetachedToStandalone)
                {
                    ClosePreviewTimeline();
                }
                _page = page;
                RequestRebuild();
            })
            {
                text = text,
            };
            button.style.height = 28;
            if (_page == page)
            {
                button.style.backgroundColor = new Color(0.32f, 0.52f, 0.82f, 1f);
            }
            return button;
        }

        private VisualElement BuildActionRow(CombatActionAsset action, int index)
        {
            if (action == null)
            {
                return new Label($"[{index}] 空引用");
            }

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.height = 32;
            row.style.marginBottom = 3;

            var selected = _page == Page.Skills && _selectedAction == action;
            row.style.backgroundColor = selected
                ? new Color(0.19f, 0.34f, 0.58f, 1f)
                : new Color(0.16f, 0.16f, 0.16f, 1f);

            var text = $"{action.ActionId}    {GetActionName(action)}";
            if (GetActionTimeline(action) == null) text += "  *";

            var selectButton = new Button(() =>
            {
                // 切技能时不销毁嵌入 Timeline：RefreshEmbeddedTimeline 会走 TryRebind 复用，
                // 销毁重建整个 Timeline 窗口是切换慢的主要原因。
                // 只有从独立窗口模式切回来时才关闭独立窗口。
                if (_timelineDetachedToStandalone)
                {
                    _timelineDetachedToStandalone = false;
                    TimelineWindow.CloseStandalone();
                }
                _selectedAction = action;
                _page = Page.Skills;
                _actionSerialized = null;
                RequestRebuild();
            })
            {
                text = text,
            };
            selectButton.style.flexGrow = 1;
            selectButton.style.unityTextAlign = TextAnchor.MiddleLeft;
            selectButton.style.paddingLeft = 12;
            row.Add(selectButton);

            var deleteButton = new Button(() => { RemoveAction(action); RequestRebuild(); }) { text = "-" };
            deleteButton.style.width = 24;
            row.Add(deleteButton);

            return row;
        }

        private void BuildProfilePage(VisualElement parent)
        {
            var title = new Label("基础参数");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(title);

            var serialized = GetProfileSerialized();
            serialized.Update();
            Undo.RecordObject(_profile, "修改角色战斗配置");

            var fields = new VisualElement();
            AddProperty(fields, serialized, "group", "角色分组");
            AddProperty(fields, serialized, "frameRate", "逻辑帧率");
            AddProperty(fields, serialized, "moveSpeedPerSecond", "移动速度 / 秒");
            AddProperty(fields, serialized, "turnDegreesPerSecond", "转向速度 / 秒");
            parent.Add(fields);

            fields.Bind(serialized);
            fields.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ApplyProfile(serialized);
            });

            parent.Add(Space(8));

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.Add(new Button(() => { AddAction(true); RequestRebuild(); })
            {
                text = "创建基础普攻示例",
            });
            buttons.Add(new Button(() => { CreateMissingActionTimelines(); RequestRebuild(); })
            {
                text = "创建缺失技能 Timeline",
            });
            buttons.Add(new Button(() => { CreateMissingStateTimelines(); RequestRebuild(); })
            {
                text = "创建缺失状态 Timeline",
            });
            parent.Add(buttons);

            var panel = BuildValidationPanel();
            if (panel != null) parent.Add(panel);
        }

        private void BuildStatesPage(VisualElement parent)
        {
            var serialized = GetProfileSerialized();
            serialized.Update();
            Undo.RecordObject(_profile, "修改状态表现配置");

            var presentations = serialized.FindProperty("statePresentations");
            if (presentations == null)
            {
                parent.Add(CreateHelpBox("找不到 statePresentations 字段。", MessageType.Error));
                return;
            }

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;

            var countLabel = new Label($"已配置 {presentations.arraySize} 条映射");
            countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(countLabel);
            header.Add(FlexibleSpace());

            header.Add(ToolbarButton("添加默认映射", () =>
            {
                AddNextDefaultPresentation();
                RequestRebuild();
            }, 110));

            header.Add(ToolbarButton("补齐空 Timeline", () =>
            {
                CreateMissingStateTimelines();
                RequestRebuild();
            }, 110));
            parent.Add(header);

            for (var i = 0; i < presentations.arraySize; i++)
            {
                var row = BuildPresentationRow(serialized, presentations, i);
                if (row != null) parent.Add(row);
            }

            if (presentations.arraySize == 0)
            {
                parent.Add(CreateHelpBox(
                    "还没有状态表现映射。建议至少配置 Locomotion/Idle 和 Locomotion/Move。",
                    MessageType.Warning));
            }

            var panel = BuildValidationPanel();
            if (panel != null) parent.Add(panel);
        }

        private VisualElement BuildPresentationRow(
            SerializedObject serialized,
            SerializedProperty presentations,
            int index)
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
                return CreateHelpBox($"第 {index + 1} 条状态映射字段不完整。", MessageType.Error);
            }

            var box = new VisualElement();
            box.style.marginBottom = 6;
            box.style.paddingLeft = 6;
            box.style.paddingRight = 6;
            box.style.paddingTop = 4;
            box.style.paddingBottom = 4;
            box.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);

            var currentLayer = (StateLayer)layer.enumValueIndex;
            var layerIndex = Array.IndexOf(PresentationLayers, currentLayer);
            var stateName = CombatStateId.GetDisplayName(currentLayer, stateId.intValue);

            // ---- 标题行 ----
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;

            var title = new Label(
                $"{CombatEditorUtility.GetStateLayerDisplayName(currentLayer)} / {stateName}");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleRow.Add(title);
            titleRow.Add(FlexibleSpace());

            titleRow.Add(ToolbarButton("打开", () =>
            {
                var timeline = timelineProperty.objectReferenceValue as TimelineAsset;
                if (timeline != null) OpenTimeline(timeline);
            }, 48));

            titleRow.Add(ToolbarButton("移除", () =>
            {
                presentations.DeleteArrayElementAtIndex(index);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                _profile.ValidateData();
                EditorUtility.SetDirty(_profile);
                _issues.Clear();
                RequestRebuild();
            }, 48));
            box.Add(titleRow);

            // ---- 层 / 状态 ----
            var selectRow = new VisualElement();
            selectRow.style.flexDirection = FlexDirection.Row;

            if (layerIndex >= 0)
            {
                var layerNames = new List<string>(PresentationLayers.Length);
                for (var i = 0; i < PresentationLayers.Length; i++)
                {
                    layerNames.Add(CombatEditorUtility.GetStateLayerDisplayName(PresentationLayers[i]));
                }

                var layerField = new DropdownField("层", layerNames, layerIndex);
                layerField.style.flexGrow = 1;
                layerField.RegisterValueChangedCallback(e =>
                {
                    var nextIndex = layerNames.IndexOf(e.newValue);
                    if (nextIndex < 0 || nextIndex == layerIndex) return;

                    layer.enumValueIndex = (int)PresentationLayers[nextIndex];
                    var ids2 = CombatEditorUtility.GetDefinedStateIds(PresentationLayers[nextIndex]);
                    if (ids2.Length > 0) stateId.intValue = ids2[0];
                    OnPresentationChanged(serialized);
                    RequestRebuild();
                });
                selectRow.Add(layerField);
            }
            else
            {
                var layerField = new PropertyField(layer, "层");
                selectRow.Add(layerField);
            }

            var ids = CombatEditorUtility.GetDefinedStateIds((StateLayer)layer.enumValueIndex);
            if (ids.Length > 0)
            {
                var stateNames = new List<string>(ids.Length);
                for (var i = 0; i < ids.Length; i++)
                {
                    stateNames.Add(CombatStateId.GetDisplayName((StateLayer)layer.enumValueIndex, ids[i]));
                }

                var stateIndex = Array.IndexOf(ids, stateId.intValue);
                if (stateIndex < 0) stateIndex = 0;

                var stateField = new DropdownField("状态", stateNames, stateIndex);
                stateField.style.flexGrow = 1;
                stateField.RegisterValueChangedCallback(e =>
                {
                    var nextIndex = stateNames.IndexOf(e.newValue);
                    if (nextIndex < 0 || nextIndex >= ids.Length) return;

                    stateId.intValue = ids[nextIndex];
                    OnPresentationChanged(serialized);
                    RequestRebuild();
                });
                selectRow.Add(stateField);
            }
            else
            {
                selectRow.Add(new PropertyField(stateId, "状态 ID"));
            }
            box.Add(selectRow);

            // ---- variantId / priority / displayName ----
            var variantRow = new VisualElement();
            variantRow.style.flexDirection = FlexDirection.Row;

            var variantField = new PropertyField(variant, "variantId");
            variantField.style.flexGrow = 1;
            variantRow.Add(variantField);

            if (priority != null)
            {
                var priorityField = new PropertyField(priority, "优先级");
                priorityField.style.width = 150;
                variantRow.Add(priorityField);
            }
            box.Add(variantRow);

            if (displayName != null)
            {
                box.Add(new PropertyField(displayName, "显示名称"));
            }

            // ---- Timeline ----
            var timelineRow = new VisualElement();
            timelineRow.style.flexDirection = FlexDirection.Row;

            var timelineField = new PropertyField(timelineProperty, "Timeline");
            timelineField.style.flexGrow = 1;
            timelineRow.Add(timelineField);

            var timeline = timelineProperty.objectReferenceValue as TimelineAsset;
            timelineRow.Add(new Button(() =>
            {
                var current = timelineProperty.objectReferenceValue as TimelineAsset;
                if (current == null)
                {
                    Undo.IncrementCurrentGroup();
                    var timelineUndoGroup = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("创建状态 Timeline");
                    current = CombatEditorUtility.CreateTimelineAsset(
                        _profile,
                        CombatEditorUtility.GetStatePresentationTimelineSuffix(
                            (StateLayer)layer.enumValueIndex,
                            stateId.intValue,
                            variant.stringValue),
                        true);
                    if (current != null)
                    {
                        Undo.RecordObject(_profile, "关联状态 Timeline");
                    }
                    timelineProperty.objectReferenceValue = current;
                    Undo.CollapseUndoOperations(timelineUndoGroup);
                    OnPresentationChanged(serialized);
                    RequestRebuild();
                }
                else
                {
                    OpenTimeline(current);
                }
            })
            {
                text = timeline == null ? "新建" : "编辑",
            });

            var locateButton = new Button(() =>
            {
                var current = timelineProperty.objectReferenceValue as TimelineAsset;
                if (current == null) return;
                Selection.activeObject = current;
                EditorGUIUtility.PingObject(current);
            })
            {
                text = "定位",
            };
            locateButton.SetEnabled(timeline != null);
            timelineRow.Add(locateButton);

            var syncButton = new Button(() =>
            {
                var current = timelineProperty.objectReferenceValue as TimelineAsset;
                if (current != null) SetTimelineFrameRate(current, _profile.FrameRate);
            })
            {
                text = "同步",
            };
            syncButton.SetEnabled(timeline != null);
            timelineRow.Add(syncButton);
            box.Add(timelineRow);

            if (stableId != null)
            {
                var stableLabel = new Label($"StableId {stableId.stringValue}");
                stableLabel.style.fontSize = 10;
                box.Add(stableLabel);
            }

            if (timeline == null)
            {
                box.Add(CreateHelpBox("未关联 Timeline。", MessageType.Warning));
            }
            else
            {
                var info = new Label($"{timeline.FrameRate} fps / {timeline.DurationFrames} 帧");
                info.style.fontSize = 10;
                box.Add(info);
            }

            box.Bind(serialized);
            return box;
        }

        private void OnPresentationChanged(SerializedObject serialized)
        {
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _profile.ValidateData();
            EditorUtility.SetDirty(_profile);
            _issues.Clear();
        }

        private void BuildSkillsPage(VisualElement parent)
        {
            if (!IsActionInProfile(_selectedAction))
            {
                parent.Add(CreateHelpBox("请从左侧技能列表选择技能。", MessageType.Info));
                return;
            }

            var action = _selectedAction;
            var serialized = GetActionSerialized();
            serialized.Update();
            Undo.RecordObject(action, "修改技能逻辑配置");

            var fold = BuildSection("配置", LogicSectionKey);
            parent.Add(fold);

            var fields = new VisualElement();
            AddProperty(fields, serialized, "stableId", "StableId");
            AddProperty(fields, serialized, "actionId", "技能 ID");
            AddProperty(fields, serialized, "displayName", "显示名称");
            AddProperty(fields, serialized, "durationFrames", "逻辑帧数");
            AddProperty(fields, serialized, "movementPolicy", "移动策略");
            fold.Add(fields);

            fields.Bind(serialized);
            fields.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                action.ValidateData();
                EditorUtility.SetDirty(action);
                _issues.Clear();
            });

            fold.Add(BuildActionPresentation(action));

            var panel = BuildValidationPanel();
            if (panel != null) parent.Add(panel);
        }

        private VisualElement BuildActionPresentation(CombatActionAsset action)
        {
            var box = new VisualElement();
            var current = GetActionTimeline(action);

            var field = new ObjectField("Timeline")
            {
                objectType = typeof(TimelineAsset),
                allowSceneObjects = false,
            };
            field.SetValueWithoutNotify(current);
            field.RegisterValueChangedCallback(e =>
            {
                var next = e.newValue as TimelineAsset;
                if (CombatEditorUtility.TrySetActionTimeline(_profile, action, next, out var error))
                {
                    AssetDatabase.SaveAssets();
                    _profileSerialized = null;
                    _issues.Clear();
                    RequestRebuild();
                }
                else
                {
                    Debug.LogError(error, _profile);
                    ShowNotification(new GUIContent(error));
                    field.SetValueWithoutNotify(current);
                }
            });
            box.Add(field);

            current = GetActionTimeline(action);
            if (current == null)
            {
                box.Add(CreateHelpBox("尚未关联技能 Timeline。", MessageType.Warning));

                var createButton = new Button(() => { CreateActionTimeline(action); RequestRebuild(); })
                {
                    text = "创建技能 Timeline",
                };
                createButton.style.width = 150;
                box.Add(createButton);
            }
            else if (current.DurationFrames <= 0)
            {
                box.Add(CreateHelpBox(
                    "当前 Timeline 没有表现 Clip，该技能仅使用逻辑时序。",
                    MessageType.Info));
            }
            else if (current.DurationFrames != action.DurationFrames)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;

                row.Add(CreateHelpBox(
                    $"逻辑时长 {action.DurationFrames} 帧，表现 Timeline {current.DurationFrames} 帧。" +
                    "两者独立保存，请确认差异符合设计。",
                    MessageType.Warning));

                var syncButton = new Button(() =>
                {
                    SyncActionDurationFromTimeline(action, current);
                    RequestRebuild();
                })
                {
                    text = "以Timeline帧为准同步",
                };
                syncButton.style.width = 112;
                row.Add(syncButton);
                box.Add(row);
            }

            return box;
        }

        /// <summary>
        /// 右栏分区折叠头：展开状态按分区记在 EditorPrefs，避免每次打开都要重新展开。
        /// </summary>
        private Foldout BuildSection(string title, string foldKey, bool defaultExpanded = true)
        {
            var expanded = EditorPrefs.GetBool(foldKey, defaultExpanded);
            var fold = new Foldout { text = title, value = expanded };
            fold.RegisterValueChangedCallback(e =>
            {
                EditorPrefs.SetBool(foldKey, e.newValue);
                _rightPanelManuallySized = false;
                _rightPanelContentHeight = 0;
                ApplyRightPanelLayout();
            });
            return fold;
        }

        private VisualElement BuildValidationPanel()
        {
            if (!_showIssues)
            {
                return null;
            }

            var wrapper = new VisualElement();
            wrapper.Add(Space(8));

            var errors = CountIssues(CombatValidationSeverity.Error);
            var warnings = CountIssues(CombatValidationSeverity.Warning);

            var fold = BuildSection($"诊断（错误 {errors} / 警告 {warnings}）", ValidationSectionKey);
            if (_issues.Count == 0)
            {
                fold.Add(CreateHelpBox("没有发现问题。", MessageType.Info));
            }
            else
            {
                foreach (var issue in _issues)
                {
                    var prefix = issue.Severity == CombatValidationSeverity.Error
                        ? "错误"
                        : issue.Severity == CombatValidationSeverity.Warning ? "警告" : "提示";

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;

                    var label = new Label($"[{prefix}] {issue.Message}");
                    label.style.fontSize = 10;
                    label.style.flexGrow = 1;
                    row.Add(label);

                    if (issue.Severity == CombatValidationSeverity.Warning &&
                        TryGetDurationMismatch(issue, out var action, out var timeline))
                    {
                        var fixButton = new Button(() =>
                        {
                            SyncActionDurationFromTimeline(action, timeline);
                            RequestRebuild();
                        })
                        {
                            text = "以Timeline帧为准同步",
                        };
                        fixButton.style.width = 112;
                        row.Add(fixButton);
                    }

                    fold.Add(row);
                }
            }

            wrapper.Add(fold);
            return wrapper;
        }

        private bool TryGetDurationMismatch(
            CombatValidationIssue issue,
            out CombatActionAsset action,
            out TimelineAsset timeline)
        {
            action = null;
            timeline = issue?.Context as TimelineAsset;
            if (timeline == null || _profile?.ActionPresentations == null ||
                issue.Message.IndexOf("逻辑时长与表现时长不一致", StringComparison.Ordinal) < 0)
            {
                return false;
            }

            foreach (var presentation in _profile.ActionPresentations)
            {
                if (presentation?.Timeline == timeline && presentation.Action != null)
                {
                    action = presentation.Action;
                    return action.DurationFrames != timeline.DurationFrames;
                }
            }
            return false;
        }

        private void SyncActionDurationFromTimeline(
            CombatActionAsset action,
            TimelineAsset timeline)
        {
            if (action == null || timeline == null || timeline.DurationFrames <= 0)
            {
                return;
            }

            if (action.DurationFrames > timeline.DurationFrames &&
                !EditorUtility.DisplayDialog(
                    "同步逻辑时长",
                    "Timeline 比逻辑动作更短。同步后，超出表现长度的命中/取消窗口和帧事件将被删除，是否继续？",
                    "继续同步",
                    "取消"))
            {
                return;
            }

            if (!CombatEditorUtility.SyncActionDurationExactlyToTimeline(action, timeline))
            {
                return;
            }

            _actionSerialized = null;
            _issues = CombatEditorUtility.ValidateProfile(_profile);
            TimelineWindow.RefreshView?.Invoke();
            TimelineWindow.RefreshClip?.Invoke();
            RequestRebuild();
            _embeddedTimeline?.Repaint();
            Repaint();
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
            var actionAssetName = CombatEditorUtility.GetActionAssetName(_profile, actionId);
            var timelineAssetName = CombatEditorUtility.GetTimelineAssetName(
                _profile,
                GetActionTimelineSuffix(actionId));
            var sampleClip = basicAttack ? FindHeroZsAttackClip() : null;

            CombatActionCreatePopup.Open(
                actionId,
                BuildActionStableId(actionId),
                displayName,
                actionAssetName,
                timelineAssetName,
                (string stableId, string name, string logicName, string timelineName, out string error) =>
                    CreateActionFromPopup(
                        actionId,
                        sampleClip,
                        stableId,
                        name,
                        logicName,
                        timelineName,
                        out error));
        }

        private bool CreateActionFromPopup(
            int actionId,
            AnimationClip sampleClip,
            string stableId,
            string displayName,
            string actionAssetName,
            string timelineAssetName,
            out string error)
        {
            if (!CombatEditorUtility.TryCreateActionAssets(
                    _profile,
                    actionId,
                    stableId,
                    displayName,
                    DefaultActionDuration,
                    ActionMovementPolicy.Block,
                    GetActionTimelineSuffix(actionId),
                    actionAssetName,
                    timelineAssetName,
                    sampleClip,
                    out var action,
                    out _,
                    out error))
            {
                return false;
            }

            _profileSerialized = null;
            _selectedAction = action;
            _page = Page.Skills;
            _actionSerialized = null;
            _timelineDetachedToStandalone = false;
            Selection.activeObject = _selectedAction;
            ShowNotification(new GUIContent($"已创建：{displayName}（Action {actionId}）"));
            Repaint();
            return true;
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
                "Assets/GameRes/Art/Model/Unit/Hero_ZS/Hero_ZS/Animations/Hero_ZS@Attack.FBX";
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

        private static void AddProperty(
            VisualElement parent,
            SerializedObject serialized,
            string name,
            string label)
        {
            var property = serialized.FindProperty(name);
            if (property == null) return;

            parent.Add(new PropertyField(property, label));
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
