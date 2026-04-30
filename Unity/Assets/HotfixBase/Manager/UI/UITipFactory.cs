using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class UITipFactory : UIBaseFactory<UITip>
    {
        public struct TipData
        {
            public TipData(Action<UITip> _showFn, Action<UITip> _hideFn, string content)
            {
                ShowCallBack = _showFn;
                HideCallBack = _hideFn;
                Content = content;
            }
            public Action<UITip> HideCallBack { get; }
            public Action<UITip> ShowCallBack { get; }
            public string Content { get; }
        }

        public void Show(string content)
        {
            Show(GetDefaultID(), content);
        }

        public void Show<T>(string content) where T : UITip
        {
            var id = GetUIID(typeof(T));
            Show(id, content);
        }

        void Show(int id, string content)
        {
            if (!CheckDefault(id))
            {
                Log.Error("没有指定Tip面板,请检查是否已初始化SetDefaultType");
                return;
            }
            CheckPos();            
            UIMgr.Ins.Create(id).SetParam(IUIParam.Create(new TipData(OnShow, OnHide, content))).Show();
        }

        void CheckPos()
        {
            foreach (var id in _showed)
            {
                var ui = UIMgr.Ins.GetUI<UITip>(id);
                if (ui == null) continue;
                var gobj = ui.GObject;
                gobj.SetPosition(gobj.x, gobj.y - 30, 0);
            }
        }
    }
}