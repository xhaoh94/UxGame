using Assets.Editor.Timeline;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    /// <summary>
    /// 将 CombatActionAsset 的确定性帧数据适配到通用 Timeline 编辑器。
    /// 逻辑轨只声明确定性时序；表现 Timeline 仍由 TimelineAssetEditorSource 独立负责。
    /// </summary>
    public sealed class CombatLogicTimelineSource : ITimelineEditorSource
    {
        readonly CombatActionAsset action;
        readonly CharacterCombatProfile profile;
        readonly int fallbackFrameRate;
        readonly Func<int> frameRateProvider;
        readonly Action<string, UnityEngine.Object, Action> registerUndo;
        readonly Action afterSave;
        readonly Action completeUndo;
        readonly Func<bool> canEdit;
        readonly Dictionary<object, HashSet<Action>> bindings = new();
        readonly CombatCancelWindowEditorTrack cancelTrack;
        readonly CombatHitWindowEditorTrack hitTrack;
        readonly IReadOnlyList<ITimelineEditorTrack> tracks;
        readonly string id;

        public CombatLogicTimelineSource(
            CombatActionAsset action,
            CharacterCombatProfile profile = null,
            int frameRate = TimelineEditorDocument.DefaultFrameRate,
            Action<string, UnityEngine.Object, Action> registerUndo = null,
            Action save = null,
            Func<bool> canEdit = null,
            Action completeUndo = null,
            Func<int> frameRateProvider = null)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.profile = profile;
            fallbackFrameRate = Mathf.Max(1, frameRate);
            this.frameRateProvider = frameRateProvider;
            this.registerUndo = registerUndo;
            afterSave = save;
            this.canEdit = canEdit;
            this.completeUndo = completeUndo;

            if (action.MigrateLogicItemStableIds() && AssetDatabase.Contains(action))
            {
                // 旧资源首次接入逻辑时间轴时只补齐稳定 ItemId，不静默修正非法业务区间。
                EditorUtility.SetDirty(action);
                AssetDatabase.SaveAssetIfDirty(action);
            }
            var assetPath = AssetDatabase.GetAssetPath(action);
            var guid = string.IsNullOrEmpty(assetPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(assetPath);
            id = string.IsNullOrEmpty(guid)
                ? $"combat-action-instance:{action.GetInstanceID()}"
                : $"combat-action:{guid}";

            cancelTrack = new CombatCancelWindowEditorTrack(this);
            hitTrack = new CombatHitWindowEditorTrack(this);
            tracks = new ITimelineEditorTrack[] { cancelTrack, hitTrack };
        }

        public string Id => id;
        public TimelineEditorSourceRole Role => TimelineEditorSourceRole.Logic;
        public UnityEngine.Object UndoOwner => action;
        public bool CanEdit => canEdit?.Invoke() ?? true;
        public bool CanSetFrameRate => false;
        public int FrameRate => Mathf.Max(1, frameRateProvider?.Invoke() ?? fallbackFrameRate);
        public int DurationFrames => action.DurationFrames;
        public int TrackCount => tracks.Count;
        public IReadOnlyList<ITimelineEditorTrack> Tracks => tracks;
        public CombatActionAsset Action => action;
        public CharacterCombatProfile Profile => profile;

        public event Action StructureChanged;
        public event Action Changed;

        public IReadOnlyList<Type> GetTrackTypes()
        {
            // 取消窗口与命中窗口都是动作存在时自动出现的固定逻辑轨，不允许重复添加。
            return Array.Empty<Type>();
        }

        public string GetTrackDisplayName(Type trackType)
        {
            return trackType?.Name ?? string.Empty;
        }

        public ITimelineEditorTrack AddTrack(Type trackType)
        {
            return null;
        }

        public bool SetFrameRate(int value)
        {
            return false;
        }

        public TimelineInspectorBase CreateInspector(object selection)
        {
            return selection switch
            {
                CombatCancelWindowEditorTrack track when ReferenceEquals(track.Source, this) =>
                    new CombatCancelWindowTrackInspector(this, track),
                CombatCancelWindowEditorClip clip when ReferenceEquals(clip.Source, this) =>
                    new CombatCancelWindowClipInspector(this, clip),
                CombatHitWindowEditorTrack track when ReferenceEquals(track.Source, this) =>
                    new CombatHitWindowTrackInspector(this, track),
                CombatHitWindowEditorClip clip when ReferenceEquals(clip.Source, this) =>
                    new CombatHitWindowClipInspector(this, clip),
                _ => null,
            };
        }

        public void CommitInspectorChange(object assetObject)
        {
            if (!CanEdit)
            {
                return;
            }
            Save();
            Changed?.Invoke();
        }

        public void Save()
        {
            action.ValidateData();
            completeUndo?.Invoke();
            EditorUtility.SetDirty(action);
            AssetDatabase.SaveAssets();
            afterSave?.Invoke();
        }

        public void RefreshAfterUndo()
        {
            action.ValidateData();
            bindings.Clear();
            cancelTrack.RebuildAdapters();
            hitTrack.RebuildAdapters();
            Save();
            StructureChanged?.Invoke();
            Changed?.Invoke();
        }

        public void Bind(object selection, Action callback)
        {
            if (selection == null || callback == null)
            {
                return;
            }
            if (!bindings.TryGetValue(selection, out var callbacks))
            {
                callbacks = new HashSet<Action>();
                bindings.Add(selection, callbacks);
            }
            callbacks.Add(callback);
        }

        public void Unbind(object selection, Action callback)
        {
            if (selection != null && bindings.TryGetValue(selection, out var callbacks))
            {
                callbacks.Remove(callback);
            }
        }

        internal CombatCancelWindowEditorClip AddCancelWindow()
        {
            if (!CanEdit || DurationFrames <= 0)
            {
                return null;
            }

            RegisterUndo("combat_add_cancel_window");
            var serialized = new SerializedObject(action);
            serialized.Update();
            var windows = serialized.FindProperty("cancelWindows");
            var index = windows.arraySize;
            windows.InsertArrayElementAtIndex(index);
            var element = windows.GetArrayElementAtIndex(index);
            var stableId = Guid.NewGuid().ToString("N");
            element.FindPropertyRelative("stableId").stringValue = stableId;
            var startFrame = Mathf.Clamp(cancelTrack.EndFrame, 0, DurationFrames - 1);
            element.FindPropertyRelative("StartFrame").intValue = startFrame;
            element.FindPropertyRelative("EndFrame").intValue = startFrame + 1;
            element.FindPropertyRelative("TargetActionId").intValue = Mathf.Max(1, action.ActionId);
            element.FindPropertyRelative("RequiresHitConfirm").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            action.ValidateData();
            cancelTrack.RebuildAdapters();
            Save();
            StructureChanged?.Invoke();
            Changed?.Invoke();
            return cancelTrack.FindClip(stableId);
        }

        internal bool RemoveCancelWindow(CombatCancelWindowEditorClip clip)
        {
            if (!CanEdit || clip == null || !ReferenceEquals(clip.Source, this))
            {
                return false;
            }

            var index = FindWindowIndex("cancelWindows", clip.Id);
            if (index < 0)
            {
                return false;
            }

            RegisterUndo("combat_remove_cancel_window");
            var serialized = new SerializedObject(action);
            serialized.Update();
            serialized.FindProperty("cancelWindows").DeleteArrayElementAtIndex(index);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            action.ValidateData();
            cancelTrack.RebuildAdapters();
            Save();
            StructureChanged?.Invoke();
            Changed?.Invoke();
            return true;
        }

        internal void BeginWindowDrag(CombatCancelWindowEditorClip clip)
        {
            if (CanEdit && clip != null && ReferenceEquals(clip.Source, this))
            {
                RegisterUndo("combat_drag_cancel_window");
            }
        }

        internal void SetWindowFrames(
            CombatCancelWindowEditorClip clip,
            int startFrame,
            int endFrame,
            bool notify)
        {
            if (!CanEdit || DurationFrames <= 0 || clip == null ||
                !ReferenceEquals(clip.Source, this))
            {
                return;
            }

            var window = FindWindow(clip.Id);
            if (window == null)
            {
                return;
            }

            startFrame = Mathf.Clamp(startFrame, 0, DurationFrames - 1);
            endFrame = Mathf.Clamp(endFrame, startFrame + 1, DurationFrames);
            window.StartFrame = startFrame;
            window.EndFrame = endFrame;
            if (notify)
            {
                Run(clip);
            }
        }

        internal void CommitWindowEdit(CombatCancelWindowEditorClip clip)
        {
            if (!CanEdit || clip == null || !ReferenceEquals(clip.Source, this))
            {
                return;
            }
            var window = FindWindow(clip.Id);
            window?.ValidateData();
            Save();
            Run(clip);
            Changed?.Invoke();
        }

        internal void SetTargetActionId(CombatCancelWindowEditorClip clip, int targetActionId)
        {
            var window = FindWindow(clip?.Id);
            targetActionId = Mathf.Max(1, targetActionId);
            if (!CanEdit || window == null || window.TargetActionId == targetActionId)
            {
                return;
            }

            RegisterUndo("combat_cancel_target_action");
            window.TargetActionId = targetActionId;
            CommitWindowEdit(clip);
        }

        internal void SetRequiresHitConfirm(CombatCancelWindowEditorClip clip, bool value)
        {
            var window = FindWindow(clip?.Id);
            if (!CanEdit || window == null || window.RequiresHitConfirm == value)
            {
                return;
            }

            RegisterUndo("combat_cancel_hit_confirm");
            window.RequiresHitConfirm = value;
            CommitWindowEdit(clip);
        }

        internal CombatHitWindowEditorClip AddHitWindow()
        {
            if (!CanEdit || DurationFrames <= 0)
            {
                return null;
            }

            RegisterUndo("combat_add_hit_window");
            var serialized = new SerializedObject(action);
            serialized.Update();
            var windows = serialized.FindProperty("hitWindows");
            var index = windows.arraySize;
            windows.InsertArrayElementAtIndex(index);
            var element = windows.GetArrayElementAtIndex(index);
            var stableId = Guid.NewGuid().ToString("N");
            element.FindPropertyRelative("stableId").stringValue = stableId;
            var startFrame = Mathf.Clamp(hitTrack.EndFrame, 0, DurationFrames - 1);
            element.FindPropertyRelative("StartFrame").intValue = startFrame;
            element.FindPropertyRelative("EndFrame").intValue = startFrame + 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            action.ValidateData();
            hitTrack.RebuildAdapters();
            Save();
            StructureChanged?.Invoke();
            Changed?.Invoke();
            return hitTrack.FindClip(stableId);
        }

        internal bool RemoveHitWindow(CombatHitWindowEditorClip clip)
        {
            if (!CanEdit || clip == null || !ReferenceEquals(clip.Source, this))
            {
                return false;
            }

            var index = FindWindowIndex("hitWindows", clip.Id);
            if (index < 0)
            {
                return false;
            }

            RegisterUndo("combat_remove_hit_window");
            var serialized = new SerializedObject(action);
            serialized.Update();
            serialized.FindProperty("hitWindows").DeleteArrayElementAtIndex(index);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            action.ValidateData();
            hitTrack.RebuildAdapters();
            Save();
            StructureChanged?.Invoke();
            Changed?.Invoke();
            return true;
        }

        internal void BeginHitWindowDrag(CombatHitWindowEditorClip clip)
        {
            if (CanEdit && clip != null && ReferenceEquals(clip.Source, this))
            {
                RegisterUndo("combat_drag_hit_window");
            }
        }

        internal void SetHitWindowFrames(
            CombatHitWindowEditorClip clip,
            int startFrame,
            int endFrame,
            bool notify)
        {
            if (!CanEdit || DurationFrames <= 0 || clip == null ||
                !ReferenceEquals(clip.Source, this))
            {
                return;
            }

            var window = FindHitWindow(clip.Id);
            if (window == null)
            {
                return;
            }

            startFrame = Mathf.Clamp(startFrame, 0, DurationFrames - 1);
            endFrame = Mathf.Clamp(endFrame, startFrame + 1, DurationFrames);
            window.StartFrame = startFrame;
            window.EndFrame = endFrame;
            if (notify)
            {
                Run(clip);
            }
        }

        internal void SetHitWindowGeometry(
            CombatHitWindowEditorClip clip,
            ActionHitShape shape,
            int radiusMillimeters)
        {
            if (!CanEdit || clip == null || !ReferenceEquals(clip.Source, this))
            {
                return;
            }
            var index = FindWindowIndex("hitWindows", clip.Id);
            if (index < 0)
            {
                return;
            }
            var serialized = new SerializedObject(action);
            serialized.Update();
            var element = serialized.FindProperty("hitWindows").GetArrayElementAtIndex(index);
            element.FindPropertyRelative("shape").enumValueIndex = (int)shape;
            element.FindPropertyRelative("radiusMillimeters").intValue =
                Mathf.Clamp(radiusMillimeters, 1, 10000000);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Save();
            Run(clip);
            Changed?.Invoke();
        }

        internal void CommitHitWindowEdit(CombatHitWindowEditorClip clip)
        {
            if (!CanEdit || clip == null || !ReferenceEquals(clip.Source, this))
            {
                return;
            }
            FindHitWindow(clip.Id)?.ValidateData();
            Save();
            Run(clip);
            Changed?.Invoke();
        }

        internal ActionHitWindow FindHitWindow(string stableId)
        {
            if (string.IsNullOrEmpty(stableId) || action.HitWindows == null)
            {
                return null;
            }
            foreach (var window in action.HitWindows)
            {
                if (window != null && string.Equals(window.StableId, stableId, StringComparison.Ordinal))
                {
                    return window;
                }
            }
            return null;
        }

        internal string GetHitWindowDisplayName(ActionHitWindow window)
        {
            if (window == null || action.HitWindows == null)
            {
                return "命中窗口";
            }
            for (var i = 0; i < action.HitWindows.Count; i++)
            {
                if (ReferenceEquals(action.HitWindows[i], window))
                {
                    return $"命中 {i + 1}";
                }
            }
            return "命中窗口";
        }

        internal bool RecordEdit(string key)
        {
            if (!CanEdit)
            {
                return false;
            }
            RegisterUndo(key);
            return true;
        }

        internal ActionCancelWindow FindWindow(string stableId)
        {
            if (string.IsNullOrEmpty(stableId) || action.CancelWindows == null)
            {
                return null;
            }
            foreach (var window in action.CancelWindows)
            {
                if (window != null && string.Equals(window.StableId, stableId, StringComparison.Ordinal))
                {
                    return window;
                }
            }
            return null;
        }

        internal string GetWindowDisplayName(ActionCancelWindow window)
        {
            if (window == null)
            {
                return "取消窗口";
            }

            var target = profile?.FindAction(window.TargetActionId);
            var targetName = target == null || string.IsNullOrEmpty(target.DisplayName)
                ? window.TargetActionId.ToString()
                : $"{target.DisplayName} ({window.TargetActionId})";
            return window.RequiresHitConfirm ? $"→ {targetName} [需命中]" : $"→ {targetName}";
        }

        internal void Run(object selection)
        {
            if (selection != null && bindings.TryGetValue(selection, out var callbacks))
            {
                foreach (var callback in new List<Action>(callbacks))
                {
                    callback.Invoke();
                }
            }
        }

        int FindWindowIndex(string propertyName, string stableId)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(stableId))
            {
                return -1;
            }
            var serialized = new SerializedObject(action);
            serialized.Update();
            var windows = serialized.FindProperty(propertyName);
            for (var i = 0; i < windows.arraySize; i++)
            {
                var idProperty = windows.GetArrayElementAtIndex(i).FindPropertyRelative("stableId");
                if (idProperty != null && idProperty.stringValue == stableId)
                {
                    return i;
                }
            }
            return -1;
        }

        void RegisterUndo(string key)
        {
            registerUndo?.Invoke(key, action, RefreshAfterUndo);
        }

    }

    public sealed class CombatCancelWindowEditorTrack : ITimelineEditorTrack
    {
        readonly List<CombatCancelWindowEditorClip> clips = new();

        internal CombatCancelWindowEditorTrack(CombatLogicTimelineSource source)
        {
            Source = source;
            RebuildAdapters();
        }

        public CombatLogicTimelineSource Source { get; }
        ITimelineEditorSource ITimelineEditorTrack.Source => Source;
        public string Id => $"{Source.Id}/cancel-windows";
        public string Name => "取消窗口";
        public string TypeName => nameof(ActionCancelWindow);
        public string DisplayTypeName => "逻辑";
        public Color Color => new(0.88f, 0.52f, 0.18f);
        public int EndFrame
        {
            get
            {
                var endFrame = 0;
                foreach (var clip in clips)
                {
                    endFrame = Mathf.Max(endFrame, clip.EndFrame);
                }
                return endFrame;
            }
        }
        public bool CanRename => false;
        public bool CanRemove => false;
        public bool CanCreateClip => true;
        public IReadOnlyList<CombatCancelWindowEditorClip> Clips => clips;
        IReadOnlyList<ITimelineEditorClip> ITimelineEditorTrack.Clips => clips;

        public void Rename(string name) { }
        public bool Remove() => false;
        public CombatCancelWindowEditorClip CreateClip(string assetPath = null) =>
            Source.AddCancelWindow();
        ITimelineEditorClip ITimelineEditorTrack.CreateClip(string assetPath) => CreateClip(assetPath);

        public bool RemoveClip(ITimelineEditorClip clip)
        {
            return clip is CombatCancelWindowEditorClip cancelClip &&
                   Source.RemoveCancelWindow(cancelClip);
        }

        public bool IsLayoutValid()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var window in Source.Action.CancelWindows)
            {
                if (window == null || string.IsNullOrEmpty(window.StableId) ||
                    !ids.Add(window.StableId) || window.StartFrame < 0 ||
                    window.EndFrame <= window.StartFrame ||
                    window.EndFrame > Source.DurationFrames)
                {
                    return false;
                }
            }
            // 多个取消目标在同一帧区间同时开放是合法配置，因此不做重叠限制。
            return true;
        }

        public void UpdateMixData() { }
        public void RecordUndo(string key) => Source.RecordEdit(key);
        public void Bind(Action callback) => Source.Bind(this, callback);
        public void Unbind(Action callback) => Source.Unbind(this, callback);

        internal CombatCancelWindowEditorClip FindClip(string stableId)
        {
            return clips.Find(clip => clip.Id == stableId);
        }

        internal void RebuildAdapters()
        {
            clips.Clear();
            foreach (var window in Source.Action.CancelWindows)
            {
                if (window != null)
                {
                    clips.Add(new CombatCancelWindowEditorClip(this, window.StableId));
                }
            }
        }
    }

    public sealed class CombatCancelWindowEditorClip : ITimelineEditorClip
    {
        internal CombatCancelWindowEditorClip(CombatCancelWindowEditorTrack track, string stableId)
        {
            Track = track;
            Id = stableId;
        }

        public CombatCancelWindowEditorTrack Track { get; }
        public CombatLogicTimelineSource Source => Track.Source;
        ITimelineEditorTrack ITimelineEditorClip.Track => Track;
        ActionCancelWindow Window => Source.FindWindow(Id);
        public string Id { get; }
        public string Name => Source.GetWindowDisplayName(Window);
        public string TypeName => "取消";
        public int StartFrame => Window?.StartFrame ?? 0;
        public int EndFrame => Window?.EndFrame ?? 1;
        public int InFrame => 0;
        public int OutFrame => 0;
        public int DurationFrames => Mathf.Max(1, EndFrame - StartFrame);
        public bool CanFitAnimationDuration => false;
        public int TargetActionId => Window?.TargetActionId ?? 1;
        public bool RequiresHitConfirm => Window?.RequiresHitConfirm == true;

        public void Rename(string name) { }
        public void BeginDrag() => Source.BeginWindowDrag(this);

        public void Drag(DragStatus status, int nowFrame, int lastFrame)
        {
            if (!Source.CanEdit)
            {
                return;
            }

            switch (status)
            {
                case DragStatus.Left:
                    Source.SetWindowFrames(this, nowFrame, EndFrame, true);
                    break;
                case DragStatus.Right:
                    Source.SetWindowFrames(this, StartFrame, nowFrame, true);
                    break;
                case DragStatus.Move:
                    var length = DurationFrames;
                    var start = Mathf.Clamp(
                        StartFrame + nowFrame - lastFrame,
                        0,
                        Source.DurationFrames - length);
                    Source.SetWindowFrames(this, start, start + length, true);
                    break;
            }
        }

        public void SetFrames(int startFrame, int endFrame, bool save = true)
        {
            if (!Source.CanEdit)
            {
                return;
            }
            if (save)
            {
                RecordUndo("combat_cancel_window_frames");
            }
            Source.SetWindowFrames(this, startFrame, endFrame, true);
            if (save)
            {
                CommitEdit();
            }
        }

        public void CommitEdit() => Source.CommitWindowEdit(this);
        public bool TryAssignAnimation(string assetPath) => false;
        public bool TryAssignAnimation(AnimationClip animation) => false;
        public bool FitAnimationDuration() => false;
        public void RecordUndo(string key) => Source.RecordEdit(key);
        public void Bind(Action callback) => Source.Bind(this, callback);
        public void Unbind(Action callback) => Source.Unbind(this, callback);
        public void SetTargetActionId(int actionId) => Source.SetTargetActionId(this, actionId);
        public void SetRequiresHitConfirm(bool value) => Source.SetRequiresHitConfirm(this, value);
    }
}
