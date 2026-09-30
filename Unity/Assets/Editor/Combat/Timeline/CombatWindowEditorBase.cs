using System;
using System.Collections.Generic;
using Assets.Editor.Timeline;
using UnityEditor;
using UnityEngine;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    /// <summary>
    /// 逻辑时间轴上的可选元素自己声明"如何显示 Inspector"。
    ///
    /// 有了它，CombatLogicTimelineSource 只按 Owner 分派，不需要认识任何一种具体窗口类型 ——
    /// 新增一条逻辑轨不需要改动数据源，也不需要往它的 switch 里加分支。
    /// </summary>
    public interface ICombatLogicTimelineInspectorSource
    {
        CombatLogicTimelineSource Owner { get; }

        TimelineInspectorBase CreateInspector();
    }

    /// <summary>
    /// 战斗逻辑窗口轨的公共实现。子类只声明窗口在资产上的哪个列表、叫什么、什么颜色，
    /// 帧区间校验、片段重建、增删窗口后的保存与通知都在这里统一处理。
    ///
    /// 重叠永远是合法的：每条窗口代表一个独立的周期，是否允许同时成立由业务语义决定，
    /// 不由布局校验代判。
    /// </summary>
    public abstract class CombatWindowEditorTrackBase<TWindow, TClip> : ITimelineEditorTrack, ICombatLogicTimelineInspectorSource
        where TWindow : CombatLogicWindow
        where TClip : CombatWindowEditorClipBase<TWindow>
    {
        private const int DefaultWindowLengthFrames = 5;
        readonly List<TClip> clips = new();

        protected CombatWindowEditorTrackBase(CombatLogicTimelineSource source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            RebuildAdapters();
        }

        public CombatLogicTimelineSource Source { get; }
        ITimelineEditorSource ITimelineEditorTrack.Source => Source;
        CombatLogicTimelineSource ICombatLogicTimelineInspectorSource.Owner => Source;

        public string Id => $"{Source.Id}/{TrackIdSuffix}";

        public abstract string Name { get; }
        public abstract string TypeName { get; }
        public virtual string DisplayTypeName => "逻辑";
        public abstract Color Color { get; }

        /// <summary>窗口在 CombatActionAsset 上的序列化字段名，增删与查找都按它定位。</summary>
        protected abstract string WindowPropertyName { get; }

        /// <summary>轨道 ID 后缀，只需在本数据源内唯一。</summary>
        protected abstract string TrackIdSuffix { get; }

        /// <summary>当前动作上的窗口列表，顺序与资产配置一致。</summary>
        protected abstract IReadOnlyList<TWindow> Windows { get; }

        public int EndFrame
        {
            get
            {
                var endFrame = 0;
                for (var i = 0; i < clips.Count; i++)
                {
                    endFrame = Mathf.Max(endFrame, clips[i].EndFrame);
                }
                return endFrame;
            }
        }

        public bool CanRename => false;
        public bool CanRemove => true;
        public bool CanCreateClip => true;
        public IReadOnlyList<TClip> Clips => clips;
        IReadOnlyList<ITimelineEditorClip> ITimelineEditorTrack.Clips => clips;

        public void Rename(string name) { }

        public bool Remove()
        {
            if (!Source.CanEdit || Windows.Count == 0)
            {
                return false;
            }

            Source.RecordEdit(RemoveUndoKey);
            var serialized = new SerializedObject(Source.Action);
            serialized.Update();
            var windows = serialized.FindProperty(WindowPropertyName);
            if (windows == null)
            {
                return false;
            }

            windows.arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CommitStructureChange();
            return true;
        }

        public TClip CreateClip(string assetPath = null) => AddWindow();

        ITimelineEditorClip ITimelineEditorTrack.CreateClip(string assetPath) => CreateClip(assetPath);

        public bool RemoveClip(ITimelineEditorClip clip)
        {
            return clip is TClip typed && RemoveWindow(typed);
        }

        public bool IsLayoutValid()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var windows = Windows;
            for (var i = 0; i < windows.Count; i++)
            {
                var window = windows[i];
                if (window == null ||
                    string.IsNullOrEmpty(window.StableId) ||
                    !ids.Add(window.StableId) ||
                    window.StartFrame < 0 ||
                    window.EndFrame <= window.StartFrame ||
                    window.EndFrame > Source.DurationFrames)
                {
                    return false;
                }
            }
            return true;
        }

        public void UpdateMixData() { }

        public void RecordUndo(string key) => Source.RecordEdit(key);

        public void Bind(Action callback) => Source.Bind(this, callback);

        public void Unbind(Action callback) => Source.Unbind(this, callback);

        internal TClip FindClip(string stableId)
        {
            for (var i = 0; i < clips.Count; i++)
            {
                if (string.Equals(clips[i].Id, stableId, StringComparison.Ordinal))
                {
                    return clips[i];
                }
            }
            return null;
        }

        internal void RebuildAdapters()
        {
            clips.Clear();
            var windows = Windows;
            for (var i = 0; i < windows.Count; i++)
            {
                var window = windows[i];
                if (window != null)
                {
                    clips.Add(MakeClip(window.StableId));
                }
            }
        }

        protected abstract TClip MakeClip(string stableId);

        /// <summary>本轨的 Inspector。由轨道自己创建，数据源因此不需要认识任何具体轨类型。</summary>
        public abstract TimelineInspectorBase CreateInspector();

        /// <summary>补写子类自己的业务字段默认值；通用字段（stableId 与帧区间）由基类写入。</summary>
        protected virtual void WriteNewWindowFields(SerializedProperty element) { }

        /// <summary>结构变更（增删窗口）的统一收尾：校验资产、重建片段、保存并通知。</summary>
        internal void CommitStructureChange()
        {
            Source.Action.ValidateData();
            RebuildAdapters();
            Source.Save();
            Source.NotifyStructureChanged();
        }

        protected virtual string AddUndoKey => "combat_add_window";

        protected virtual string RemoveUndoKey => "combat_remove_window";

        private TClip AddWindow()
        {
            if (!Source.CanEdit || Source.DurationFrames <= 0)
            {
                return null;
            }

            Source.RecordEdit(AddUndoKey);
            var serialized = new SerializedObject(Source.Action);
            serialized.Update();
            var windows = serialized.FindProperty(WindowPropertyName);
            var index = windows.arraySize;
            windows.InsertArrayElementAtIndex(index);
            var element = windows.GetArrayElementAtIndex(index);
            var stableId = Guid.NewGuid().ToString("N");
            element.FindPropertyRelative("stableId").stringValue = stableId;
            var startFrame = Mathf.Clamp(EndFrame, 0, Source.DurationFrames - 1);
            var length = Mathf.Min(DefaultWindowLengthFrames, Source.DurationFrames - startFrame);
            element.FindPropertyRelative("StartFrame").intValue = startFrame;
            element.FindPropertyRelative("EndFrame").intValue = startFrame + Mathf.Max(1, length);
            WriteNewWindowFields(element);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CommitStructureChange();
            return FindClip(stableId);
        }

        private bool RemoveWindow(TClip clip)
        {
            if (!Source.CanEdit || clip == null || !ReferenceEquals(clip.Source, Source))
            {
                return false;
            }

            var index = Source.FindWindowIndex(WindowPropertyName, clip.Id);
            if (index < 0)
            {
                return false;
            }

            Source.RecordEdit(RemoveUndoKey);
            var serialized = new SerializedObject(Source.Action);
            serialized.Update();
            serialized.FindProperty(WindowPropertyName).DeleteArrayElementAtIndex(index);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CommitStructureChange();
            return true;
        }
    }

    /// <summary>
    /// 战斗逻辑窗口片段的公共实现：帧区间读写、拖拽、clamp、undo 登记与提交。
    ///
    /// 拖拽算法只有一份：左右边界取鼠标帧，整体移动按长度平移并把起点夹在容器内，
    /// 三种操作最终都落到 ApplyWindowFrames 上做同一次夹取。
    /// </summary>
    public abstract class CombatWindowEditorClipBase<TWindow> : ITimelineEditorClip, ICombatLogicTimelineInspectorSource
        where TWindow : CombatLogicWindow
    {
        protected CombatWindowEditorClipBase(CombatLogicTimelineSource source, string stableId)
        {
            Source = source;
            Id = stableId;
        }

        internal CombatLogicTimelineSource Source { get; }
        CombatLogicTimelineSource ICombatLogicTimelineInspectorSource.Owner => Source;

        public string Id { get; }

        public abstract string Name { get; }
        public abstract string TypeName { get; }

        /// <summary>具体片段提供所属轨道；接口实现统一留在基类，避免每个子类重复显式实现。</summary>
        protected abstract ITimelineEditorTrack EditorTrack { get; }
        ITimelineEditorTrack ITimelineEditorClip.Track => EditorTrack;

        /// <summary>当前窗口。窗口已被删除时返回 null，读取方必须自行兜底。</summary>
        protected abstract TWindow Window { get; }

        public int StartFrame => Window?.StartFrame ?? 0;
        public int EndFrame => Window?.EndFrame ?? 1;
        public int InFrame => 0;
        public int OutFrame => 0;
        public int DurationFrames => Mathf.Max(1, EndFrame - StartFrame);
        public bool CanFitAnimationDuration => false;

        public void Rename(string name) { }

        public void BeginDrag()
        {
            if (Source.CanEdit)
            {
                Source.RecordEdit(DragUndoKey);
            }
        }

        public void Drag(DragStatus status, int nowFrame, int lastFrame)
        {
            if (!Source.CanEdit)
            {
                return;
            }

            var startFrame = StartFrame;
            var endFrame = EndFrame;
            switch (status)
            {
                case DragStatus.Left:
                    startFrame = nowFrame;
                    break;
                case DragStatus.Right:
                    endFrame = nowFrame;
                    break;
                case DragStatus.Move:
                    var length = DurationFrames;
                    var maxStart = Mathf.Max(0, Source.DurationFrames - length);
                    startFrame = Mathf.Clamp(startFrame + nowFrame - lastFrame, 0, maxStart);
                    endFrame = startFrame + length;
                    break;
            }
            ApplyWindowFrames(startFrame, endFrame, true);
        }

        public void SetFrames(int startFrame, int endFrame, bool save = true)
        {
            if (!Source.CanEdit || (StartFrame == startFrame && EndFrame == endFrame))
            {
                return;
            }
            if (save)
            {
                Source.RecordEdit(FramesUndoKey);
            }
            ApplyWindowFrames(startFrame, endFrame, true);
            if (save)
            {
                CommitEdit();
            }
        }

        /// <summary>提交变更：先校验业务字段，再落盘并让预览重播当前层。</summary>
        public void CommitEdit()
        {
            if (!Source.CanEdit)
            {
                return;
            }
            ValidateWindow();
            Source.Save();
            Source.Run(this);
            Source.NotifyChanged();
        }

        public bool TryAssignAnimation(string assetPath) => false;

        public bool TryAssignAnimation(AnimationClip animation) => false;

        public bool FitAnimationDuration() => false;

        public void RecordUndo(string key) => Source.RecordEdit(key);

        public void Bind(Action callback) => Source.Bind(this, callback);

        public void Unbind(Action callback) => Source.Unbind(this, callback);

        /// <summary>提交前的业务字段校验。窗口不存在时什么也不做。</summary>
        internal void ValidateWindow() => Window?.ValidateData();

        /// <summary>本片段的 Inspector。由片段自己创建，数据源因此不需要认识任何具体片段类型。</summary>
        public abstract TimelineInspectorBase CreateInspector();

        protected virtual string FramesUndoKey => "combat_window_frames";

        internal virtual string DragUndoKey => "combat_drag_window";

        private void ApplyWindowFrames(int startFrame, int endFrame, bool notify)
        {
            if (!Source.CanEdit || Source.DurationFrames <= 0)
            {
                return;
            }

            var window = Window;
            if (window == null)
            {
                return;
            }

            startFrame = Mathf.Clamp(startFrame, 0, Source.DurationFrames - 1);
            endFrame = Mathf.Clamp(endFrame, startFrame + 1, Source.DurationFrames);
            window.StartFrame = startFrame;
            window.EndFrame = endFrame;
            if (notify)
            {
                Source.Run(this);
            }
        }
    }
}
