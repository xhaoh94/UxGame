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
    /// 瑙掕壊鎴樻枟鍐呭鍏ュ彛銆?    ///
    /// 杩欓噷璐熻矗瑙掕壊 Profile 鍙傛暟銆佸畯瑙傜姸鎬佸埌 Timeline 鐨勬槧灏勫拰鎶€鑳借祫婧愬垪琛ㄣ€?    /// 鎶€鑳界殑閫昏緫杞ㄩ亾涓庤〃鐜?Clip 榛樿浠ュ唴宓屽弻婧愭椂闂磋酱缂栬緫锛屼粛淇濈暀鐙珛 TimelineWindow 鍏ュ彛銆?    /// </summary>
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

        // 宸︽爮瀹藉害鍙嫋鍔ㄨ皟鏁达細榛樿 205锛屾渶灏?150锛屽苟涓哄彸渚у唴瀹瑰尯淇濈暀 300銆?        private const float MinLeftPanelWidth = 150f;
        private const float DefaultLeftPanelWidth = 205f;
        private const float LeftPanelReserve = 300f;
        private const string LeftPanelWidthKey = "Ux.CombatEditor.LeftPanelWidth";
        private const string LastProfileKey = "Ux.CombatEditor.LastProfile";
        private const string LastPreviewObjectKey = "Ux.CombatEditor.LastPreviewObject";
        private const string PreviewObjectKeyPrefix = "Ux.CombatEditor.PreviewObject.";
        private const string RightPanelRatioKey = "Ux.CombatEditor.RightPanelRatio";

        // 鍙虫爮鍒嗗尯鎶樺彔鐘舵€侊細鎸夊垎鍖虹嫭绔嬭蹇嗭紝閬垮厤姣忔鎵撳紑閮借閲嶆柊灞曞紑銆?        private const string LogicSectionKey = "Ux.CombatEditor.Fold.Logic";
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
                tooltip = vertical ? "鎷栧姩璋冩暣宸︿晶瀹藉害" : "鎷栧姩璋冩暣涓婃柟淇℃伅楂樺害";
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

        [MenuItem("UxGame/宸ュ叿/鎴樻枟/瑙掕壊閰嶇疆", false, 520)]
        public static void ShowWindow()
        {
            var window = GetWindow<CombatEditorWindow>();
            window.titleContent = new GUIContent("瑙掕壊鎴樻枟閰嶇疆");
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
                // 娌℃湁鎵嬪姩鎷栧姩杩囨椂浣跨敤 IMGUI 鑷韩鐨勫唴瀹归珮搴︼紝鎶樺彔鍒嗗尯鍚?Timeline 浼氳嚜鍔ㄤ笂绉汇€?                _configContainer.style.height = StyleKeyword.Auto;
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
            window.titleContent = new GUIContent("瑙掕壊鎴樻枟閰嶇疆");
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
            titleContent = new GUIContent("瑙掕壊鎴樻枟閰嶇疆");
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
                // 鎶€鑳介〉闇€瑕佹妸瀹屾暣淇℃伅楂樺害浜ょ粰澶栧眰甯冨眬锛岄伩鍏?ScrollView 鎶?Timeline 椤跺嚭澶ф绌虹櫧銆?                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
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
                    GUILayout.Label("瑙掕壊", EditorStyles.miniLabel, GUILayout.Width(36));
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

                    if (GUILayout.Button("鏂板缓", EditorStyles.toolbarButton, GUILayout.Width(48)))
                    {
                        CreateProfile();
                    }
                    using (new EditorGUI.DisabledScope(_profile == null))
                    {
                        if (GUILayout.Button("淇濆瓨", EditorStyles.toolbarButton, GUILayout.Width(48)))
                        {
                            SaveAll();
                        }
                        if (GUILayout.Button("鏍￠獙", EditorStyles.toolbarButton, GUILayout.Width(48)))
                        {
                            ValidateAndReport();
                        }
                    }

                    GUILayout.FlexibleSpace();
                    if (_profile != null)
                    {
                        var states = _profile.StatePresentations == null ? 0 : _profile.StatePresentations.Count;
                        var actions = _profile.Actions == null ? 0 : _profile.Actions.Count;
                        GUILayout.Label($"鐘舵€?{states}  鎶€鑳?{actions}", EditorStyles.miniLabel);
                    }
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("棰勮瀵硅薄", EditorStyles.miniLabel, GUILayout.Width(60));
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
                        if (GUILayout.Button("鍚屾鍏ㄩ儴甯х巼", EditorStyles.toolbarButton, GUILayout.Width(96)))
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
                    GUILayout.Label("瑙掕壊鎴樻枟閰嶇疆", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "閫夋嫨 CharacterCombatProfile 寮€濮嬬紪杈戙€俓n\n" +
                        "鐘舵€佽〃鐜板彧鍋氱姸鎬佸埌 Timeline 鐨勬槧灏勶紱鎶€鑳藉垪琛ㄧ鐞嗕富鍔ㄥ姩浣滐紱" +
                        "鎶€鑳界殑閫昏緫杞ㄩ亾鍜岃〃鐜?Timeline 榛樿鍦ㄥ綋鍓嶇獥鍙ｅ唴缂栬緫锛屼篃鍙互鎸夐渶鎵撳紑鐙珛绐楀彛銆?,
                        MessageType.Info);
                    if (GUILayout.Button("鏂板缓瑙掕壊鎴樻枟閰嶇疆", GUILayout.Height(30)))
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
                GUILayout.Label("閰嶇疆", EditorStyles.boldLabel);
                DrawPageButton("鍩虹鍙傛暟", Page.Profile);
                DrawPageButton("鐘舵€佽〃鐜版槧灏?, Page.States);

                EditorGUILayout.Space(10);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("鎶€鑳藉垪琛?, EditorStyles.boldLabel);
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
                        EditorGUILayout.HelpBox("鏆傛棤鎶€鑳?, MessageType.Info);
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
                    "浠ｇ爜瑙﹀彂鎶€鑳斤紝Profile 鎻愪緵鏁版嵁锛屽弻婧愭椂闂磋酱鍦ㄥ綋鍓嶇獥鍙ｅ唴缂栬緫琛ㄧ幇銆?,
                    MessageType.None);
            }

        }

        private float GetMaxLeftPanelWidth()
        {
            return Mathf.Max(MinLeftPanelWidth, position.width - LeftPanelReserve);
        }

        /// <summary>
        /// 宸︽爮鎶€鑳借鐨勫彲鐢ㄥ搴︼細鐩存帴闂竷灞€瑕佷竴涓暣琛岀煩褰紙闆堕珮搴︺€佸彲鎷変几锛夈€?        /// 杩欐牱鎷垮埌鐨勫搴﹀氨鏄粴鍔ㄨ鍥惧唴瀹瑰尯鐨勭湡瀹炲搴︼紙宸叉墸闄ょ旱鍚戞粴鍔ㄦ潯锛夛紝
        /// 琛屽涓嶄細瓒呭嚭瑙嗗彛锛堜笉鍑虹幇妯悜婊氬姩鏉★級锛屼篃鑳介摵婊★紙鍙充晶涓嶇暀绌虹櫧锛夈€?        /// </summary>
        private float MeasureLeftRowWidth()
        {
            var probe = GUILayoutUtility
                .GetRect(0f, 0f, GUILayout.ExpandWidth(true))
                .width - 2f;

            if (probe <= 60f)
            {
                // 鍏滃簳锛氬竷灞€娌＄粰鍑哄彲鐢ㄥ搴︽椂鎸夐潰鏉垮搴︿及绠椼€?                probe = _leftPanelWidth
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
                EditorGUILayout.LabelField($"[{index}] 绌哄紩鐢?, EditorStyles.miniLabel);
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
            GUILayout.Label("鍩虹鍙傛暟", EditorStyles.boldLabel);

            var serialized = GetProfileSerialized();
            serialized.Update();
            Undo.RecordObject(_profile, "淇敼瑙掕壊鎴樻枟閰嶇疆");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProperty(serialized, "group", "瑙掕壊鍒嗙粍");
                DrawProperty(serialized, "frameRate", "閫昏緫甯х巼");
                DrawProperty(serialized, "moveSpeedPerSecond", "绉诲姩閫熷害 / 绉?);
                DrawProperty(serialized, "turnDegreesPerSecond", "杞悜閫熷害 / 绉?);
            }
            ApplyProfile(serialized);

            EditorGUILayout.Space(8);            

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("鍒涘缓鍩虹鏅敾绀轰緥", GUILayout.Width(170)))
                {
                    AddAction(true);
                }
                if (GUILayout.Button("鍒涘缓缂哄け鎶€鑳?Timeline", GUILayout.Width(170)))
                {
                    CreateMissingActionTimelines();
                }
                if (GUILayout.Button("鍒涘缓缂哄け鐘舵€?Timeline", GUILayout.Width(170)))
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
            Undo.RecordObject(_profile, "淇敼鐘舵€佽〃鐜伴厤缃?);
            var presentations = serialized.FindProperty("statePresentations");
            if (presentations == null)
            {
                EditorGUILayout.HelpBox("鎵句笉鍒?statePresentations 瀛楁銆?, MessageType.Error);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"宸查厤缃?{presentations.arraySize} 鏉℃槧灏?, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("娣诲姞榛樿鏄犲皠", GUILayout.Width(110)))
                {
                    AddNextDefaultPresentation();
                    return;
                }
                if (GUILayout.Button("琛ラ綈绌?Timeline", GUILayout.Width(110)))
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
                    "杩樻病鏈夌姸鎬佽〃鐜版槧灏勩€傚缓璁嚦灏戦厤缃?Locomotion/Idle 鍜?Locomotion/Move銆?,
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
                EditorGUILayout.HelpBox($"绗?{index + 1} 鏉＄姸鎬佹槧灏勫瓧娈典笉瀹屾暣銆?, MessageType.Error);
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
                    if (GUILayout.Button("鎵撳紑", GUILayout.Width(48)) && timelineProperty.objectReferenceValue != null)
                    {
                        OpenTimeline(timelineProperty.objectReferenceValue as TimelineAsset);
                    }
                    if (GUILayout.Button("绉婚櫎", GUILayout.Width(48)))
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
                        var nextLayer = EditorGUILayout.Popup("灞?, layerIndex, names);
                        if (nextLayer != layerIndex)
                        {
                            layer.enumValueIndex = (int)PresentationLayers[nextLayer];
                            stateId.intValue = CombatEditorUtility.GetDefinedStateIds(PresentationLayers[nextLayer])[0];
                            changed = true;
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(layer, new GUIContent("灞?));
                    }

                    ids = CombatEditorUtility.GetDefinedStateIds((StateLayer)layer.enumValueIndex);
                    var stateNames = new string[ids.Length];
                    for (var i = 0; i < ids.Length; i++)
                    {
                        stateNames[i] = CombatStateId.GetDisplayName((StateLayer)layer.enumValueIndex, ids[i]);
                    }
                    if (ids.Length > 0)
                    {
                        var nextState = EditorGUILayout.IntPopup("鐘舵€?, stateId.intValue, stateNames, ids);
                        if (nextState != stateId.intValue)
                        {
                            stateId.intValue = nextState;
                            changed = true;
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(stateId, new GUIContent("鐘舵€?ID"));
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
                        EditorGUILayout.PropertyField(priority, new GUIContent("浼樺厛绾?), GUILayout.Width(130));
                    }
                }

                if (displayName != null)
                {
                    EditorGUILayout.PropertyField(displayName, new GUIContent("鏄剧ず鍚嶇О"));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(timelineProperty, new GUIContent("Timeline"));
                    changed |= EditorGUI.EndChangeCheck();
                    var timeline = timelineProperty.objectReferenceValue as TimelineAsset;
                    if (GUILayout.Button(timeline == null ? "鏂板缓" : "缂栬緫", GUILayout.Width(52)))
                    {
                        if (timeline == null)
                        {
                            Undo.IncrementCurrentGroup();
                            var timelineUndoGroup = Undo.GetCurrentGroup();
                            Undo.SetCurrentGroupName("鍒涘缓鐘舵€?Timeline");
                            timeline = CombatEditorUtility.CreateTimelineAsset(
                                _profile,
                                CombatEditorUtility.GetStatePresentationTimelineSuffix(
                                    (StateLayer)layer.enumValueIndex,
                                    stateId.intValue,
                                    variant.stringValue),
                                true);
                            if (timeline != null)
                            {
                                Undo.RecordObject(_profile, "鍏宠仈鐘舵€?Timeline");
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
                        if (GUILayout.Button("瀹氫綅", GUILayout.Width(48)))
                        {
                            Selection.activeObject = timeline;
                            EditorGUIUtility.PingObject(timeline);
                        }
                        if (GUILayout.Button("鍚屾", GUILayout.Width(48)))
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
                    EditorGUILayout.HelpBox("鏈叧鑱?Timeline銆?, MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Timeline",
                        $"{currentTimeline.FrameRate} fps / {currentTimeline.DurationFrames} 甯?,
                        EditorStyles.miniLabel);
                }
            }
            return changed;
        }

        private void DrawSkillsPage()
        {
            if (!IsActionInProfile(_selectedAction))
            {
                EditorGUILayout.HelpBox("璇蜂粠宸︿晶鎶€鑳藉垪琛ㄩ€夋嫨鎶€鑳姐€?, MessageType.Info);
                return;
            }

            var action = _selectedAction;
            var timeline = GetActionTimeline(action);
            GUILayout.Label($"鎶€鑳斤細{GetActionName(action)}", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "鎶€鑳介€昏緫淇濆瓨鍦?CombatActionAsset锛涜〃鐜?Timeline 閫氳繃 Profile 鍏宠仈銆備笅鏂瑰弻婧愭椂闂磋酱鍏辩敤鍚屼竴甯ф爣灏恒€?,
                MessageType.Info);

            var serialized = GetActionSerialized();
            serialized.Update();
            Undo.RecordObject(action, "淇敼鎶€鑳介€昏緫閰嶇疆");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (DrawSection("閫昏緫鏁版嵁锛圕ombatActionAsset锛?, LogicSectionKey))
                {
                    EditorGUILayout.ObjectField("閫昏緫璧勪骇", action, typeof(CombatActionAsset), false);
                    DrawProperty(serialized, "stableId", "StableId");
                    DrawProperty(serialized, "actionId", "鎶€鑳?ID");
                    DrawProperty(serialized, "displayName", "鏄剧ず鍚嶇О");
                    DrawProperty(serialized, "durationFrames", "閫昏緫鎸佺画甯ф暟");
                    DrawProperty(serialized, "movementPolicy", "绉诲姩绛栫暐");
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
                if (DrawSection("瀹㈡埛绔〃鐜版槧灏勶紙CharacterCombatProfile锛?, PresentationSectionKey))
                {
                    timeline = DrawActionPresentation(action);
                    if (timeline == null)
                    {
                        EditorGUILayout.HelpBox("灏氭湭鍏宠仈鎶€鑳?Timeline銆?, MessageType.Warning);
                        if (GUILayout.Button("鍒涘缓鎶€鑳?Timeline", GUILayout.Width(150)))
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
                                "棣栦釜鍔ㄧ敾 Clip",
                                clip,
                                typeof(AnimationClip),
                                false);
                            if (nextClip != clip)
                            {
                                Undo.IncrementCurrentGroup();
                                var undoGroup = Undo.GetCurrentGroup();
                                Undo.SetCurrentGroupName("璁剧疆鎶€鑳藉姩鐢?);
                                Undo.RecordObject(timeline, "璁剧疆鎶€鑳藉姩鐢?);
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
                            $"{timeline.FrameRate} fps / {timeline.DurationFrames} 甯?,
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
                "琛ㄧ幇 Timeline",
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
                    $"閫昏緫鏃堕暱 {action.DurationFrames} 甯э紝琛ㄧ幇 Timeline {current.DurationFrames} 甯с€? +
                    "涓よ€呯嫭绔嬩繚瀛橈紝璇风‘璁ゅ樊寮傜鍚堣璁°€?,
                    MessageType.Warning);
            }
            return current;
        }

        /// <summary>
        /// 鍙虫爮鍒嗗尯鎶樺彔澶达細灞曞紑鐘舵€佹寜鍒嗗尯璁板湪 EditorPrefs锛岄伩鍏嶆瘡娆℃墦寮€閮借閲嶆柊灞曞紑銆?        /// </summary>
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
                if (DrawSection($"璇婃柇锛堥敊璇?{errors} / 璀﹀憡 {warnings}锛?, ValidationSectionKey))
                {
                    if (_issues.Count == 0)
                    {
                        EditorGUILayout.HelpBox("娌℃湁鍙戠幇闂銆?, MessageType.Info);
                    }
                    else
                    {
                        foreach (var issue in _issues)
                        {
                            var prefix = issue.Severity == CombatValidationSeverity.Error
                                ? "閿欒"
                                : issue.Severity == CombatValidationSeverity.Warning ? "璀﹀憡" : "鎻愮ず";
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
                "鍒涘缓瑙掕壊鎴樻枟閰嶇疆",
                "CharacterCombatProfile",
                "asset",
                "璇烽€夋嫨淇濆瓨浣嶇疆銆?,
                CombatEditorUtility.CombatRoot);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            path = CombatEditorUtility.NormalizeAssetPath(path);
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("鍒涘缓瑙掕壊鎴樻枟閰嶇疆");
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
            Undo.RegisterCreatedObjectUndo(persistedProfile ?? profile, "鍒涘缓瑙掕壊鎴樻枟閰嶇疆");
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
                EditorUtility.DisplayDialog("鏃犳硶鍒涘缓鎶€鑳?, "璇峰厛淇濆瓨 CharacterCombatProfile銆?, "纭畾");
                return;
            }

            if (basicAttack)
            {
                var existingAttack = _profile.FindAction(DefaultActionId);
                if (existingAttack != null)
                {
                    _selectedAction = existingAttack;
                    _page = Page.Skills;
                    ShowNotification(new GUIContent("褰撳墠 Profile 宸叉湁 Action 1001"));
                    return;
                }
            }

            var actionId = basicAttack ? DefaultActionId : GetNextActionId();
            var displayName = basicAttack ? "鏅€氭敾鍑? : $"鎶€鑳?{actionId}";
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
                EditorUtility.DisplayDialog("鏃犳硶鍒涘缓鎶€鑳?, error, "纭畾");
                return;
            }

            _profileSerialized = null;
            _selectedAction = action;
            _page = Page.Skills;
            _actionSerialized = null;
            _timelineDetachedToStandalone = false;
            Selection.activeObject = _selectedAction;
            ShowNotification(new GUIContent($"宸插垱寤猴細{displayName}锛圓ction {actionId}锛?));
        }

        private void AddNextDefaultPresentation()
        {
            if (_profile == null)
            {
                return;
            }

            // 鍊欓€夐泦鍚堟潵鑷灇涓惧弽灏勶紙CombatStateId.GetMappableStateIds锛夛紝鏂板鏋氫妇鐘舵€佷細鑷姩绾冲叆锛屾棤闇€鎵嬫敼銆?            foreach (var layer in PresentationLayers)
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
                    Undo.SetCurrentGroupName("娣诲姞鐘舵€佽〃鐜版槧灏?);
                    var serialized = GetProfileSerialized();
                    serialized.Update();
                    Undo.RecordObject(_profile, "娣诲姞鐘舵€佽〃鐜版槧灏?);
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
            ShowNotification(new GUIContent("榛樿鐘舵€佹槧灏勫凡缁忓叏閮ㄥ瓨鍦?));
        }

        private void CreateMissingStateTimelines()
        {
            if (_profile?.StatePresentations == null)
            {
                return;
            }
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("琛ラ綈鐘舵€?Timeline");
            var serialized = GetProfileSerialized();
            serialized.Update();
            Undo.RecordObject(_profile, "琛ラ綈鐘舵€?Timeline");
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
                Undo.RecordObject(_profile, "琛ラ綈鐘舵€?Timeline");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                _profile.ValidateData();
                EditorUtility.SetDirty(_profile);
                AssetDatabase.SaveAssets();
            }
            Undo.CollapseUndoOperations(undoGroup);
            ShowNotification(new GUIContent(created == 0 ? "娌℃湁闇€瑕佸垱寤虹殑鐘舵€?Timeline" : $"宸插垱寤?{created} 鏉＄姸鎬?Timeline"));
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
            Undo.SetCurrentGroupName("鍒涘缓鎶€鑳?Timeline");
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
            Undo.SetCurrentGroupName("琛ラ綈鎶€鑳?Timeline");
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
            ShowNotification(new GUIContent(created == 0 ? "娌℃湁闇€瑕佸垱寤虹殑鎶€鑳?Timeline" : $"宸插垱寤?{created} 鏉℃妧鑳?Timeline"));
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
            Undo.SetCurrentGroupName("鍚屾鍏ㄩ儴 Timeline 甯х巼");
            var changed = 0;
            foreach (var timeline in timelines)
            {
                if (timeline.FrameRate == _profile.FrameRate)
                {
                    continue;
                }
                Undo.RecordObject(timeline, "鍚屾鍏ㄩ儴 Timeline 甯х巼");
                if (timeline.SetFrameRate(_profile.FrameRate))
                {
                    timeline.ValidateData();
                    EditorUtility.SetDirty(timeline);
                    changed++;
                }
            }
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            ShowNotification(new GUIContent(changed == 0 ? "甯х巼宸茬粡涓€鑷? : $"宸插悓姝?{changed} 鏉?Timeline"));
        }

        private void SetTimelineFrameRate(TimelineAsset timeline, int frameRate)
        {
            if (timeline == null || timeline.FrameRate == frameRate)
            {
                return;
            }
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("鍚屾 Timeline 甯х巼");
            Undo.RecordObject(timeline, "鍚屾 Timeline 甯х巼");
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
            Undo.SetCurrentGroupName("淇濆瓨瑙掕壊鎴樻枟閰嶇疆");
            Undo.RecordObject(_profile, "淇濆瓨瑙掕壊鎴樻枟閰嶇疆");
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
                    Undo.RecordObject(action, "淇濆瓨瑙掕壊鎴樻枟閰嶇疆");
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
                        Undo.RecordObject(presentation.Timeline, "淇濆瓨瑙掕壊鎴樻枟閰嶇疆");
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
                        Undo.RecordObject(presentation.Timeline, "淇濆瓨瑙掕壊鎴樻枟閰嶇疆");
                        presentation.Timeline.ValidateData();
                        EditorUtility.SetDirty(presentation.Timeline);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            ShowNotification(new GUIContent("宸蹭繚瀛?));
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
                $"鏍￠獙瀹屾垚锛氶敊璇?{CountIssues(CombatValidationSeverity.Error)}锛岃鍛?{CountIssues(CombatValidationSeverity.Warning)}"));
        }

        private void RemoveAction(CombatActionAsset action)
        {
            if (_profile == null || action == null)
            {
                return;
            }
            if (!EditorUtility.DisplayDialog(
                    "绉婚櫎鎶€鑳?,
                    $"浠?{_profile.name} 鐨勬妧鑳藉垪琛ㄤ腑绉婚櫎鈥渰GetActionName(action)}鈥濓紵\n璧勬簮鏂囦欢涓嶄細琚垹闄ゃ€?,
                    "绉婚櫎",
                    "鍙栨秷"))
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
            Undo.SetCurrentGroupName("浠庤鑹茬Щ闄ゆ妧鑳?);
            var serialized = GetProfileSerialized();
            serialized.Update();
            Undo.RecordObject(_profile, "浠庤鑹茬Щ闄ゆ妧鑳?);
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
                return "绌烘妧鑳?;
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
