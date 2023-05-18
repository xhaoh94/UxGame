using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Ux
{
    public class UIDialog : UIWindow
    {
        protected override UILayer Layer => UILayer.Normal;
        protected override string PkgName => "Common";

        protected override string ResName => "CommonDialog";

        protected DialogFactory.DialogData dialogData;
        GTextField txtTitle;
        GTextField txtContent;
        GButton btn1;
        GButton btn2;
        Controller dialogState;
        public override void InitData(IUIData data, Action<IUI> remove)
        {
            OnHideCallBack += _Hide;
            base.InitData(data, remove);
        }

        public override void DoShow(bool isAnim, object param)
        {
            dialogData = (DialogFactory.DialogData)param;
            base.DoShow(isAnim, param);
        }
        protected override void OnShow(object param)
        {
            foreach (var (paramType, value) in dialogData.Param)
            {
                switch (paramType)
                {
                    case DialogFactory.ParamType.Title:
                        txtTitle.text = value.ToString();
                        break;
                    case DialogFactory.ParamType.Content:
                        txtContent.text = value.ToString();
                        break;
                    case DialogFactory.ParamType.Btn1Title:
                        btn1.text = value.ToString();
                        break;
                    case DialogFactory.ParamType.Btn1Fn:
                        AddClick(btn1, OnBtn1Click);
                        break;
                    case DialogFactory.ParamType.Btn2Title:
                        btn2.text = value.ToString();
                        break;
                    case DialogFactory.ParamType.Btn2Fn:
                        AddClick(btn2, OnBtn2Click);
                        break;
                    case DialogFactory.ParamType.Custom:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            

            switch (dialogData.DType)
            {
                case DialogFactory.DialogType.SingleBtn:
                    dialogState.selectedPage = "btn1";
                    break;
                case DialogFactory.DialogType.DoubleBtn:
                    dialogState.selectedPage = "btn2";
                    break;
                case DialogFactory.DialogType.Custom:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        void OnBtn1Click()
        {
            if (dialogData.Param.TryGetValue(DialogFactory.ParamType.Btn1Fn, out var obj))
            {
                (obj as Action)?.Invoke();
            }
            Hide();
        }
        void OnBtn2Click()
        {
            if (dialogData.Param.TryGetValue(DialogFactory.ParamType.Btn2Fn, out var obj))
            {
                (obj as Action)?.Invoke();
            }
            Hide();
        }
        protected override void OnLayout()
        {
            SetLayout(UILayout.Center_Middle);
        }
        private void _Hide()
        {
            dialogData.HideCallBack?.Invoke(this);
        }
    }
    public class DialogFactory
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
        public readonly Dictionary<string, IUI> _waitDels = new Dictionary<string, IUI>();
        readonly Dictionary<Type, Queue<string>> _pool = new Dictionary<Type, Queue<string>>();
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
                    ids = new Queue<string>();
                    _pool.Add(type, ids);
                }
                ids.Enqueue(mb.ID);
            }
        }
        public void Clear()
        {
            _pool.Clear();
        }
        string __GetTypeUIID(Type type, string[] pkgs, string[] lazyloads)
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

            var id = $"{type.Name}_{IDGenerater.GenerateId()}";
            var data = new UIData(id, type, pkgs, lazyloads);
            UIMgr.Ins.RegisterUI(data);
            return id;
        }
        public void SingleBtn<T>(string[] pkgs, string title, string content, string btn1Title, Action btn1Fn = null) where T : UIDialog
        {
            SingleBtn<T>(pkgs, null, title, content, btn1Title, btn1Fn);
        }
        public void SingleBtn<T>(string[] pkgs, string[] lazyloads, string title, string content, string btn1Title, Action btn1Fn = null) where T : UIDialog
        {
            var id = __GetTypeUIID(typeof(T), pkgs, lazyloads);
            var mbData = new DialogData(_Hide, DialogType.SingleBtn);
            mbData.Param.Add(ParamType.Title, title);
            mbData.Param.Add(ParamType.Content, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            UIMgr.Ins.Show(id, mbData);
        }
        public void SingleBtn(string title, string content, string btn1Title, Action btn1Fn = null)
        {
            SingleBtn<UIDialog>(new[] { "Common" }, title, content, btn1Title, btn1Fn);
        }


        public void DoubleBtn<T>(string[] pkgs, string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn) where T : UIDialog
        {
            DoubleBtn<T>(pkgs, null, title, content, btn1Title, btn1Fn, btn2Title, btn2Fn);
        }
        public void DoubleBtn<T>(string[] pkgs, string[] lazyloads, string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn) where T : UIDialog
        {
            var id = __GetTypeUIID(typeof(T), pkgs, lazyloads);
            var mbData = new DialogData(_Hide, DialogType.DoubleBtn);
            mbData.Param.Add(ParamType.Title, title);
            mbData.Param.Add(ParamType.Content, content);
            mbData.Param.Add(ParamType.Btn1Title, btn1Title);
            mbData.Param.Add(ParamType.Btn1Fn, btn1Fn);
            mbData.Param.Add(ParamType.Btn2Title, btn2Title);
            mbData.Param.Add(ParamType.Btn2Fn, btn2Fn);
            UIMgr.Ins.Show(id, mbData);
        }
        public void DoubleBtn(string title, string content, string btn1Title, Action btn1Fn, string btn2Title, Action btn2Fn)
        {
            DoubleBtn<UIDialog>(new[] { "Common" }, title, content, btn1Title, btn1Fn, btn2Title, btn2Fn);
        }

        public void Custom<T>(string[] pkgs, object param)
        {
            Custom<T>(pkgs, null, param);
        }
        public void Custom<T>(string[] pkgs, string[] lazyloads, object param)
        {
            var id = __GetTypeUIID(typeof(T), pkgs, lazyloads);
            var mbData = new DialogData(_Hide, DialogType.Custom);
            mbData.Param.Add(ParamType.Custom, param);
            UIMgr.Ins.Show(id, mbData);
        }
        public void ShowCustom(object param)
        {
            Custom<UIDialog>(new[] { "Common" }, param);
        }
    }
}
