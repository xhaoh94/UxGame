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
    ///
    /// 区间窗口轨由各自适配器声明；离散帧事件通过独立的事件源接口暴露，不占用轨道。
    /// 本类负责暴露资产与帧参数、统一 Undo 与保存，并把 Inspector 分派回元素自己。
    /// </summary>
    public sealed class CombatLogicTimelineSource : ITimelineEditorSource, ITimelineEditorFrameEventSource
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
        readonly List<ITimelineEditorTrack> tracks = new();
        readonly List<ITimelineEditorFrameEvent> frameEvents = new();
        CombatCancelWindowEditorTrack cancelTrack;
        CombatLinkWindowEditorTrack linkTrack;
        CombatHitboxWindowEditorTrack hitboxTrack;
        readonly string id;

        public CombatLogicTimelineSource(CombatActionAsset action, CharacterCombatProfile profile = null, int frameRate = TimelineEditorDocument.DefaultFrameRate, Action<string, UnityEngine.Object, Action> registerUndo = null, Action save = null, Func<bool> canEdit = null, Action completeUndo = null, Func<int> frameRateProvider = null)
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

            SyncTracks();
            SyncFrameEvents();
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
        public IReadOnlyList<ITimelineEditorFrameEvent> FrameEvents => frameEvents;
        public CombatActionAsset Action => action;
        public CharacterCombatProfile Profile => profile;

        public event Action StructureChanged;
        public event Action Changed;

        public IReadOnlyList<Type> GetTrackTypes()
        {
            var result = new List<Type>();
            if (cancelTrack == null)
            {
                result.Add(typeof(CombatCancelWindowEditorTrack));
            }
            if (linkTrack == null)
            {
                result.Add(typeof(CombatLinkWindowEditorTrack));
            }
            if (hitboxTrack == null)
            {
                result.Add(typeof(CombatHitboxWindowEditorTrack));
            }
            return result;
        }

        public string GetTrackDisplayName(Type trackType)
        {
            if (trackType == typeof(CombatCancelWindowEditorTrack))
            {
                return "可取消窗口";
            }
            if (trackType == typeof(CombatLinkWindowEditorTrack))
            {
                return "连招衔接";
            }
            if (trackType == typeof(CombatHitboxWindowEditorTrack))
            {
                return "攻击判定";
            }
            return trackType?.Name ?? string.Empty;
        }

        public ITimelineEditorTrack AddTrack(Type trackType)
        {
            if (!CanEdit || action.DurationFrames <= 0)
            {
                return null;
            }

            if (trackType == typeof(CombatCancelWindowEditorTrack) && cancelTrack == null)
            {
                cancelTrack = new CombatCancelWindowEditorTrack(this);
            }
            else if (trackType == typeof(CombatLinkWindowEditorTrack) && linkTrack == null)
            {
                linkTrack = new CombatLinkWindowEditorTrack(this);
            }
            else if (trackType == typeof(CombatHitboxWindowEditorTrack) && hitboxTrack == null)
            {
                hitboxTrack = new CombatHitboxWindowEditorTrack(this);
            }
            else
            {
                return null;
            }

            var track = trackType == typeof(CombatCancelWindowEditorTrack)
                ? (ITimelineEditorTrack)cancelTrack
                : trackType == typeof(CombatLinkWindowEditorTrack)
                    ? linkTrack
                    : hitboxTrack;
            if (track == null || track.CreateClip() == null)
            {
                cancelTrack = null;
                linkTrack = null;
                hitboxTrack = null;
                SyncTracks();
                return null;
            }
            return track;
        }

        public bool SetFrameRate(int value)
        {
            return false;
        }

        /// <summary>把 Inspector 分派回元素自己 —— 本类因此不认识任何一种具体窗口类型。</summary>
        public TimelineInspectorBase CreateInspector(object selection)
        {
            return selection is ICombatLogicTimelineInspectorSource inspectorSource &&
                   ReferenceEquals(inspectorSource.Owner, this)
                ? inspectorSource.CreateInspector()
                : null;
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
            SaveInternal(true);
        }

        public void RefreshAfterUndo()
        {
            action.ValidateData();
            bindings.Clear();
            SyncTracks();
            SyncFrameEvents();
            SaveInternal(false);
            NotifyStructureChanged();
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

        internal ActionCancelWindow FindCancelWindow(string stableId)
        {
            return FindWindow(action.CancelWindows, stableId);
        }

        internal ActionLinkWindow FindLinkWindow(string stableId)
        {
            return FindWindow(action.LinkWindows, stableId);
        }

        internal ActionHitboxWindow FindHitboxWindow(string stableId)
        {
            return FindWindow(action.HitboxWindows, stableId);
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

        /// <summary>轨道增删后必须同时通知结构变更与内容变更，帧标尺与 Inspector 都要重建。</summary>
        internal void NotifyStructureChanged()
        {
            SyncTracks();
            SyncFrameEvents();
            StructureChanged?.Invoke();
            Changed?.Invoke();
        }

        internal void NotifyChanged()
        {
            Changed?.Invoke();
        }

        public IReadOnlyList<Type> GetFrameEventTypes()
        {
            return new[] { typeof(ActionSpawnEvent) };
        }

        public string GetFrameEventDisplayName(Type eventType)
        {
            return eventType == typeof(ActionSpawnEvent)
                ? "生成物事件"
                : eventType?.Name ?? string.Empty;
        }

        public ITimelineEditorFrameEvent AddFrameEvent(Type eventType, int frame)
        {
            if (!CanEdit || eventType != typeof(ActionSpawnEvent) || action.DurationFrames <= 0)
            {
                return null;
            }

            RecordEdit("combat_add_frame_event");
            var stableId = Guid.NewGuid().ToString("N");
            var serialized = new SerializedObject(action);
            serialized.Update();
            var events = serialized.FindProperty("frameEvents");
            if (events == null)
            {
                return null;
            }

            var index = events.arraySize;
            events.InsertArrayElementAtIndex(index);
            var element = events.GetArrayElementAtIndex(index);
            element.managedReferenceValue = new ActionSpawnEvent();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            serialized.Update();
            element = serialized.FindProperty("frameEvents").GetArrayElementAtIndex(index);
            element.FindPropertyRelative("stableId").stringValue = stableId;
            element.FindPropertyRelative("Frame").intValue = Mathf.Clamp(frame, 0, action.DurationFrames - 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CommitFrameEventStructure();
            return FindFrameEvent(stableId);
        }

        public bool RemoveFrameEvent(ITimelineEditorFrameEvent frameEvent)
        {
            if (!CanEdit || frameEvent == null || !ReferenceEquals(frameEvent.Source, this))
            {
                return false;
            }

            var index = FindFrameEventIndex(frameEvent.Id);
            if (index < 0)
            {
                return false;
            }

            RecordEdit("combat_remove_frame_event");
            var serialized = new SerializedObject(action);
            serialized.Update();
            serialized.FindProperty("frameEvents").DeleteArrayElementAtIndex(index);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CommitFrameEventStructure();
            return true;
        }

        internal CombatFrameEventEditorAdapter FindFrameEvent(string stableId)
        {
            for (var i = 0; i < frameEvents.Count; i++)
            {
                if (string.Equals(frameEvents[i].Id, stableId, StringComparison.Ordinal))
                {
                    return frameEvents[i] as CombatFrameEventEditorAdapter;
                }
            }
            return null;
        }

        internal CombatFrameEvent FindFrameEventData(string stableId)
        {
            if (string.IsNullOrEmpty(stableId) || action.FrameEvents == null)
            {
                return null;
            }
            for (var i = 0; i < action.FrameEvents.Count; i++)
            {
                var frameEvent = action.FrameEvents[i];
                if (frameEvent != null && string.Equals(frameEvent.StableId, stableId, StringComparison.Ordinal))
                {
                    return frameEvent;
                }
            }
            return null;
        }

        internal ActionSpawnEvent FindSpawnEvent(string stableId)
        {
            return FindFrameEventData(stableId) as ActionSpawnEvent;
        }

        internal int FindFrameEventIndex(string stableId)
        {
            if (string.IsNullOrEmpty(stableId))
            {
                return -1;
            }
            var serialized = new SerializedObject(action);
            serialized.Update();
            var events = serialized.FindProperty("frameEvents");
            if (events == null)
            {
                return -1;
            }
            for (var i = 0; i < events.arraySize; i++)
            {
                var idProperty = events.GetArrayElementAtIndex(i).FindPropertyRelative("stableId");
                if (idProperty != null && idProperty.stringValue == stableId)
                {
                    return i;
                }
            }
            return -1;
        }

        internal void SetFrameEventFrame(string stableId, int frame)
        {
            var index = FindFrameEventIndex(stableId);
            if (index < 0 || action.DurationFrames <= 0)
            {
                return;
            }
            var serialized = new SerializedObject(action);
            serialized.Update();
            var element = serialized.FindProperty("frameEvents").GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Frame").intValue = Mathf.Clamp(frame, 0, action.DurationFrames - 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal void SetSpawnEventProfile(string stableId, CombatSpawnProfile spawn)
        {
            var index = FindFrameEventIndex(stableId);
            if (index < 0)
            {
                return;
            }
            var serialized = new SerializedObject(action);
            serialized.Update();
            var element = serialized.FindProperty("frameEvents").GetArrayElementAtIndex(index);
            element.FindPropertyRelative("spawnProfile").objectReferenceValue = spawn;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal void SetFrameEventDisplayName(string stableId, string displayName)
        {
            var index = FindFrameEventIndex(stableId);
            if (index < 0)
            {
                return;
            }
            var serialized = new SerializedObject(action);
            serialized.Update();
            var element = serialized.FindProperty("frameEvents").GetArrayElementAtIndex(index);
            element.FindPropertyRelative("displayName").stringValue = displayName ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        void CommitFrameEventStructure()
        {
            action.ValidateData();
            SyncFrameEvents();
            Save();
            NotifyStructureChanged();
        }

        void SyncTracks()
        {
            if (action.CancelWindows != null && action.CancelWindows.Count > 0)
            {
                cancelTrack ??= new CombatCancelWindowEditorTrack(this);
            }
            else
            {
                cancelTrack = null;
            }

            if (action.LinkWindows != null && action.LinkWindows.Count > 0)
            {
                linkTrack ??= new CombatLinkWindowEditorTrack(this);
            }
            else
            {
                linkTrack = null;
            }

            if (action.HitboxWindows != null && action.HitboxWindows.Count > 0)
            {
                hitboxTrack ??= new CombatHitboxWindowEditorTrack(this);
            }
            else
            {
                hitboxTrack = null;
            }

            tracks.Clear();
            if (cancelTrack != null)
            {
                tracks.Add(cancelTrack);
            }
            if (linkTrack != null)
            {
                tracks.Add(linkTrack);
            }
            if (hitboxTrack != null)
            {
                tracks.Add(hitboxTrack);
            }
        }

        void SyncFrameEvents()
        {
            frameEvents.Clear();
            if (action.FrameEvents == null)
            {
                return;
            }
            for (var i = 0; i < action.FrameEvents.Count; i++)
            {
                var frameEvent = action.FrameEvents[i];
                if (frameEvent != null)
                {
                    frameEvents.Add(new CombatFrameEventEditorAdapter(this, frameEvent.StableId));
                }
            }
        }

        /// <summary>按稳定 ID 取窗口在序列化列表中的下标，找不到返回 -1。</summary>
        internal int FindWindowIndex(string propertyName, string stableId)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(stableId))
            {
                return -1;
            }
            var serialized = new SerializedObject(action);
            serialized.Update();
            var windows = serialized.FindProperty(propertyName);
            if (windows == null)
            {
                return -1;
            }
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

        void SaveInternal(bool completePendingUndo)
        {
            action.ValidateData();
            if (completePendingUndo)
            {
                completeUndo?.Invoke();
            }
            EditorUtility.SetDirty(action);
            AssetDatabase.SaveAssets();
            afterSave?.Invoke();
        }

        void RegisterUndo(string key)
        {
            if (registerUndo != null)
            {
                registerUndo.Invoke(key, action, RefreshAfterUndo);
            }
            else if (key.IndexOf("drag", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Undo.RegisterCompleteObjectUndo(action, key);
            }
            else
            {
                Undo.RecordObject(action, key);
            }
        }

        static TWindow FindWindow<TWindow>(IReadOnlyList<TWindow> windows, string stableId)
            where TWindow : CombatLogicWindow
        {
            if (string.IsNullOrEmpty(stableId) || windows == null)
            {
                return null;
            }
            for (var i = 0; i < windows.Count; i++)
            {
                var window = windows[i];
                if (window != null && string.Equals(window.StableId, stableId, StringComparison.Ordinal))
                {
                    return window;
                }
            }
            return null;
        }
    }
}
