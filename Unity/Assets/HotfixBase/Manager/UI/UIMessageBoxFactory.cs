using System;
using System.Collections.Generic;
using System.Linq;

namespace Ux
{
    public class UIMessageBoxFactory
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

        public struct MessageBoxCheckBox
        {
            public string Tag { get; }
            public string Desc { get; }
            public MessageBoxCheckBox(string tag, string desc)
            {
                Tag = tag;
                Desc = desc;
            }
        }
        public struct MessageBoxData
        {
            public MessageBoxData(Action<UIMessageBox> _showFn, Action<UIMessageBox> _hideFn, Action<string> _pushTag)
            {
                ShowCallBack = _showFn;
                HideCallBack = _hideFn;
                PushTagCallBack = _pushTag;
                Param = new Dictionary<ParamType, object>();
            }
            public Action<UIMessageBox> ShowCallBack { get; }
            public Action<UIMessageBox> HideCallBack { get; }
            public Action<string> PushTagCallBack { get; }
            public Dictionary<ParamType, object> Param { get; }
        }
        public readonly Dictionary<int, IUI> _waitDels = new Dictionary<int, IUI>();
        readonly Dictionary<Type, Queue<int>> _pool = new Dictionary<Type, Queue<int>>();
        readonly HashSet<int> _showed = new HashSet<int>();
        HashSet<string> _tags = new HashSet<string>();
        void _PushTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }
            _tags.Add(tag);
        }
        void _Show(UIMessageBox box)
        {
            _showed.Add(box.ID);
        }
        void _Hide(UIMessageBox box)
        {
            _showed.Remove(box.ID);
            if (box.HideDestroyTime >= 0)
            {
                _waitDels.Add(box.ID, box);
            }
            else
            {
                var type = box.GetType();
                if (!_pool.TryGetValue(type, out var ids))
                {
                    ids = new Queue<int>();
                    _pool.Add(type, ids);
                }
                ids.Enqueue(box.ID);
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
        public void SetDefalutType<T>() where T : UIMessageBox
        {
            _defalutType = typeof(T);
        }
        int _GetDefalutID()
        {
            Type type = _defalutType;
            if (type == null)
            {
                return 0;
            }
            var id = _GetTypeUIID(type);
            return id;
        }
        bool _CheckDefalut(int id)
        {
            if (id == 0)
            {
                Log.Error("没有指定Dialog面板,请检查是否已初始化SetDefalutType");
                return false;
            }
            return true;
        }
        public void SingleBtn(string title, string content, string btn1Title, Action btn1Fn)
        {
            _SingleBtn(_GetDefalutID(), title, content, btn1Title, btn1Fn);
        }
        public void SingleBtn<T>(string title, string content, string btn1Title, Action btn1Fn) where T : UIMessageBox
        {
            var id = _GetTypeUIID(typeof(T));
            _SingleBtn(id, title, content, btn1Title, btn1Fn);
        }
        void _SingleBtn(int id, string title, string content, string btn1Title, Action btn1Fn)
        {
            if (!_CheckDefalut(id))
            {
                Log.Error("没有指定Dialog面板,请检查是否已初始化SetDefalutType");
                return;
            }
            var mbData = CreateData(title, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            _Show(id, mbData);
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
        public void SingleBtnCheckBox(string tag, string checkboxContent, string title, string content, string btn1Title, Action btn1Fn)
        {
            if (_CheckBox(tag))
            {
                btn1Fn?.Invoke();
                return;
            }
            _SingleBtnCheckBox(_GetDefalutID(), title, content, btn1Title, btn1Fn, tag, checkboxContent);
        }
        public void SingleBtnCheckBox<T>(string tag, string checkboxContent, string title, string content, string btn1Title, Action btn1Fn) where T : UIMessageBox
        {
            if (_CheckBox(tag))
            {
                btn1Fn?.Invoke();
                return;
            }
            var id = _GetTypeUIID(typeof(T));
            _SingleBtnCheckBox(id, title, content, btn1Title, btn1Fn, tag, checkboxContent);
        }
        void _SingleBtnCheckBox(int id, string title, string content, string btn1Title, Action btn1Fn, string tag, string desc)
        {
            if (!_CheckDefalut(id))
            {
                Log.Error("没有指定Dialog面板,请检查是否已初始化SetDefalutType");
                return;
            }
            var mbData = CreateData(title, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            mbData.Param.Add(ParamType.ChcekBox, new MessageBoxCheckBox(tag, desc));
            _Show(id, mbData);
        }
        public void DoubleBtn(string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn)
        {
            _DoubleBtn(_GetDefalutID(), title, content, btn1Title, btn1Fn, btn2Title, btn2Fn);
        }
        public void DoubleBtn<T>(string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn) where T : UIMessageBox
        {
            var id = _GetTypeUIID(typeof(T));
            _DoubleBtn(id, title, content, btn1Title, btn1Fn, btn2Title, btn2Fn);
        }
        void _DoubleBtn(int id, string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn)
        {
            if (!_CheckDefalut(id))
            {
                Log.Error("没有指定Dialog面板,请检查是否已初始化SetDefalutType");
                return;
            }
            var mbData = CreateData(title, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            mbData.Param.Add(ParamType.Btn2Title, btn2Title);
            mbData.Param.Add(ParamType.Btn2Fn, btn2Fn);
            _Show(id, mbData);
        }
        public void DoubleBtnCheckBox(string tag, string checkboxContent, string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn)
        {
            if (_CheckBox(tag))
            {
                btn1Fn?.Invoke();
                return;
            }
            _DoubleBtnCheckBox(_GetDefalutID(), title, content, btn1Title, btn1Fn, btn2Title, btn2Fn, tag, checkboxContent);
        }
        public void DoubleBtnCheckBox<T>(string tag, string checkboxContent, string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn) where T : UIMessageBox
        {
            if (_CheckBox(tag))
            {
                btn1Fn?.Invoke();
                return;
            }
            var id = _GetTypeUIID(typeof(T));
            _DoubleBtnCheckBox(id, title, content, btn1Title, btn1Fn, btn2Title, btn2Fn, tag, checkboxContent);
        }
        void _DoubleBtnCheckBox(int id, string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn, string tag, string desc)
        {
            var mbData = CreateData(title, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            mbData.Param.Add(ParamType.Btn2Title, btn2Title);
            mbData.Param.Add(ParamType.Btn2Fn, btn2Fn);
            mbData.Param.Add(ParamType.ChcekBox, new MessageBoxCheckBox(tag, desc));
            _Show(id, mbData);
        }
        void _Show(int id, MessageBoxData mbData)
        {
            UIMgr.Ins.Show(id,IUIParam.Create(mbData));
        }
        public MessageBoxData CreateData(string title, string content)
        {
            var mbData = new MessageBoxData(_Show, _Hide, _PushTag);
            mbData.Param.Add(ParamType.Title, title);
            mbData.Param.Add(ParamType.Content, content);
            return mbData;
        }
    }
}