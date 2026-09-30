using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Ux.Editor.Combat;
namespace Ux.Editor.Timeline
{
    struct BindData
    {
        public int index;
        public Type type;
        public BindData(int i, Type t)
        {
            index = i;
            type = t;
        }
    }
    public class TLEntity : Entity
    {
        protected override void OnDestroy()
        {
            var viewer = Viewer;
            base.OnDestroy();
            if (viewer != null && viewer.gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(viewer.gameObject);
            }
        }
    }
    [InitializeOnLoad]
    public partial class TimelineWindow : EditorWindow
    {
        private const string PreviewObjectSuffix = " (Timeline Preview)";
        private const HideFlags PreviewObjectHideFlags =
            HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        static TimelineWindow()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CleanupAllPreviewObjects;
            EditorApplication.quitting += CleanupAllPreviewObjects;
        }

        public enum PlayMode
        {
            Loop,
            Once,
        }

        private enum PreviewBaseMode
        {
            ActionOnly,
            Idle,
            Move,
        }
        public static TimelineWindow wnd;
        private TimelineAsset _requestedTimeline;
        private CombatActionAsset _requestedCombatAction;
        private CharacterCombatProfile _requestedCombatProfile;
        private GameObject _requestedPreviewObject;
        private bool _requestedAutoPlay;
        private bool _autoPlayPending;
        private bool _hasOpenRequest;
        private bool _isEmbedded;
        private VisualElement _sourcePanel;
        private Button _standaloneButton;
        private PopupField<PreviewBaseMode> _previewBasePopup;
        private PreviewBaseMode _previewBaseMode = PreviewBaseMode.Idle;
        private CombatTimelinePlayer _previewPlayer;

        public static event Action StandaloneWindowOpened;
        public static event Action StandaloneWindowClosed;

        public static void CloseStandalone()
        {
            if (wnd != null && !wnd._isEmbedded)
            {
                wnd.Close();
            }
        }

        public static void RefreshCurrentDocumentAfterUndo()
        {
            if (wnd == null || TimelineWindow.Document == null)
            {
                return;
            }

            foreach (var source in TimelineWindow.Document.Sources)
            {
                source.RefreshAfterUndo();
            }
            InspectorContent?.FreshInspector(null, null);
            RefreshView?.Invoke();
            RefreshClip?.Invoke();
            wnd.UpdateDurationLabel();
        }

        /// <summary>
        /// 从其它编辑器打开指定 Timeline，并尽可能复用传入的预览对象。
        /// 资源和对象通过待处理请求传递，兼容 EditorWindow 首次创建时 CreateGUI 尚未执行的时序。
        /// </summary>
        public static TimelineWindow Open(TimelineAsset timeline, GameObject previewObject = null, bool autoPlay = false)
        {
            return OpenInternal(timeline, null, null, previewObject, autoPlay);
        }

        /// <summary>
        /// 创建一个不显示独立窗口的 Timeline 面板，并把面板挂载到调用方容器中。
        /// 角色战斗配置窗口使用该入口，独立打开 Timeline 仍使用 Open。
        /// </summary>
        public static TimelineWindow CreateEmbedded(VisualElement container, CombatActionAsset action, TimelineAsset timeline, CharacterCombatProfile profile, GameObject previewObject = null, bool autoPlay = false)
        {
            if (container == null || (timeline == null && action == null))
            {
                return null;
            }

            if (wnd != null)
            {
                UnityEngine.Object.DestroyImmediate(wnd);
                wnd = null;
            }

            var host = CreateInstance<TimelineWindow>();
            host.hideFlags = HideFlags.HideAndDontSave;
            host.titleContent = new GUIContent("双源时间轴");
            host._isEmbedded = true;
            host._requestedTimeline = timeline;
            host._requestedCombatAction = action;
            host._requestedCombatProfile = profile;
            host._requestedPreviewObject = previewObject;
            host._requestedAutoPlay = autoPlay && timeline != null;
            host._hasOpenRequest = true;
            host.CreateGUI();

            if (host.root.parent != null)
            {
                host.root.parent.Remove(host.root);
            }
            container.Add(host.root);
            return host;
        }

        /// <summary>
        /// 同时打开技能的客户端表现 Timeline 与确定性逻辑轨道。两类数据共用帧标尺，
        /// 但分别以 TimelineAsset 和 CombatActionAsset 作为 Undo/Save owner。
        /// </summary>
        public static TimelineWindow Open(CombatActionAsset action, TimelineAsset timeline, CharacterCombatProfile profile = null, GameObject previewObject = null, bool autoPlay = false)
        {
            return OpenInternal(timeline, action, profile, previewObject, autoPlay);
        }

