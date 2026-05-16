using System.Collections.Generic;

namespace Ux
{
    internal class UICacheHandler
    {
        private readonly IUICacheHandlerCallback _callback;
        private readonly Dictionary<int, IUI> _cache = new Dictionary<int, IUI>();
        private readonly Dictionary<int, UIMgr.WaitDel> _waitDels = new Dictionary<int, UIMgr.WaitDel>();

        internal UICacheHandler(IUICacheHandlerCallback callback)
        {
            _callback = callback;
        }

        internal bool TryTakeCached(int id, out IUI ui)
        {
            if (_cache.TryGetValue(id, out ui))
            {
                _cache.Remove(id);
#if UNITY_EDITOR
                UIMgr.__Debugger_Cacel_Event();
#endif
                return true;
            }

            if (_waitDels.TryGetValue(id, out var waitDel))
            {
                waitDel.GetUI(out ui);
                _waitDels.Remove(id);
#if UNITY_EDITOR
                UIMgr.__Debugger_WaitDel_Event();
#endif
                return true;
            }

            ui = null;
            return false;
        }

        // 缓存只处理隐藏后的实例生命周期；请求取消由UIMgr版本处理。
        internal void TrackHidden(UIMgr.UIRecord record)
        {
            if (record?.UI == null)
            {
                return;
            }

            record.CacheState = UIMgr.CacheState.None;
            record.WaitDel = null;
            TrackHidden(record.UI, record.Id, record);
        }

        internal void TrackHidden(IUI ui, int id)
        {
            TrackHidden(ui, id, null);
        }

        private void TrackHidden(IUI ui, int id, UIMgr.UIRecord record)
        {
            if (ui == null)
            {
                return;
            }

            // 先清掉同 id 的旧缓存/旧延迟销毁，避免旧实例在新实例入缓存后继续存活并反向销毁。
            RemoveCachedEntry(id, ui);
            RemoveWaitDelEntry(id, ui);

            if (ui.HideDestroyTime < 0)
            {
                _cache[id] = ui;
                if (record != null)
                {
                    record.CacheState = UIMgr.CacheState.Cached;
                }
#if UNITY_EDITOR
                UIMgr.__Debugger_Cacel_Event();
#endif
                return;
            }

            var wd = Pool.Get<UIMgr.WaitDel>();
            wd.Init(ui, OnRemoveWaitDel, OnDisposeWaitDel);
            _waitDels[id] = wd;
            if (record != null)
            {
                record.WaitDel = wd;
                record.CacheState = UIMgr.CacheState.WaitDestroy;
            }
#if UNITY_EDITOR
            UIMgr.__Debugger_WaitDel_Event();
#endif
        }

        private void RemoveCachedEntry(int id, IUI keep)
        {
            if (!_cache.TryGetValue(id, out var cached))
            {
                return;
            }

            _cache.Remove(id);
            if (!ReferenceEquals(cached, keep))
            {
                _callback.DisposeUI(cached);
            }
#if UNITY_EDITOR
            UIMgr.__Debugger_Cacel_Event();
#endif
        }

        private void RemoveWaitDelEntry(int id, IUI keep)
        {
            if (!_waitDels.TryGetValue(id, out var waitDel))
            {
                return;
            }

            _waitDels.Remove(id);
            waitDel.GetUI(out var oldUI);
            if (!ReferenceEquals(oldUI, keep))
            {
                _callback.DisposeUI(oldUI);
            }
#if UNITY_EDITOR
            UIMgr.__Debugger_WaitDel_Event();
#endif
        }

        internal void RemoveRecord(UIMgr.UIRecord record)
        {
            if (record == null)
            {
                return;
            }

            _cache.Remove(record.Id);
            if (_waitDels.TryGetValue(record.Id, out var waitDel))
            {
                _waitDels.Remove(record.Id);
                if (!ReferenceEquals(record.WaitDel, waitDel))
                {
                    waitDel.Dispose();
                }
            }

            record.WaitDel = null;
            record.CacheState = UIMgr.CacheState.None;
        }

        internal void ClearMemory()
        {
            if (_cache.Count > 0)
            {
                var cachedUIs = Pool.Get<List<IUI>>();
                foreach (var kv in _cache)
                {
                    cachedUIs.Add(kv.Value);
                }
                _cache.Clear();
                for (int i = 0; i < cachedUIs.Count; i++)
                {
                    _callback.DisposeUI(cachedUIs[i]);
                }
                cachedUIs.Clear();
                Pool.Push(cachedUIs);
            }

            if (_waitDels.Count == 0)
            {
                return;
            }

            var keys = Pool.Get<List<int>>();
            keys.AddRange(_waitDels.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                if (_waitDels.TryGetValue(keys[i], out var wd))
                {
                    wd.Dispose();
                }
            }
            keys.Clear();
            Pool.Push(keys);
        }

        internal void FillCacheDebugNames(List<string> output)
        {
            output.Clear();
            foreach (var kv in _cache)
            {
                output.Add(kv.Value.Name);
            }
        }

        internal void FillWaitDestroyDebugNames(List<string> output)
        {
            output.Clear();
            foreach (var kv in _waitDels)
            {
                output.Add(kv.Value.Name);
            }
        }

        void OnRemoveWaitDel(int id)
        {
            _waitDels.Remove(id);
#if UNITY_EDITOR
            UIMgr.__Debugger_WaitDel_Event();
#endif
        }

        void OnDisposeWaitDel(IUI ui)
        {
            var record = UIMgr.Ins.GetRecord(ui.ID);
            if (record != null)
            {
                record.UI = null;
                record.Phase = UIMgr.UIPhase.Disposed;
                record.WaitDel = null;
                record.CacheState = UIMgr.CacheState.None;
                record.CurrentChildId = record.ParentRootId;
                UIMgr.Ins.RemoveRecord(ui.ID);
            }
            _callback.DisposeUI(ui);
        }
    }
}
