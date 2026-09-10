using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    /// <summary>
    /// 角色战斗内容入口。
    ///
    /// 这里仅负责三件事：角色 Profile 参数、宏观状态到 Timeline 的映射、技能资源列表。
    /// 技能的具体轨道和 Clip 仍由 TimelineWindow 编辑，状态切换规则由运行时代码负责。
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
        private const float SplitterWidth = 5f;
        private const string LeftPanelWidthKey = "Ux.CombatEditor.LeftPanelWidth";

        // 右栏分区折叠状态：按分区独立记忆，避免每次打开都要重新展开。
        private const string LogicSectionKey = "Ux.CombatEditor.Fold.Logic";
        private const string LogicWindowSectionKey = "Ux.CombatEditor.Fold.LogicWindow";
        private const string PresentationSectionKey = "Ux.CombatEditor.Fold.Presentation";
        private const string ValidationSectionKey = "Ux.CombatEditor.Fold.Validation";
        private static GUIStyle _sectionFoldoutStyle;

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
        private bool _resizingLeftPanel;

        [MenuItem("UxGame/工具/战斗/角色配置", false, 520)]
        public static void ShowWindow()
        {
            var window = GetWindow<CombatEditorWindow>();
            window.titleContent = new GUIContent("角色战斗配置");
            window.minSize = new Vector2(760, 500);
            window.Show();
        }

        public static CombatEditorWindow Open(
            CharacterCombatProfile profile,
            GameObject previewObject = null)
        {
            var window = GetWindow<CombatEditorWindow>();
            window.titleContent = new GUIContent("角色战斗配置");
            window.minSize = new Vector2(760, 500);
            window.SetProfile(profile);
            if (previewObject != null)
            {
                window._previewObject = previewObject;
            }
            window.Show();
            window.Repaint();
            return window;
        }

        public static CombatEditorWindow OpenAction(
            CharacterCombatProfile profile,
            CombatActionAsset action,
            GameObject previewObject = null)
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
            titleContent = new GUIContent("角色战斗配置");
            minSize = new Vector2(760, 500);
            _leftPanelWidth = EditorPrefs.GetFloat(LeftPanelWidthKey, DefaultLeftPanelWidth);
            var selected = Selection.activeObject as CharacterCombatProfile;
            if (selected != null)
            {
                SetProfile(selected);
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

        private void OnGUI()
        {
            DrawHeader();
            if (_profile == null)
            {
                DrawEmptyState();
                return;
            }

            if (!IsActionInProfile(_selectedAction))
            {
                _selectedAction = FindFirstAction();
            }

            _leftPanelWidth = Mathf.Clamp(
                _leftPanelWidth,
                MinLeftPanelWidth,
                GetMaxLeftPanelWidth());

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawNavigation();
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
                            case Page.Skills:
                                DrawSkillsPage();
                                break;
                        }
                    }
                }
            }
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
                    _previewObject = (GameObject)EditorGUILayout.ObjectField(
                        _previewObject,
                        typeof(GameObject),
                        false,
                        GUILayout.MinWidth(220));
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
                        "Timeline 的轨道和 Clip 继续在 TimelineWindow 中编辑。",
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
                    "代码触发技能，Profile 提供数据，TimelineWindow 编辑表现。",
                    MessageType.None);
            }

            // 分隔条不占布局宽度，贴在左栏右边界上，两栏之间不留空白带。
            DrawLeftPanelSplitter(GUILayoutUtility.GetLastRect());
        }

        /// <summary>
        /// 左栏与内容区之间的分隔条：拖动调整左栏宽度。
        /// 命中区跨在左栏右边界上，本身不参与布局，所以两栏之间不会出现空白带；
        /// 鼠标悬停或拖动时才画出高亮线，平时是不可见的拖动热区。
        /// </summary>
        private void DrawLeftPanelSplitter(Rect panelRect)
        {
            var edge = panelRect.xMax;
            var hot = new Rect(
                edge - SplitterWidth,
                panelRect.y,
                SplitterWidth * 2f,
                panelRect.height);
            EditorGUIUtility.AddCursorRect(hot, MouseCursor.ResizeHorizontal);

            var evt = Event.current;
            switch (evt.type)
            {
                case UnityEngine.EventType.MouseDown:
                    if (evt.button == 0 && hot.Contains(evt.mousePosition))
                    {
                        _resizingLeftPanel = true;
                        evt.Use();
                    }
                    break;
                case UnityEngine.EventType.MouseDrag:
                    if (_resizingLeftPanel)
                    {
                        _leftPanelWidth = Mathf.Clamp(
                            _leftPanelWidth + evt.delta.x,
                            MinLeftPanelWidth,
                            GetMaxLeftPanelWidth());
                        evt.Use();
                        Repaint();
                    }
                    break;
                case UnityEngine.EventType.MouseUp:
                    if (_resizingLeftPanel)
                    {
                        _resizingLeftPanel = false;
                        EditorPrefs.SetFloat(LeftPanelWidthKey, _leftPanelWidth);
                        evt.Use();
                    }
                    break;
            }

            if (_resizingLeftPanel || hot.Contains(evt.mousePosition))
            {
                EditorGUI.DrawRect(
                    new Rect(
                        edge - 1f,
                        panelRect.y + 2f,
                        2f,
                        Mathf.Max(0f, panelRect.height - 4f)),
                    new Color(0.32f, 0.52f, 0.82f, 0.7f));
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
            EditorPrefs.SetFloat(LeftPanelWidthKey, _leftPanelWidth);
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

            var label = $"{action.ActionId}  {GetActionName(action)}";
            if (GetActionTimeline(action) == null)
            {
                label += " *";
            }
            var oldColor = GUI.backgroundColor;
            if (_page == Page.Skills && _selectedAction == action)
            {
                GUI.backgroundColor = new Color(0.32f, 0.52f, 0.82f, 1f);
            }
            if (GUILayout.Button(
                    label,
                    EditorStyles.toolbarButton,
                    GUILayout.Height(25),
                    GUILayout.Width(rowWidth)))
            {
                _selectedAction = action;
                _page = Page.Skills;
                _actionSerialized = null;
            }
            GUI.backgroundColor = oldColor;

            var timeline = GetActionTimeline(action);
            // 固定宽度交给 GUIStyle 裁剪文本，既不留右侧空白，也不会撑出横向滚动条。
            EditorGUILayout.LabelField(
                $"逻辑：{action.name}",
                timeline == null ? "表现：未关联" : $"表现：{timeline.name}",
                EditorStyles.miniLabel,
                GUILayout.Width(rowWidth));
        }

        private void DrawProfilePage()
        {
            GUILayout.Label("基础参数", EditorStyles.boldLabel);

            var serialized = GetProfileSerialized();
            serialized.Update();
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

            if (serialized.ApplyModifiedProperties() || changed)
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
                            timeline = CombatEditorUtility.CreateTimelineAsset(
                                _profile,
                                CombatEditorUtility.GetStatePresentationTimelineSuffix(
                                    (StateLayer)layer.enumValueIndex,
                                    stateId.intValue,
                                    variant.stringValue),
                                true);
                            timelineProperty.objectReferenceValue = timeline;
                            changed = timeline != null;
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
                "技能逻辑保存在 CombatActionAsset；Timeline 通过 Profile 的独立表现映射关联，只负责客户端表现。",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开双源时间轴", GUILayout.Width(120)))
                {
                    OpenActionTimeline(action, timeline);
                }
                using (new EditorGUI.DisabledScope(timeline == null))
                {
                    if (GUILayout.Button("预览", GUILayout.Width(70)))
                    {
                        OpenActionTimeline(action, timeline, true);
                    }
                }
                if (GUILayout.Button("定位逻辑资产", GUILayout.Width(100)))
                {
                    Selection.activeObject = action;
                    EditorGUIUtility.PingObject(action);
                }
                using (new EditorGUI.DisabledScope(timeline == null))
                {
                    if (GUILayout.Button("定位表现资产", GUILayout.Width(100)))
                    {
                        Selection.activeObject = timeline;
                        EditorGUIUtility.PingObject(timeline);
                    }
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("从角色移除", GUILayout.Width(100)))
                {
                    RemoveSelectedAction();
                    return;
                }
            }

            var serialized = GetActionSerialized();
            serialized.Update();
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
            if (serialized.ApplyModifiedProperties())
            {
                action.ValidateData();
                EditorUtility.SetDirty(action);
                _issues.Clear();
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (DrawSection("逻辑窗口（在时间轴中编辑）", LogicWindowSectionKey))
                {
                    DrawLogicWindowSummary(action);
                }
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
                                Undo.RecordObject(timeline, "设置技能动画");
                                if (CombatEditorUtility.SetPrimaryAnimationClip(timeline, nextClip, out _))
                                {
                                    EditorUtility.SetDirty(timeline);
                                    AssetDatabase.SaveAssets();
                                }
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
            EditorGUILayout.ObjectField(
                "所属 Profile",
                _profile,
                typeof(CharacterCombatProfile),
                false);
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
        /// 逻辑窗口在本窗口只做只读摘要与跳转：权威编辑入口是时间轴上的逻辑轨道
        /// （CombatLogicTimelineSource），避免同一份数据出现第二个写入入口。
        /// 新增一类逻辑窗口只需扩展这里的摘要，不需要再往本窗口加表单。
        /// </summary>
        private void DrawLogicWindowSummary(CombatActionAsset action)
        {
            var cancelCount = action.CancelWindows?.Count ?? 0;
            var hitCount = action.HitWindows?.Count ?? 0;
            if (cancelCount == 0 && hitCount == 0)
            {
                EditorGUILayout.HelpBox(
                    "未配置逻辑窗口。在时间轴逻辑轨上新增区间 Clip 即可。",
                    MessageType.None);
            }

            for (var i = 0; i < cancelCount; i++)
            {
                var window = action.CancelWindows[i];
                if (window == null)
                {
                    continue;
                }
                EditorGUILayout.LabelField(
                    $"取消窗口 {i + 1}",
                    $"帧 {window.StartFrame}–{window.EndFrame} → 目标 {window.TargetActionId}"
                        + (window.RequiresHitConfirm ? "，需已命中" : "，无需命中"),
                    EditorStyles.miniLabel);
            }

            for (var i = 0; i < hitCount; i++)
            {
                var window = action.HitWindows[i];
                if (window == null)
                {
                    continue;
                }
                EditorGUILayout.LabelField(
                    $"命中窗口 {i + 1}",
                    $"帧 {window.StartFrame}–{window.EndFrame}，" +
                    $"{window.Shape} 半径 {window.RadiusMillimeters} mm",
                    EditorStyles.miniLabel);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("在时间轴中编辑", GUILayout.Width(140)))
                {
                    OpenActionTimeline(action, GetActionTimeline(action));
                }
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.LabelField(
                "区间增删与拖动只在时间轴逻辑轨进行，本窗口不提供第二套编辑入口。",
                EditorStyles.miniLabel);
        }

        /// <summary>
        /// 右栏分区折叠头：展开状态按分区记在 EditorPrefs，避免每次打开都要重新展开。
        /// </summary>
        private static bool DrawSection(string title, string foldKey, bool defaultExpanded = true)
        {
            var expanded = EditorPrefs.GetBool(foldKey, defaultExpanded);
            var next = EditorGUILayout.Foldout(expanded, title, true, SectionFoldoutStyle);
            if (next != expanded)
            {
                EditorPrefs.SetBool(foldKey, next);
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
            SetProfile(AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(path) ?? profile);
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

                    var serialized = GetProfileSerialized();
                    serialized.Update();
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
                    serialized.ApplyModifiedProperties();
                    _profile.ValidateData();
                    EditorUtility.SetDirty(_profile);
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
            var serialized = GetProfileSerialized();
            serialized.Update();
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
                serialized.ApplyModifiedProperties();
                _profile.ValidateData();
                EditorUtility.SetDirty(_profile);
                AssetDatabase.SaveAssets();
            }
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

            var timeline = CombatEditorUtility.CreateTimelineAsset(
                _profile,
                GetActionTimelineSuffix(action.ActionId),
                true);
            if (timeline == null)
            {
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
                return null;
            }

            _profileSerialized = null;
            AssetDatabase.SaveAssets();
            _issues.Clear();
            return timeline;
        }

        private void CreateMissingActionTimelines()
        {
            if (_profile?.Actions == null)
            {
                return;
            }
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
            var changed = 0;
            foreach (var timeline in timelines)
            {
                if (timeline.FrameRate == _profile.FrameRate)
                {
                    continue;
                }
                Undo.RecordObject(timeline, "同步 Timeline 帧率");
                if (timeline.SetFrameRate(_profile.FrameRate))
                {
                    timeline.ValidateData();
                    EditorUtility.SetDirty(timeline);
                    changed++;
                }
            }
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent(changed == 0 ? "帧率已经一致" : $"已同步 {changed} 条 Timeline"));
        }

        private void SetTimelineFrameRate(TimelineAsset timeline, int frameRate)
        {
            if (timeline == null || timeline.FrameRate == frameRate)
            {
                return;
            }
            Undo.RecordObject(timeline, "同步 Timeline 帧率");
            timeline.SetFrameRate(frameRate);
            timeline.ValidateData();
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
        }

        private void SaveAll()
        {
            if (_profile == null)
            {
                return;
            }
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
                        presentation.Timeline.ValidateData();
                        EditorUtility.SetDirty(presentation.Timeline);
                    }
                }
            }
            AssetDatabase.SaveAssets();
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

        private void RemoveSelectedAction()
        {
            if (_profile == null || _selectedAction == null)
            {
                return;
            }
            if (!EditorUtility.DisplayDialog(
                    "移除技能",
                    $"从 {_profile.name} 的技能列表中移除“{GetActionName(_selectedAction)}”？\n资源文件不会被删除。",
                    "移除",
                    "取消"))
            {
                return;
            }
            var serialized = GetProfileSerialized();
            serialized.Update();
            var actions = serialized.FindProperty("actions");
            var presentations = serialized.FindProperty("actionPresentations");
            if (presentations != null)
            {
                for (var i = presentations.arraySize - 1; i >= 0; i--)
                {
                    var presentation = presentations.GetArrayElementAtIndex(i);
                    if (presentation.FindPropertyRelative("action")?.objectReferenceValue == _selectedAction)
                    {
                        presentations.DeleteArrayElementAtIndex(i);
                    }
                }
            }
            for (var i = actions.arraySize - 1; i >= 0; i--)
            {
                if (actions.GetArrayElementAtIndex(i).objectReferenceValue == _selectedAction)
                {
                    actions.DeleteArrayElementAtIndex(i);
                }
            }
            serialized.ApplyModifiedProperties();
            _profile.ValidateData();
            EditorUtility.SetDirty(_profile);
            AssetDatabase.SaveAssets();
            _selectedAction = FindFirstAction();
            _actionSerialized = null;
        }

        private void OpenTimeline(TimelineAsset timeline, bool autoPlay = false)
        {
            if (timeline != null)
            {
                TimelineWindow.Open(timeline, _previewObject, autoPlay);
            }
        }

        private void OpenActionTimeline(
            CombatActionAsset action,
            TimelineAsset timeline,
            bool autoPlay = false)
        {
            if (action != null)
            {
                TimelineWindow.Open(action, timeline, _profile, _previewObject, autoPlay);
            }
        }

        private void SetProfile(CharacterCombatProfile profile)
        {
            _profile = profile;
            _profileSerialized = null;
            _actionSerialized = null;
            _selectedAction = FindFirstAction();
            _page = Page.Profile;
            _issues.Clear();
            _showIssues = false;
            _contentScroll = Vector2.zero;
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
            if (serialized.ApplyModifiedProperties())
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
