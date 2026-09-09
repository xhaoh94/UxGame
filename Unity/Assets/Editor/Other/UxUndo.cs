using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor
{
    readonly struct UndoData
    {
        public UndoData(string key, UnityEngine.Object owner, Action action)
        {
            Key = key;
            Owner = owner;
            Action = action;
        }

        public string Key { get; }
        public UnityEngine.Object Owner { get; }
        public Action Action { get; }
    }

    public sealed class UxUndo : IDisposable
    {
        readonly Dictionary<int, UndoData> records = new();
        int pendingGroup = -1;

        public UxUndo()
        {
            Undo.undoRedoEvent += UndoRedoEventCallBack;
        }

        public void RegUndo(string key, UnityEngine.Object obj, Action action)
        {
            if (obj == null)
            {
                return;
            }

            // 每次编辑占用独立 group，UndoRedoInfo 才能精确定位回调；
            // 不能在任意全局 Undo 时简单弹出本窗口的私有栈。
            CompleteUndo();
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(key);
            Undo.RecordObject(obj, key);
            // 不能在调用方真正修改对象之前 Flush/Collapse；否则 Unity 会把尚无差异的记录丢弃。
            // 调用方完成本次修改后必须调用 CompleteUndo，再推进到下一组。
            records[group] = new UndoData(key, obj, action);
            pendingGroup = group;
        }

        public void CompleteUndo()
        {
            if (pendingGroup < 0)
            {
                return;
            }

            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();
            pendingGroup = -1;
        }

        public void Dispose()
        {
            CompleteUndo();
            Undo.undoRedoEvent -= UndoRedoEventCallBack;
            records.Clear();
        }

        void UndoRedoEventCallBack(in UndoRedoInfo undo)
        {
            if (!records.TryGetValue(undo.undoGroup, out var data) || data.Owner == null)
            {
                return;
            }

            data.Action?.Invoke();
            Log.Info(undo.isRedo ? $"redo: {data.Key}" : $"undo: {data.Key}");
        }
    }
}
