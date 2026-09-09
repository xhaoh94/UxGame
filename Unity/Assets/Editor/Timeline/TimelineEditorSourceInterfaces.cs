using Assets.Editor.Timeline;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux.Editor.Timeline
{
    public enum TimelineEditorSourceRole
    {
        Presentation,
        Logic,
    }

    /// <summary>
    /// Timeline 编辑器的数据源边界。View 只依赖该接口；Document 可组合多个独立保存的数据源。
    /// </summary>
    public interface ITimelineEditorSource
    {
        string Id { get; }
        TimelineEditorSourceRole Role { get; }
        UnityEngine.Object UndoOwner { get; }
        bool CanEdit { get; }
        bool CanSetFrameRate { get; }
        int FrameRate { get; }
        int DurationFrames { get; }
        int TrackCount { get; }
        IReadOnlyList<ITimelineEditorTrack> Tracks { get; }

        event Action StructureChanged;
        event Action Changed;

        IReadOnlyList<Type> GetTrackTypes();
        string GetTrackDisplayName(Type trackType);
        ITimelineEditorTrack AddTrack(Type trackType);
        bool SetFrameRate(int frameRate);
        TimelineInspectorBase CreateInspector(object selection);
        void Bind(object selection, Action action);
        void Unbind(object selection, Action action);
        void CommitInspectorChange(object assetObject);
        void Save();
        void RefreshAfterUndo();
    }

    public interface ITimelineEditorTrack
    {
        ITimelineEditorSource Source { get; }
        string Id { get; }
        string Name { get; }
        string TypeName { get; }
        string DisplayTypeName { get; }
        Color Color { get; }
        int EndFrame { get; }
        bool CanRename { get; }
        bool CanRemove { get; }
        bool CanCreateClip { get; }
        IReadOnlyList<ITimelineEditorClip> Clips { get; }

        void Rename(string name);
        bool Remove();
        ITimelineEditorClip CreateClip(string assetPath = null);
        bool RemoveClip(ITimelineEditorClip clip);
        bool IsLayoutValid();
        void UpdateMixData();
        void RecordUndo(string key);
        void Bind(Action action);
        void Unbind(Action action);
    }

    public interface ITimelineEditorClip
    {
        ITimelineEditorTrack Track { get; }
        string Id { get; }
        string Name { get; }
        string TypeName { get; }
        int StartFrame { get; }
        int EndFrame { get; }
        int InFrame { get; }
        int OutFrame { get; }
        int DurationFrames { get; }
        bool CanFitAnimationDuration { get; }

        void Rename(string name);
        void BeginDrag();
        void Drag(DragStatus status, int nowFrame, int lastFrame);
        void SetFrames(int startFrame, int endFrame, bool save = true);
        void CommitEdit();
        bool TryAssignAnimation(string assetPath);
        bool TryAssignAnimation(AnimationClip animation);
        bool FitAnimationDuration();
        void RecordUndo(string key);
        void Bind(Action action);
        void Unbind(Action action);
    }
}
