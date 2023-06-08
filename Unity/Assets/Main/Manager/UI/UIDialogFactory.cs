using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ux
{
    public class UIDialogFactory
    {
        public enum ParamType
        {
            Title,
            Content,
            Btn1Title,
            Btn1Fn,
            Btn2Title,
            Btn2Fn,
            Custom,
        }
        public enum DialogType
        {
            SingleBtn,
            DoubleBtn,
            Custom
        }
        public struct DialogData
        {
            public DialogData(Action<UIDialog> _closeFn, DialogType _boxType)
            {
                HideCallBack = _closeFn;
                DType = _boxType;
                Param = new Dictionary<ParamType, object>();
            }
            public Action<UIDialog> HideCallBack { get; }
            public DialogType DType { get; }
            public Dictionary<ParamType, object> Param { get; }
        }
        public readonly Dictionary<int, IUI> _waitDels = new Dictionary<int, IUI>();
        readonly Dictionary<Type, Queue<int>> _pool = new Dictionary<Type, Queue<int>>();
        void _Hide(UIDialog mb)
        {
            if (mb.IsDestroy)
            {
                _waitDels.Add(mb.ID, mb);
            }
            else
            {
                var type = mb.GetType();
                if (!_pool.TryGetValue(type, out var ids))
                {
                    ids = new Queue<int>();
                    _pool.Add(type, ids);
                }
                ids.Enqueue(mb.ID);
            }
        }
        public void Clear()
        {
            _pool.Clear();
        }
        int _GetTypeUIID(Type type)
        {
            if (_pool.TryGetValue(type, out var ids) && ids.Count > 0)
            {
                return ids.Dequeue();
            }

            if (_waitDels.Count > 0)
            {
                var keys = _waitDels.Keys.ToList();
                foreach (var key in keys)
                {
                    if (!_waitDels.TryGetValue(key, out var ui) || ui.GetType() != type) continue;
                    _waitDels.Remove(key);
                    return key;
                }
            }

            var data = new UIData((int)IDGenerater.GenerateId(), type);
            UIMgr.Ins.RegisterUI(data);
            return data.ID;
        }

        Type _defalutType;
        public void SetDefalutType<T>() where T : UIDialog
        {
            _defalutType = typeof(T);
        }
        int GetDefalutID()
        {
            Type type = _defalutType;
            if (type == null)
            {
                type = typeof(UIDialog);
            }
            var id = _GetTypeUIID(type);
            return id;
        }
        public void SingleBtn(string title, string content, string btn1Title, Action btn1Fn = null)
        {
            SingleBtn(GetDefalutID(), title, content, btn1Title, btn1Fn);
        }
        public void SingleBtn<T>(string title, string content, string btn1Title, Action btn1Fn = null) where T : UIDialog
        {
            var id = _GetTypeUIID(typeof(T));
            SingleBtn(id, title, content, btn1Title, btn1Fn);
        }
        public void SingleBtn(int id, string title, string content, string btn1Title, Action btn1Fn = null)
        {
            var mbData = new DialogData(_Hide, DialogType.SingleBtn);
            mbData.Param.Add(ParamType.Title, title);
            mbData.Param.Add(ParamType.Content, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            UIMgr.Ins.Show(id, mbData);
        }
        public void DoubleBtn(string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn)
        {
            DoubleBtn(GetDefalutID(), title, content, btn1Title, btn1Fn, btn2Title, btn2Fn);
        }
        public void DoubleBtn<T>(string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn) where T : UIDialog
        {
            var id = _GetTypeUIID(typeof(T));
            DoubleBtn(id, title, content, btn1Title, btn1Fn, btn2Title, btn2Fn);
        }
        void DoubleBtn(int id, string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn)
        {
            var mbData = new DialogData(_Hide, DialogType.DoubleBtn);
            mbData.Param.Add(ParamType.Title, title);
            mbData.Param.Add(ParamType.Content, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            mbData.Param.Add(ParamType.Btn2Title, btn2Title);
            mbData.Param.Add(ParamType.Btn2Fn, btn2Fn);
            UIMgr.Ins.Show(id, mbData);
        }
    }
}