        static TimelineWindow OpenInternal(TimelineAsset timeline, CombatActionAsset action, CharacterCombatProfile profile, GameObject previewObject, bool autoPlay)
        {
            if (wnd != null && wnd._isEmbedded)
            {
                UnityEngine.Object.DestroyImmediate(wnd);
                wnd = null;
            }
            if (timeline == null && action == null)
            {
                return null;
            }
            if (action != null && timeline != null && profile != null &&
                timeline.FrameRate != profile.FrameRate)
            {
                Debug.LogError(
                    $"无法打开技能双源时间轴：表现帧率 {timeline.FrameRate} 与 Profile 逻辑帧率 {profile.FrameRate} 不一致。",
                    timeline);
                return null;
            }

            wnd = GetWindow<TimelineWindow>();
            wnd.titleContent = new GUIContent("时间轴");
            wnd.CancelAutoPlay();
            if (IsPlaying)
            {
                wnd._OnBtnPauseClick();
            }
            wnd._requestedTimeline = timeline;
            wnd._requestedCombatAction = action;
            wnd._requestedCombatProfile = profile;
            wnd._requestedPreviewObject = previewObject;
            wnd._requestedAutoPlay = autoPlay && timeline != null;
            wnd._hasOpenRequest = true;
            wnd.Show();

            if (wnd.ofTimeline != null)
            {
                wnd.ApplyOpenRequest();
            }

            wnd.Focus();
            wnd.Repaint();
            StandaloneWindowOpened?.Invoke();
            return wnd;
        }

        static string Path = "Assets/Data/Res/Timeline";
        bool isCreateing = false;
        double _lastTime;
        float _playTime;
        TLEntity _entity;
        GameObject _previewInstance;
        List<int> _frameSelects = new List<int>() { 24, 30, 60, 120 };
        Dictionary<string, Dictionary<string, BindData>> _binds = new();
        PopupField<int> _framePopupField;
        PopupField<PlayMode> _playModePopup;
        IntegerField _currentFrameField;
        Label _durationLabel;
        ObjectField _combatActionField;
        CombatActionAsset _combatAction;
        CharacterCombatProfile _combatProfile;
        public void CreateGUI()
        {
            // UXML 或脚本热重载后 EditorWindow 可能保留旧视觉树，必须先清理再重建。
            EditorApplication.delayCall -= TryAutoPlay;
            _autoPlayPending = false;
            rootVisualElement.Clear();
            EditorApplication.update -= OnPlay;
            Undo?.Dispose();
            DestroyPreviewEntity();
            Timeline = null;
            _previewPlayer = null;
            Asset = null;
            _combatAction = null;
            _combatProfile = null;
            Document = new TimelineEditorDocument(() => IsPlaying);
            Document.SourceStructureChanged += OnDocumentStructureChanged;
            Document.SourceChanged += OnDocumentChanged;
            InspectorContent = null;
            ClipContent = null;
            GetPositionByFrame = null;
            GetFrameByMousePosition = null;
            RefreshView = null;
            RefreshClip = null;
            Undo = new UxUndo();
            isCreateing = true;
            IsPlaying = false;
            SaveAssets = _SaveAssets;
            RefreshEntity = _RefreshEntity;
            RefreshBinds = _RefreshBinds;
            MarkerMove = _MarkerMove;

            wnd = this;
            BuildEditorUI();
            rootVisualElement.Add(root);
            clipView.Init();
            clipView.FrameChanged += OnEditorFrameChanged;
            trackView.VerticalScrollChanged += clipView.SetVerticalScroll;
            clipView.VerticalScrollChanged += trackView.SetVerticalScroll;

            ofEntity.objectType = typeof(GameObject);
            ofTimeline.objectType = typeof(TimelineAsset);

            _framePopupField = new PopupField<int>(_frameSelects, 2);
            _framePopupField.label = "帧率";
            _framePopupField.RegisterValueChangedCallback(evt =>
            {
                if (Document?.SetFrameRate(evt.newValue) == true)
                {
                    RefreshView?.Invoke();
                    RefreshClip?.Invoke();
                    RefreshEntity?.Invoke();
                    UpdateDurationLabel();
                }
            });
            _framePopupField.SetValueWithoutNotify(TimelineAsset.DefaultFrameRate);
            CenterToolbarField(_framePopupField, _framePopupField.labelElement, 26);
            _framePopupField.labelElement.style.minWidth = 32;
            _framePopupField.style.width = 112;
            _framePopupField.style.minWidth = 112;
            _framePopupField.style.flexShrink = 0;
            frameContent.Add(_framePopupField);

            createView.style.display = DisplayStyle.None;
            _sourcePanel.style.display = DisplayStyle.None;
            UpdatePreviewBaseSelector();

            inputPath.SetValueWithoutNotify(Path);

            if (!_isEmbedded)
            {
                _OnOfEntityChanged(ChangeEvent<UnityEngine.Object>.GetPooled(null, SettingTools.GetPlayerPrefs<GameObject>("timeline_entity")));
                _OnOfTimelineChanged(ChangeEvent<UnityEngine.Object>.GetPooled(null, SettingTools.GetPlayerPrefs<TimelineAsset>("timeline_asset")));
            }
            ApplyOpenRequest();
            UpdateStandaloneButton();
            UpdatePlayButton();
            _OnBindObjs();
            RefreshEntity();
            RefreshView();
            clipView.ResetView();
            isCreateing = false;
            UpdateDurationLabel();
        }

