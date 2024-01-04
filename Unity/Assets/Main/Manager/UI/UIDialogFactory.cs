using System;
using System.Collections.Generic;
using System.Linq;

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
            ChcekBox,
            Custom,
        }
        public enum DialogType
        {
            SingleBtn,
            DoubleBtn,
            Custom
        }
        public struct DialogCheckBox
        {
            public string Tag { get; }
            public string Desc { get; }
            public DialogCheckBox(string tag, string desc)
            {
                Tag = tag;
                Desc = desc;
            }
        }
        public struct DialogData
        {
            public DialogData(Action<UIDialog> _closeFn, Action<string> _pushTag, DialogType _boxType)
            {
                HideCallBack = _closeFn;
                PushTagCallBack = _pushTag;
                DType = _boxType;
                Param = new Dictionary<ParamType, object>();
            }
            public Action<UIDialog> HideCallBack { get; }
            public Action<string> PushTagCallBack { get; }
            public DialogType DType { get; }
            public Dictionary<ParamType, object> Param { get; }
        }
        public readonly Dictionary<int, IUI> _waitDels = new Dictionary<int, IUI>();
        readonly Dictionary<Type, Queue<int>> _pool = new Dictionary<Type, Queue<int>>();

        HashSet<string> _tags = new HashSet<string>();
        void _PushTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }
            _tags.Add(tag);
        }
        void _Hide(UIDialog dialog)
        {
            if (dialog.IsDestroy)
            {
                _waitDels.Add(dialog.ID, dialog);
            }
            else
            {
                var type = dialog.GetType();
                if (!_pool.TryGetValue(type, out var ids))
                {
                    ids = new Queue<int>();
                    _pool.Add(type, ids);
                }
                ids.Enqueue(dialog.ID);
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
            UIMgr.Ins.AddUIData(data);
            return data.ID;
        }

        Type _defalutType;
        public void SetDefalutType<T>() where T : UIDialog
        {
            _defalutType = typeof(T);
        }
        int _GetDefalutID()
        {
            Type type = _defalutType;
            if (type == null)
            {
                type = typeof(UIDialog);
            }
            var id = _GetTypeUIID(type);
            return id;
        }
        public void SingleBtn(string title, string content, string btn1Title, Action btn1Fn)
        {
            _SingleBtn(_GetDefalutID(), title, content, btn1Title, btn1Fn);
        }
        public void SingleBtn<T>(string title, string content, string btn1Title, Action btn1Fn) where T : UIDialog
        {
            var id = _GetTypeUIID(typeof(T));
            _SingleBtn(id, title, content, btn1Title, btn1Fn);
        }
        void _SingleBtn(int id, string title, string content, string btn1Title, Action btn1Fn)
        {
            var mbData = CreateDialogData(title, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            UIMgr.Ins.Show(id, mbData);
        }
        bool _CheckBox(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                Log.Error("Dialog CheckBox Tag Empty");
                return true;
            }
            if (_tags.Contains(tag))
            {
                return true;
            }
            return false;
        }
        public void SingleBtnCheckBox(string title, string content, string btn1Title, Action btn1Fn, string tag, string desc)
        {
            if (_CheckBox(tag))
            {
                btn1Fn?.Invoke();
                return;
            }
            _SingleBtnCheckBox(_GetDefalutID(), title, content, btn1Title, btn1Fn, tag, desc);
        }
        public void SingleBtnCheckBox<T>(string title, string content, string btn1Title, Action btn1Fn, string tag, string desc) where T : UIDialog
        {
            if (_CheckBox(tag))
            {
                btn1Fn?.Invoke();
                return;
            }
            var id = _GetTypeUIID(typeof(T));
            _SingleBtnCheckBox(id, title, content, btn1Title, btn1Fn, tag, desc);
        }
        void _SingleBtnCheckBox(int id, string title, string content, string btn1Title, Action btn1Fn, string tag, string desc)
        {
            var mbData = CreateDialogData(title, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            mbData.Param.Add(ParamType.ChcekBox, new DialogCheckBox(tag, desc));
            UIMgr.Ins.Show(id, mbData);
        }
        public void DoubleBtn(string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn)
        {
            _DoubleBtn(_GetDefalutID(), title, content, btn1Title, btn1Fn, btn2Title, btn2Fn);
        }
        public void DoubleBtn<T>(string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn) where T : UIDialog
        {
            var id = _GetTypeUIID(typeof(T));
            _DoubleBtn(id, title, content, btn1Title, btn1Fn, btn2Title, btn2Fn);
        }
        void _DoubleBtn(int id, string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn)
        {
            var mbData = CreateDialogData(title, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            mbData.Param.Add(ParamType.Btn2Title, btn2Title);
            mbData.Param.Add(ParamType.Btn2Fn, btn2Fn);
            UIMgr.Ins.Show(id, mbData);
        }
        public void DoubleBtnCheckBox(string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn, string tag, string desc)
        {
            if (_CheckBox(tag))
            {
                btn1Fn?.Invoke();
                return;
            }
            _DoubleBtnCheckBox(_GetDefalutID(), title, content, btn1Title, btn1Fn, btn2Title, btn2Fn, tag, desc);
        }
        public void DoubleBtnCheckBox<T>(string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn, string tag, string desc) where T : UIDialog
        {
            if (_CheckBox(tag))
            {
                btn1Fn?.Invoke();
                return;
            }
            var id = _GetTypeUIID(typeof(T));
            _DoubleBtnCheckBox(id, title, content, btn1Title, btn1Fn, btn2Title, btn2Fn, tag, desc);
        }
        void _DoubleBtnCheckBox(int id, string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn, string tag, string desc)
        {
            var mbData = CreateDialogData(title, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            mbData.Param.Add(ParamType.Btn2Title, btn2Title);
            mbData.Param.Add(ParamType.Btn2Fn, btn2Fn);
            mbData.Param.Add(ParamType.ChcekBox, new DialogCheckBox(tag, desc));
            UIMgr.Ins.Show(id, mbData);
        }
        public DialogData CreateDialogData(string title, string content)
        {
            var mbData = new DialogData(_Hide, _PushTag, DialogType.DoubleBtn);
            mbData.Param.Add(ParamType.Title, title);
            mbData.Param.Add(ParamType.Content, content);
            return mbData;
        }
    }
}