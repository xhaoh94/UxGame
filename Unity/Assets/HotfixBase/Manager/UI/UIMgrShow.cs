using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;

namespace Ux
{
    public partial class UIMgr
    {
        /// <summary>
        /// 创建UI构建器
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public UIShowBuilder Create(int id = 0)
        {
            var builder = Pool.Get<UIShowBuilder>();
            builder.SetId(id);
            return builder;
        }

        private async UniTask<T> _ShowAsync<T>(int id, IUIParam param, bool isAnim, bool checkStack) where T : IUI
        {
            var data = GetUIData(id);
            if (data == null)
            {
                return default;
            }

            var childID = data.GetChildID();
            if (_CheckDownload(childID, param, isAnim))
            {
                return default;
            }

            if (_cacheHandler.CreatedDels.Contains(id))
            {
                _cacheHandler.CreatedDels.Remove(id);
            }

            var uis = Pool.Get<List<IUI>>();
            var succ = await _ShowAsync(childID, uis);
            if (succ)
            {
                for (int i = 0; i < uis.Count; i++)
                {
                    var uiid = uis[i].ID;
                    if (_cacheHandler.CreatedDels.Contains(uiid))
                    {
                        succ = false;
                        _cacheHandler.CreatedDels.Remove(uiid);
                    }
                }
            }

            foreach (var ui in uis)
            {
                var uiid = ui.ID;
                if (!succ)
                {
                    // 只有当界面不是已显示状态时才销毁
                    // 避免父界面已显示但子界面创建失败时错误销毁父界面
                    if (!_showed.ContainsKey(uiid))
                    {
                        _cacheHandler.CheckDestroy(ui);
                    }
                    _FinishPendingShow(uiid, null);
                    continue;
                }
                await ui.DoShow(isAnim, id, uiid == id ? param : null, checkStack);

                if (_showed.ContainsKey(uiid))
                {
                    continue;
                }
                _showed.Add(uiid, ui);
                _FinishPendingShow(uiid, ui);
            }

            uis.Clear();
            Pool.Push(uis);

#if UNITY_EDITOR
            __Debugger_Showing_Event();
            __Debugger_Showed_Event();
#endif
            return succ ? (T)_showed[id] : default;
        }

        private void _FinishPendingShow(int id, IUI ui)
        {
            if (_pendingShows.TryGetValue(id, out var tcs))
            {
                tcs.TrySetResult(ui);
                _pendingShows.Remove(id);
            }
        }

        private async UniTask<bool> _ShowAsync(int id, ICollection<IUI> uis)
        {
            var data = GetUIData(id);
            if (data == null)
            {
                return false;
            }

            if (data.TabData != null && data.TabData.PID != 0)
            {
                if (!await _ShowAsync(data.TabData.PID, uis))
                {
                    return false;
                }
            }

            if (_showed.TryGetValue(id, out var ui))
            {
                uis.Add(ui);
                return true;
            }

            if (_pendingShows.TryGetValue(id, out var pendingTcs))
            {
                ui = await pendingTcs.Task;
                if (ui == null) return false;
                uis.Add(ui);
                return true;
            }
            var tcs = AutoResetUniTaskCompletionSource<IUI>.Create();
            _pendingShows.Add(id, tcs);
#if UNITY_EDITOR
            __Debugger_Showing_Event();
#endif

            if (_cacheHandler.WaitDels.TryGetValue(id, out var wd))
            {
                wd.GetUI(out ui);
            }
            else if (_cacheHandler.Cache.TryGetValue(id, out ui))
            {
                _cacheHandler.Cache.Remove(id);
#if UNITY_EDITOR
                __Debugger_Cacel_Event();
#endif
            }
            else
            {
                ui = await CreateUI(data);
            }

            if (ui == null)
            {
                _FinishPendingShow(id, null);
#if UNITY_EDITOR
                __Debugger_Showing_Event();
#endif
                return false;
            }

            uis.Add(ui);
            return true;
        }

        private async UniTask<IUI> CreateUI(IUIData data)
        {
            if (data.Pkgs is { Length: > 0 })
            {
                if (!await ResMgr.Ins.LoadUIPackage(data.Pkgs))
                {
                    Log.Error($"[{data.Name}]包加载错误");
                    return null;
                }
            }
            var ui = (IUI)Activator.CreateInstance(data.CType);
            ui.InitData(data, _initData);
            return ui;
        }

        void _ShowCallback(IUI ui, IUIParam param, bool isStack)
        {
            _stackHandler.OnShowed(ui, param, isStack);
            _blurHandler.OnShowed(ui);
        }
    }
}