        void ApplyOpenRequest()
        {
            if (!_hasOpenRequest)
            {
                return;
            }

            var requestedPreviewObject = _requestedPreviewObject;
            var previousPreviewObject = ofEntity?.value;
            var previousTimeline = Asset;
            var requestedTimeline = _requestedTimeline;
            var requestedCombatAction = _requestedCombatAction;
            var requestedCombatProfile = _requestedCombatProfile;
            var requestedAutoPlay = _requestedAutoPlay;
            CancelAutoPlay();
            _requestedPreviewObject = null;
            _requestedTimeline = null;
            _requestedCombatAction = null;
            _requestedCombatProfile = null;
            _requestedAutoPlay = false;
            _hasOpenRequest = false;

            if (requestedPreviewObject != null && ofEntity != null)
            {
                previousPreviewObject = ofEntity.value as GameObject;
                ofEntity.SetValueWithoutNotify(requestedPreviewObject);
                if (_previewInstance == null || previousPreviewObject != requestedPreviewObject)
                {
                    _OnOfEntityChanged(ChangeEvent<UnityEngine.Object>.GetPooled(
                        previousPreviewObject,
                        requestedPreviewObject));
                }
            }

            InspectorContent?.Clear();
            _combatProfile = requestedCombatProfile;
            Asset = requestedTimeline;
            _combatAction = requestedCombatAction;
            UpdatePreviewBaseSelector();
            ofTimeline?.SetValueWithoutNotify(requestedTimeline);
            _combatActionField?.SetValueWithoutNotify(requestedCombatAction);
            ConfigureDocumentSources();
            var presentationChanged = !ReferenceEquals(previousTimeline, Asset) ||
                requestedPreviewObject != null &&
                !ReferenceEquals(previousPreviewObject, requestedPreviewObject);
            if (presentationChanged)
            {
                ApplyPresentationAssetSelection();
            }
            RefreshView?.Invoke();
            if (!ReferenceEquals(previousTimeline, Asset))
            {
                clipView?.ResetView();
            }
            else
            {
                clipView?.RefreshLayout();
            }
            UpdateDurationLabel();
            UpdateStandaloneButton();

            if (requestedAutoPlay)
            {
                ScheduleAutoPlay();
            }
        }

        private void ScheduleAutoPlay()
        {
            CancelAutoPlay();
            _autoPlayPending = true;
            EditorApplication.delayCall += TryAutoPlay;
        }

        private void CancelAutoPlay()
        {
            _autoPlayPending = false;
            EditorApplication.delayCall -= TryAutoPlay;
        }

        private void TryAutoPlay()
        {
            if (!_autoPlayPending)
            {
                return;
            }

            _autoPlayPending = false;
            if (this == null || !IsValid())
            {
                return;
            }

            _OnBtnPlayClick();
        }

        void BuildEditorUI()
        {
            root = new VisualElement { name = "TimelineRoot" };
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = new Color(0.105f, 0.105f, 0.105f);
            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(OnShortcutKeyDown);

            _sourcePanel = new VisualElement();
            var sourcePanel = _sourcePanel;
            sourcePanel.style.flexShrink = 0;
            sourcePanel.style.paddingLeft = 8;
            sourcePanel.style.paddingRight = 8;
            sourcePanel.style.paddingTop = 6;
            sourcePanel.style.paddingBottom = 4;
            sourcePanel.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            root.Add(sourcePanel);

            var entityRow = CreateToolbarRow();
            ofEntity = new ObjectField("预览对象") { allowSceneObjects = false };
            ofEntity.style.flexGrow = 1;
            ofEntity.RegisterValueChangedCallback(_OnOfEntityChanged);
            entityRow.Add(ofEntity);
            sourcePanel.Add(entityRow);

            var assetRow = CreateToolbarRow();
            ofTimeline = new ObjectField("Timeline") { allowSceneObjects = false };
            ofTimeline.style.flexGrow = 1;
            ofTimeline.RegisterValueChangedCallback(_OnOfTimelineChanged);
            assetRow.Add(ofTimeline);
            btnCreate = new Button(_OnBtnCreateClick) { text = "新建资源" };
            btnCreate.style.width = 80;
            assetRow.Add(btnCreate);
            sourcePanel.Add(assetRow);

            var logicRow = CreateToolbarRow();
            _combatActionField = new ObjectField("逻辑技能")
            {
                objectType = typeof(CombatActionAsset),
                allowSceneObjects = false,
            };
            _combatActionField.style.flexGrow = 1;
            _combatActionField.RegisterValueChangedCallback(OnCombatActionChanged);
            logicRow.Add(_combatActionField);
            sourcePanel.Add(logicRow);

            _previewBasePopup = new PopupField<PreviewBaseMode>(
                new List<PreviewBaseMode>
                {
                    PreviewBaseMode.ActionOnly,
                    PreviewBaseMode.Idle,
                    PreviewBaseMode.Move,
                },
                PreviewBaseMode.Idle);
            _previewBasePopup.label = "预览基底";
            _previewBasePopup.formatSelectedValueCallback = GetPreviewBaseText;
            _previewBasePopup.formatListItemCallback = GetPreviewBaseText;
            _previewBasePopup.style.width = 130;
            _previewBasePopup.style.minWidth = 130;
            _previewBasePopup.style.flexShrink = 0;
            _previewBasePopup.RegisterValueChangedCallback(evt =>
            {
                _previewBaseMode = evt.newValue;
                RefreshEntity?.Invoke();
                RefreshView?.Invoke();
            });

            createView = new VisualElement();
            createView.style.flexDirection = FlexDirection.Row;
            createView.style.flexShrink = 0;
            createView.style.paddingLeft = 8;
            createView.style.paddingRight = 8;
            createView.style.paddingTop = 4;
            createView.style.paddingBottom = 4;
            createView.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f);
            inputPath = new TextField("保存目录") { isReadOnly = true };
            inputPath.style.flexGrow = 1;
            btnPath = new Button(SelectCreatePath) { text = "选择" };
            inputName = new TextField("资源名");
            inputName.style.width = 220;
            btnOk = new Button(_OnBtnOkClick) { text = "创建" };
            createView.Add(inputPath);
            createView.Add(btnPath);
            createView.Add(inputName);
            createView.Add(btnOk);
            root.Add(createView);

