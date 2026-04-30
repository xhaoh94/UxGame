using System.Collections.Generic;
using System;

namespace Ux
{
    public partial class UIMgr
    {
        private readonly List<int> _hideAllIdBuffer = new List<int>();

        /// <summary>
        /// 隐藏所有界面（支持忽略列表）
        /// </summary>
        /// <param name="ignoreSet">需要忽略的界面ID集合，为null时隐藏所有</param>
        void _HideAll(HashSet<int> ignoreSet)
        {
            _stackHandler.Clear();
            
            _hideAllIdBuffer.Clear();
            
            // 收集所有需要处理的ID（包括showing和showed）
            foreach (var (id,_) in _pendingShows)
            {
                _hideAllIdBuffer.Add(id);
            }
            foreach (var kv in _showed)
            {
                _hideAllIdBuffer.Add(kv.Key);
            }
            
            // 批量处理隐藏
            int cnt = _hideAllIdBuffer.Count;
            for (int i = 0; i < cnt; i++)
            {
                var id = _hideAllIdBuffer[i];
                if (ignoreSet != null && ignoreSet.Contains(id)) continue;
                Hide(id, false);
            }
            
            _hideAllIdBuffer.Clear();
        }


        public void HideAll()
        {
            _HideAll(null);
        }

        public void HideAll(IList<int> ignoreList = null)
        {
            if (ignoreList == null || ignoreList.Count == 0)
            {
                _HideAll(null);
                return;
            }

            _ignoreSet ??= new HashSet<int>();
            _ignoreSet.Clear();
            int cnt = ignoreList.Count;
            for (int i = 0; i < cnt; i++)
            {
                _ignoreSet.Add(ignoreList[i]);
            }
            _HideAll(_ignoreSet);
        }

        public void HideAll(IList<Type> ignoreList = null)
        {
            if (ignoreList == null || ignoreList.Count == 0)
            {
                _HideAll(null);
                return;
            }

            _ignoreSet ??= new HashSet<int>();
            _ignoreSet.Clear();
            int cnt = ignoreList.Count;
            for (int i = 0; i < cnt; i++)
            {
                _ignoreSet.Add(ConverterID(ignoreList[i]));
            }
            _HideAll(_ignoreSet);
        }

        public void Hide<T>(bool isAnim = true) where T : UIBase
        {
            Hide(ConverterID(typeof(T)), isAnim);
        }
        
        public void Hide(int id, bool isAnim = true)
        {
            _Hide(id, isAnim, true);
        }

        public void HideNotStack<T>(bool isAnim = true) where T : UIBase
        {
            HideNotStack(ConverterID(typeof(T)), isAnim);
        }
        
        public void HideNotStack(int id, bool isAnim = true)
        {
            _Hide(id, isAnim, false);
        }

        void _Hide(int id, bool isAnim, bool checkStack)
        {
            if (_pendingShows.ContainsKey(id))
            {
                _cacheHandler.CreatedDels.Add(id);
                return;
            }

            if (!_showed.TryGetValue(id, out IUI ui))
            {
                return;
            }

            if (ui.State == UIState.HideAnim || ui.State == UIState.Hide)
            {
                return;
            }

            var parentID = ui.Data.GetParentID();
            if (parentID != id)
            {
                _Hide(parentID, isAnim, checkStack);
                return;
            }

            if (!checkStack && _stackHandler.UIStacks.Count > 0 && ui.Type == UIType.Stack)
            {
                _stackHandler.Clear();
            }
            ui.DoHide(isAnim, checkStack);
        }

        private void _HideCallback(IUI ui)
        {
            var id = ui.ID;
            _showed.Remove(id);
            _cacheHandler.CheckDestroy(ui);
#if UNITY_EDITOR
            __Debugger_Showed_Event();
#endif
            EventMgr.Ins.Run(MainEventType.UI_HIDE, id);
            EventMgr.Ins.Run(MainEventType.UI_HIDE, ui.GetType());

            _blurHandler.OnHide(ui);
        }

        private void Dispose(IUI ui)
        {
            var id = ui.ID;
            ui.Dispose();
            var data = GetUIData(id);
            if (data == null) return;
            if (data.Pkgs == null || data.Pkgs.Length == 0) return;
            ResMgr.Ins.RemoveUIPackage(data.Pkgs);
            if (ui is UIDialog)
            {
                RemoveUIData(id);
            }
        }
    }
}