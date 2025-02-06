using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor
{
    struct UndoData
    {
        public string Key;
        public Action Action;
        public UndoData(string key, Action action)
        {
            Key = key;
            Action = action;
        }
    }
    public class UxUndo
    {
        public UxUndo()
        {
            //Undo.ClearUndo();
            Undo.undoRedoEvent += UndoRedoEventCallBack;
        }
        Stack<UndoData> undoDatas = new Stack<UndoData>();
        Stack<UndoData> redoDatas = new Stack<UndoData>();

        public void RegUndo(string key ,UnityEngine.Object obj, Action action)
        {            
            Undo.RecordObject(obj, key);            
            undoDatas.Push(new UndoData(key, action));
        }
        void UndoRedoEventCallBack(in UndoRedoInfo undo)
        {
            if (undo.isRedo)
            {
                if (redoDatas.Count > 0)
                {
                    var data = redoDatas.Pop();
                    undoDatas.Push(data);
                    data.Action.Invoke();
                }
                Log.Info("redo");
            }
            else
            {
                if (undoDatas.Count > 0)
                {
                    var data = undoDatas.Pop();
                    redoDatas.Push(data);
                    data.Action.Invoke();
                }
                Log.Info("undo");
            }
        }
    }
}