            var playback = new Toolbar();
            playback.style.height = 34;
            playback.style.minHeight = 34;
            playback.style.flexShrink = 0;
            playback.style.alignItems = Align.Center;
            btnLastFrame = CreatePlaybackButton("上一帧", _OnBtnLastFrameClick, 68);
            btnNextFrame = CreatePlaybackButton("下一帧", _OnBtnNextFrameClick, 68);
            btnPlay = CreatePlaybackButton("播放", OnPlayPauseButtonClick, 56);
            playback.Add(btnLastFrame);
            playback.Add(btnNextFrame);
            playback.Add(btnPlay);

            _currentFrameField = new IntegerField("当前帧");
            _currentFrameField.style.width = 145;
            _currentFrameField.style.minWidth = 145;
            _currentFrameField.style.flexShrink = 0;
            CenterToolbarField(_currentFrameField, _currentFrameField.labelElement, 26);
            _currentFrameField.labelElement.style.minWidth = 48;
            _currentFrameField.RegisterValueChangedCallback(evt =>
            {
                if (!IsPlaying && IsValid())
                {
                    clipView.SetNowFrame(Mathf.Max(0, evt.newValue));
                }
            });
            playback.Add(_currentFrameField);

            _playModePopup = new PopupField<PlayMode>(
                new List<PlayMode> { PlayMode.Once, PlayMode.Loop }, 0);
            _playModePopup.label = "模式";
            _playModePopup.formatSelectedValueCallback = GetPlayModeText;
            _playModePopup.formatListItemCallback = GetPlayModeText;
            _playModePopup.style.width = 125;
            _playModePopup.style.minWidth = 125;
            _playModePopup.style.flexShrink = 0;
            CenterToolbarField(_playModePopup, _playModePopup.labelElement, 26);
            playback.Add(_playModePopup);
            playback.Add(_previewBasePopup);
            frameContent = new VisualElement();
            frameContent.style.flexDirection = FlexDirection.Row;
            frameContent.style.flexShrink = 0;
            playback.Add(frameContent);
            _durationLabel = new Label();
            _durationLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _durationLabel.style.marginLeft = 8;
            playback.Add(_durationLabel);
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            playback.Add(spacer);
            var saveButton = new Button(_SaveAssets) { text = "保存" };
            saveButton.tooltip = "分别保存当前表现与逻辑资源 (Ctrl+S)";
            playback.Add(saveButton);
            _standaloneButton = new Button(OpenStandaloneWindow)
            {
                text = "独立窗口",
                tooltip = "在独立窗口中编辑当前双源 Timeline",
            };
            _standaloneButton.style.width = 76;
            _standaloneButton.style.minWidth = 76;
            _standaloneButton.style.display = _isEmbedded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            playback.Add(_standaloneButton);
            root.Add(playback);

            var mainSplit = new TwoPaneSplitView(
                0, 280, TwoPaneSplitViewOrientation.Horizontal);
            mainSplit.style.flexGrow = 1;
            mainSplit.style.minHeight = 180;
            trackView = new TimelineTrackView();
            clipView = new TimelineClipView();
            mainSplit.Add(trackView);
            mainSplit.Add(clipView);
            root.Add(mainSplit);

            VisualElement = root;
        }

        static void CenterToolbarField(VisualElement field, Label label, float height)
        {
            field.style.height = height;
            field.style.minHeight = height;
            field.style.maxHeight = height;
            field.style.alignSelf = Align.Center;
            field.style.marginTop = 0;
            field.style.marginBottom = 0;
            if (label != null)
            {
                label.style.height = height;
                label.style.minHeight = height;
                label.style.alignSelf = Align.Center;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.marginTop = 0;
                label.style.marginBottom = 0;
            }

            var input = field.Q<VisualElement>(className: "unity-base-field__input");
            if (input != null)
            {
                input.style.height = height;
                input.style.minHeight = height;
                input.style.alignItems = Align.Center;
                input.style.marginTop = 0;
                input.style.marginBottom = 0;
            }
        }

        static Button CreatePlaybackButton(string text, Action callback, float width)
        {
            var button = new Button(callback) { text = text, tooltip = text };
            button.style.width = width;
            button.style.minWidth = width;
            return button;
        }

        static string GetPlayModeText(PlayMode mode)
        {
            return mode == PlayMode.Loop ? "循环" : "单次";
        }

        private void OnPlayPauseButtonClick()
        {
            if (IsPlaying)
            {
                _OnBtnPauseClick();
            }
            else
            {
                _OnBtnPlayClick();
            }
        }

        private void UpdatePlayButton()
        {
            if (btnPlay == null)
            {
                return;
            }

            btnPlay.text = IsPlaying ? "暂停" : "播放";
            btnPlay.SetEnabled(Asset != null && Timeline != null);
        }

        void OnShortcutKeyDown(KeyDownEvent evt)
        {
            if (evt.ctrlKey && evt.keyCode == KeyCode.S)
            {
                _SaveAssets();
                evt.StopPropagation();
                return;
            }
            if (evt.keyCode == KeyCode.Space)
            {
                if (IsPlaying) _OnBtnPauseClick();
                else _OnBtnPlayClick();
                evt.StopPropagation();
                return;
            }
            if (!IsPlaying && evt.keyCode == KeyCode.LeftArrow)
            {
                _OnBtnLastFrameClick();
                evt.StopPropagation();
            }
            else if (!IsPlaying && evt.keyCode == KeyCode.RightArrow)
            {
                _OnBtnNextFrameClick();
                evt.StopPropagation();
            }
        }

        static VisualElement CreateToolbarRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.height = 30;
            row.style.flexShrink = 0;
            return row;
        }

        void OnEditorFrameChanged(int frame)
        {
            _currentFrameField?.SetValueWithoutNotify(frame);
        }

        void UpdateDurationLabel()
        {
            if (_durationLabel == null)
            {
                return;
            }
            var duration = Document?.DurationFrames ?? 0;
            _durationLabel.text = Document?.HasSource != true
                ? "未选择时间轴数据"
                : $"会话长度 {duration} 帧 / {duration / (float)Document.FrameRate:0.###} 秒";
        }

        bool keyCtrl = false;
        bool keyS = false;
        bool save = false;
        private void OnGUI()
        {
            switch (Event.current.type)
            {
                case UnityEngine.EventType.KeyDown:
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.LeftControl:
                            keyCtrl = true;
                            break;
                        case KeyCode.S:
                            keyS = true;
                            break;
                        case KeyCode.Delete:

                            break;
                    }
                    break;
                case UnityEngine.EventType.KeyUp:
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.LeftControl:
                            keyCtrl = false;
                            save = false;
                            break;
                        case KeyCode.S:
                            keyS = false;
                            save = false;
                            break;
    
                    }
                    break;                    
            }

            if (keyCtrl && keyS && !save)
            {
                Log.Debug("保存");
                save = true;
                SaveAssets();
            }
        }

        private void UpdateStandaloneButton()
        {
            if (_standaloneButton == null)
            {
                return;
            }

            _standaloneButton.style.display = _isEmbedded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _standaloneButton.SetEnabled(_isEmbedded && Asset != null);
        }

        private void OpenStandaloneWindow()
        {
            if (!_isEmbedded || Asset == null)
            {
                return;
            }

            var previewObject = ofEntity?.value as GameObject;
            var autoPlay = IsPlaying;
            if (_combatAction != null)
            {
                TimelineWindow.Open(
                    _combatAction,
                    Asset,
                    _combatProfile,
                    previewObject,
                    autoPlay);
            }
            else
            {
                TimelineWindow.Open(Asset, previewObject, autoPlay);
            }
        }

        void OnPlay()
        {
            if (IsPlaying)
            {
                if (Asset == null || Timeline == null)
                {
                    _OnBtnPauseClick();
                    return;
                }
                var deltaTime = (float)(EditorApplication.timeSinceStartup - _lastTime);
                _lastTime = EditorApplication.timeSinceStartup;
                _playTime += deltaTime;
                var frame = Asset.TimeToFrame(_playTime);
                clipView.SetNowFrame(frame);
                var duration = Document?.DurationFrames ?? Asset.DurationFrames;
                if (frame >= duration)
                {
                    switch (_playModePopup?.value ?? PlayMode.Once)
                    {
                        case PlayMode.Once:
                            _OnBtnPauseClick();
                            break;
                        case PlayMode.Loop:
                            _ResetPlay();
                            break;
                    }
                }
            }
        }
        void _ResetPlay()
        {
            _playTime = 0;
            _lastTime = EditorApplication.timeSinceStartup;
            clipView.SetNowFrame(0, true);
        }

        private void OnDestroy()
        {
            EditorApplication.delayCall -= TryAutoPlay;
            _autoPlayPending = false;
            if (IsPlaying)
            {
                IsPlaying = false;
                UnityEditor.EditorApplication.update -= OnPlay;
            }
            DestroyPreviewEntity();
            Undo?.Dispose();
            Undo = null;
            Timeline = null;
            Asset = null;
            _combatAction = null;
            _combatProfile = null;
            Document = null;
            InspectorContent = null;
            ClipContent = null;
            GetPositionByFrame = null;
            GetFrameByMousePosition = null;
            MarkerMove = null;
            SaveAssets = null;
            RefreshBinds = null;
            RefreshEntity = null;
            RefreshView = null;
            RefreshClip = null;
            if (wnd == this)
            {
                wnd = null;
            }
            UpdateStandaloneButton();
            if (!_isEmbedded)
            {
                StandaloneWindowClosed?.Invoke();
            }
        }
        partial void _OnBtnLastFrameClick()
        {
            if (!IsValid()) return;
            if (clipView.CurFrame > 0)
            {
                clipView.SetNowFrame(clipView.CurFrame - 1);
            }
        }
        partial void _OnBtnNextFrameClick()
        {
            if (!IsValid()) return;
            clipView.SetNowFrame(clipView.CurFrame + 1);
        }

        partial void _OnBtnPlayClick()
        {
            if (!IsValid() || Asset == null || Timeline == null) return;
            _OnBindObjs();
            RefreshEntity?.Invoke();
            _ResetPlay();
            UnityEditor.EditorApplication.update -= OnPlay;
            UnityEditor.EditorApplication.update += OnPlay;
            IsPlaying = true;
            UpdatePlayButton();
            _framePopupField?.SetEnabled(false);
        }
        partial void _OnBtnPauseClick()
        {
            if (!IsPlaying) return;
            UnityEditor.EditorApplication.update -= OnPlay;
            IsPlaying = false;
            UpdatePlayButton();
            _framePopupField?.SetEnabled(Asset != null && _combatProfile == null);
        }

        partial void _OnOfEntityChanged(ChangeEvent<UnityEngine.Object> e)
        {
            DestroyPreviewEntity();
            ofEntity.SetValueWithoutNotify(e.newValue);
            if (e.newValue is GameObject obj)
            {
                if (obj == null)
                {
                    return;
                }

                var model = Instantiate(obj);
                model.name = $"{obj.name}{PreviewObjectSuffix}";
                model.hideFlags = PreviewObjectHideFlags;
                _previewInstance = model;

                // 编辑器预览不依赖全局对象池，避免内嵌宿主在编辑器初始化阶段拿不到 Entity。
                _entity = Entity.Create<TLEntity>(false);
                if (_entity == null)
                {
                    DestroyPreviewEntity();
                    return;
                }

                _entity.Link(model);
                Timeline = _entity.Add<TimelineComponent>(false);
                if (Timeline == null)
                {
                    DestroyPreviewEntity();
                    return;
                }

                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _))
                {
                    SettingTools.SavePlayerPrefs("timeline_entity", guid);
                }
                if (!isCreateing)
                {
                    _OnBindObjs();
                    RefreshEntity();
                }
            }
        }
        partial void _OnOfTimelineChanged(ChangeEvent<UnityEngine.Object> e)
        {
            var requestedAsset = e.newValue as TimelineAsset;
            if (_combatProfile != null && requestedAsset != null &&
                requestedAsset.FrameRate != _combatProfile.FrameRate)
            {
                ofTimeline.SetValueWithoutNotify(Asset);
                Debug.LogError(
                    $"无法切换表现 Timeline：帧率 {requestedAsset.FrameRate} 与 Profile 逻辑帧率 {_combatProfile.FrameRate} 不一致。",
                    requestedAsset);
                return;
            }
            ofTimeline.SetValueWithoutNotify(e.newValue);
            Asset = requestedAsset;
            UpdatePreviewBaseSelector();
            ConfigureDocumentSources();
            ApplyPresentationAssetSelection();
            InspectorContent?.FreshInspector(null, null);
            if (!isCreateing)
            {
                RefreshView?.Invoke();
                clipView?.ResetView();
            }
            UpdateDurationLabel();
            UpdateStandaloneButton();
        }

        void OnCombatActionChanged(ChangeEvent<UnityEngine.Object> e)
        {
            _combatActionField.SetValueWithoutNotify(e.newValue);
            _combatAction = e.newValue as CombatActionAsset;
            UpdatePreviewBaseSelector();
            if (_combatProfile != null &&
                (_combatAction == null || _combatProfile.FindAction(_combatAction.ActionId) != _combatAction))
            {
                _combatProfile = null;
            }
            ConfigureDocumentSources();
            InspectorContent?.FreshInspector(null, null);
            RefreshView?.Invoke();
            clipView?.RefreshLayout();
            UpdateDurationLabel();
            UpdateStandaloneButton();
        }

        void ConfigureDocumentSources()
        {
            Undo?.CompleteUndo();
            var sources = new List<ITimelineEditorSource>();
            if (Asset != null)
            {
                sources.Add(new TimelineAssetEditorSource(
                    Asset,
                    (key, owner, _) => Undo?.RegUndo(key, owner, () => OnTimelineUndoRedo(owner)),
                    canEdit: () => !IsPlaying,
                    completeUndo: () => Undo?.CompleteUndo(),
                    beforeSave: SyncCombatActionDurationBeforeSave));
            }
            if (_combatAction != null)
            {
                sources.Add(new CombatLogicTimelineSource(
                    _combatAction,
                    _combatProfile,
                    Asset?.FrameRate ?? _combatProfile?.FrameRate ?? TimelineEditorDocument.DefaultFrameRate,
                    (key, owner, _) => Undo?.RegUndo(key, owner, () => OnTimelineUndoRedo(owner)),
                    canEdit: () => !IsPlaying,
                    completeUndo: () => Undo?.CompleteUndo(),
                    frameRateProvider: () => Asset?.FrameRate ??
                        _combatProfile?.FrameRate ?? TimelineEditorDocument.DefaultFrameRate));
            }
            Document?.SetSources(sources.ToArray());
            _framePopupField?.SetEnabled(
                Asset != null && _combatProfile == null && !IsPlaying);
            UpdatePlayButton();
        }

        void ApplyPresentationAssetSelection()
        {
            Timeline?.ClearBindings();
            if (Asset == null)
            {
                _framePopupField?.SetValueWithoutNotify(
                    _combatProfile?.FrameRate ?? TimelineEditorDocument.DefaultFrameRate);
                return;
            }

            Asset.ValidateData();
            if (!_frameSelects.Contains(Asset.FrameRate))
            {
                _frameSelects.Add(Asset.FrameRate);
                _frameSelects.Sort();
                if (_framePopupField != null)
                {
                    _framePopupField.choices = _frameSelects;
                }
            }
            _framePopupField?.SetValueWithoutNotify(Asset.FrameRate);

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Asset, out var guid, out long _))
            {
                SettingTools.SavePlayerPrefs("timeline_asset", guid);
            }
            if (!isCreateing)
            {
                _OnBindObjs();
                RefreshEntity?.Invoke();
            }
        }

        void _OnBindObjs()
        {
            if (Asset == null) return;
            if (Timeline == null) return;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ofEntity.value, out var entityGuid, out long _) &&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Asset, out var assetGuid, out long _))
            {
                var bindingKey = $"{entityGuid}:{assetGuid}";
                if (!_binds.TryGetValue(bindingKey, out var dict))
                {
                    var str = PlayerPrefs.GetString(bindingKey, string.Empty);
                    if (!string.IsNullOrEmpty(str))
                    {
                        try
                        {
                            dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, BindData>>(str);
                            _binds[bindingKey] = dict;
                        }
                        catch
                        {
                            PlayerPrefs.DeleteKey(bindingKey);
                        }
                    }
                }
                if (dict != null)
                {
                    foreach (var (trackId, bindData) in dict)
                    {
                        var track = Asset.FindTrack(trackId);
                        if (track == null)
                        {
                            continue;
                        }

                        if (bindData.type == null)
                        {
                            continue;
                        }

                        var components = _entity.Viewer.transform.GetComponentsInChildren(bindData.type, true);
                        if (bindData.index >= 0 && bindData.index < components.Length)
                        {
                            Timeline.SetBinding(track, components[bindData.index]);
                        }
                    }
                }
            }

            AutoBindMissingTracks();
        }

        void AutoBindMissingTracks()
        {
            if (Asset?.tracks == null || Timeline == null || _entity?.Viewer == null)
            {
                return;
            }

            foreach (var track in Asset.tracks)
            {
                UnityEngine.Object target = null;
                switch (track)
                {
                    case AnimationTrackAsset animationTrack when Timeline.GetBinding<Animator>(animationTrack) == null:
                        target = _entity.Viewer.GetComponentInChildren<Animator>(true);
                        break;
                    case ParticleAssetTrack particleTrack when Timeline.GetBinding<ParticleSystem>(particleTrack) == null:
                        target = _entity.Viewer.GetComponentInChildren<ParticleSystem>(true);
                        break;
                }

                if (target != null)
                {
                    _RefreshBinds(track, target);
                }
            }
        }

        partial void _OnBtnCreateClick()
        {
            if (createView.style.display == DisplayStyle.None)
            {
                createView.style.display = DisplayStyle.Flex;
            }
            else
            {
                createView.style.display = DisplayStyle.None;
            }
        }
        partial void _OnInputPathChanged(ChangeEvent<string> e)
        {
            SelectCreatePath();
        }

        void SelectCreatePath()
        {
            var temPath = EditorUtility.OpenFolderPanel("请选择保存路径", inputPath?.value ?? Path, "");
            if (temPath.Length == 0)
            {
                return;
            }

            var projectPath = FileUtil.GetProjectRelativePath(temPath);
            if (string.IsNullOrEmpty(projectPath) || !projectPath.StartsWith("Assets", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("错误", "Timeline 资源必须保存在当前项目的 Assets 目录内。", "ok");
                return;
            }
            inputPath.SetValueWithoutNotify(projectPath);
        }
        partial void _OnBtnOkClick()
        {
            if (string.IsNullOrEmpty(inputName.text))
            {
                Log.Error("名字不能为空");
                return;
            }
            var assetName = $"{inputPath.text}/{inputName.text}.asset";
            if (AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetName) != null)
            {
                Log.Error("重复创建同名TimelineAsset");
                return;
            }

            createView.style.display = DisplayStyle.None;
            var asset = ScriptableObject.CreateInstance<TimelineAsset>();
            asset.SetFrameRate(_framePopupField?.value ?? TimelineAsset.DefaultFrameRate);
            asset.ValidateData();
            AssetDatabase.CreateAsset(asset, assetName);
            ofTimeline.value = asset;
        }

        private void DestroyPreviewEntity()
        {
            Timeline?.Stop();
            _previewPlayer?.Release();
            _previewPlayer = null;
            Timeline = null;

            if (_entity != null)
            {
                _entity.Destroy();
                _entity = null;
            }

            if (_previewInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }
        }

        private static void CleanupAllPreviewObjects()
        {
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var gameObject in objects)
            {
                if (gameObject == null ||
                    EditorUtility.IsPersistent(gameObject) ||
                    !gameObject.name.EndsWith(PreviewObjectSuffix, StringComparison.Ordinal) ||
                    (gameObject.hideFlags & PreviewObjectHideFlags) == 0)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        void OnTimelineUndoRedo(UnityEngine.Object owner)
        {
            if (Document?.RefreshAfterUndo(owner) == true)
            {
                return;
            }

            // 非当前选中的 Timeline 也可能位于全局 Undo 栈中；只保存 owner，
            // 不保留已经脱离 Document 的旧 source/adapters。
            switch (owner)
            {
                case TimelineAsset timelineAsset:
                    timelineAsset.ValidateData();
                    EditorUtility.SetDirty(timelineAsset);
                    AssetDatabase.SaveAssets();
                    break;
                case CombatActionAsset combatAction:
                    combatAction.ValidateData();
                    EditorUtility.SetDirty(combatAction);
                    AssetDatabase.SaveAssets();
                    break;
            }
        }

        void OnDocumentStructureChanged(ITimelineEditorSource source)
        {
            InspectorContent?.FreshInspector(null, null);
            RefreshView?.Invoke();
            if (source?.Role == TimelineEditorSourceRole.Presentation)
            {
                Timeline?.ClearBindings();
                _OnBindObjs();
            }
        }

        void OnDocumentChanged(ITimelineEditorSource source)
        {
            if (!isCreateing && source?.Role == TimelineEditorSourceRole.Presentation)
            {
                RefreshEntity?.Invoke();
            }
            clipView?.RefreshLayout();
            UpdateDurationLabel();
        }

        void SyncCombatActionDurationBeforeSave()
        {
            if (_combatAction == null || Asset == null ||
                Asset.DurationFrames <= _combatAction.DurationFrames)
            {
                return;
            }

            var combatAction = _combatAction;
            Undo?.RecordAdditionalObject(
                "同步技能逻辑时长",
                combatAction,
                () => OnTimelineUndoRedo(combatAction));
            CombatEditorUtility.SyncActionDurationToTimeline(
                combatAction,
                Asset,
                recordUndo: false);
        }

        void _SaveAssets()
        {
            if (Document?.HasSource != true) return;
            Document.SaveAll();
            clipView?.RefreshLayout();
            UpdateDurationLabel();
        }

        void _RefreshBinds(TimelineTrackAsset track, UnityEngine.Object obj)
        {
            if (track == null)
            {
                return;
            }

            Timeline?.SetBinding(track, obj);
            if (Asset == null || _entity?.Viewer == null || ofEntity.value is not GameObject gameObject)
            {
                return;
            }
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(gameObject, out var entityGuid, out long _) ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Asset, out var assetGuid, out long _))
            {
                return;
            }

            var bindingKey = $"{entityGuid}:{assetGuid}";
            if (!_binds.TryGetValue(bindingKey, out var dict) || dict == null)
            {
                dict = new Dictionary<string, BindData>();
                _binds[bindingKey] = dict;
            }

            if (obj == null)
            {
                dict.Remove(track.Id);
            }
            else
            {
                var type = obj.GetType();
                var components = _entity.Viewer.transform.GetComponentsInChildren(type, true);
                for (var index = 0; index < components.Length; index++)
                {
                    if (components[index] != obj)
                    {
                        continue;
                    }
                    dict[track.Id] = new BindData(index, type);
                    break;
                }
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(dict);
            SettingTools.SavePlayerPrefs(bindingKey, json);
        }
        void _RefreshEntity()
        {
            if (Asset == null || Timeline == null)
            {
                return;
            }
            if (_combatAction != null && _combatProfile != null)
            {
                _previewPlayer ??= new CombatTimelinePlayer(Timeline);
                var plan = BuildPreviewPlan();
                _previewPlayer.Synchronize(
                    plan,
                    _entity?.Viewer?.GetComponentInChildren<Animator>(),
                    true);
                _previewPlayer.Evaluate(plan, false);
                return;
            }
            Timeline.Play(Asset);
            Timeline.Set(clipView?.CurFrame ?? 0);
        }

        void _MarkerMove(int frame)
        {
            if (Asset == null || Timeline == null)
            {
                return;
            }
            if (_previewPlayer != null && _combatAction != null && _combatProfile != null)
            {
                _previewPlayer.Evaluate(BuildPreviewPlan(), false);
                return;
            }
            Timeline.Set(frame);
        }

        private CombatTimelinePlan BuildPreviewPlan()
        {
            var frame = clipView?.CurFrame ?? 0;
            var baseAsset = default(TimelineAsset);
            var baseKey = "preview:action-only";
            if (_previewBaseMode != PreviewBaseMode.ActionOnly && _combatProfile != null)
            {
                var state = _previewBaseMode == PreviewBaseMode.Move
                    ? LocomotionState.Move
                    : LocomotionState.Idle;
                baseAsset = _combatProfile.GetStateTimeline(StateLayer.Locomotion, (int)state);
                baseKey = $"preview:locomotion:{(int)state}:{baseAsset?.name ?? "none"}";
            }
            var baseSelection = new CombatTimelineSelection(baseKey, baseAsset, frame);
            var actionSelection = new CombatTimelineSelection(
                $"preview:action:{Asset.GetInstanceID()}",
                Asset,
                frame);
            return new CombatTimelinePlan(baseSelection, actionSelection);
        }

        private void UpdatePreviewBaseSelector()
        {
            if (_previewBasePopup == null)
            {
                return;
            }
            var enabled = _combatAction != null && _combatProfile != null && Asset != null;
            _previewBasePopup.style.display = enabled
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            if (enabled)
            {
                _previewBasePopup.SetValueWithoutNotify(_previewBaseMode);
            }
        }

        private static string GetPreviewBaseText(PreviewBaseMode mode)
        {
            return mode switch
            {
                PreviewBaseMode.ActionOnly => "单独动作",
                PreviewBaseMode.Idle => "站立基底",
                PreviewBaseMode.Move => "移动基底",
                _ => mode.ToString(),
            };
        }
    }

}

