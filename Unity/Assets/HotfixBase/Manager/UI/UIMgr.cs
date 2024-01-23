using Cysharp.Threading.Tasks;
using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Ux
{
    public partial class UIMgr : Singleton<UIMgr>
    {
        //显示超时
        const float _showTimeout = 5f;
        //待销毁时间
        const float _waitDelTime = 10f;
        //对话弹窗
        public static readonly UIDialogFactory Dialog = new UIDialogFactory();

        //窗口类型对应的ID
        private readonly Dictionary<Type, int> _typeId = new Dictionary<Type, int>();
#if UNITY_EDITOR
        private readonly Dictionary<int, string> _idTypeName = new Dictionary<int, string>();
#endif

        //界面缓存，关闭不销毁的界面会缓存起来
        private readonly Dictionary<int, IUI> _cacel = new Dictionary<int, IUI>();

        //临时界面缓存，关闭销毁的界面，如果父界面没销毁，
        //会临时缓存起来，等父界面关闭了，再销毁
        private readonly Dictionary<int, IUI> _temCacel = new Dictionary<int, IUI>();
        private readonly Dictionary<int, List<int>> _parentTemCacel = new Dictionary<int, List<int>>();

        //正在显示中的ui列表
        private readonly List<int> _showing = new List<int>();

        //已经显示的ui列表
        private readonly Dictionary<int, IUI> _showed = new Dictionary<int, IUI>();

        //等待销毁的界面
        private readonly Dictionary<int, WaitDel> _waitDels = new Dictionary<int, WaitDel>();

        //创建完需要关闭的界面（用于打开界面后正在加载的时候，在其他地方又马上关闭了界面）
        private readonly List<int> _createdDels = new List<int>();

        //等待关闭动画结束后重新打开的列表
        //private readonly List<int> _waitHideAnimCompleteReShow = new List<int>();

        //界面对应的懒加载标签
        private readonly Dictionary<int, List<string>> _idLazyloads = new Dictionary<int, List<string>>();

        private readonly Dictionary<int, Downloader> _idDownloader = new Dictionary<int, Downloader>();

        private readonly CallBackData _initData;
        //UI层级
        private readonly Dictionary<UILayer, GComponent> _layerCom = new Dictionary<UILayer, GComponent>()
        {
            { UILayer.Root, GRoot.inst },
            { UILayer.Bottom, _CreateLayer(UILayer.Bottom, -100) },
            { UILayer.Tip, _CreateLayer(UILayer.Tip, 200) },
            { UILayer.Top, _CreateLayer(UILayer.Top, 300) }
        };

        static GComponent _CreateLayer(UILayer layer, int v)
        {
            var com = new GComponent();
            com.name = com.gameObjectName = layer.ToString();
            com.sortingOrder = v;
            GRoot.inst.AddChild(com);
            com.MakeFullScreen();
            com.AddRelation(GRoot.inst, RelationType.Size);
            return com;
        }

        public UIMgr()
        {            
            GRoot.inst.SetContentScaleFactor(1280, 720, UIContentScaler.ScreenMatchMode.MatchWidthOrHeight);            
            //StageCamera.main.clearFlags = CameraClearFlags.Nothing;
            if (PatchMgr.Ins.IsDone)
            {
                StageCamera.main.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            }
            _initData = new CallBackData(_ShowCallBack, _HideCallBack, _CheckStack, _HideByStack);
        }

        //内存不足时，清理缓存
        public void OnLowMemory()
        {
            Dialog?.Clear();
            if (_cacel.Count > 0)
            {
                var ids = _cacel.Keys.ToList();
                for (var i = ids.Count - 1; i >= 0; i--)
                {
                    var id = ids[i];
                    if (!_cacel.TryGetValue(id, out IUI ui)) continue;
                    Dispose(ui);
                }

                _cacel.Clear();
            }

            if (_temCacel.Count > 0)
            {
                var ids = _temCacel.Keys.ToList();
                for (var i = ids.Count - 1; i >= 0; i--)
                {
                    var id = ids[i];
                    if (!_temCacel.TryGetValue(id, out IUI ui)) continue;
                    Dispose(ui);
                }

                _temCacel.Clear();
            }

            if (_waitDels.Count > 0)
            {
                var ids = _waitDels.Keys.ToList();
                for (int i = ids.Count - 1; i >= 0; i--)
                {
                    var id = ids[i];
                    if (!_waitDels.TryGetValue(id, out var wd)) continue;
                    wd.Dispose();
                }

                _waitDels.Clear();
            }

#if UNITY_EDITOR
            __Debugger_Event();
#endif
        }

        public void Release()
        {
            OnLowMemory();
            //清理掉动态创建的UI数据
            if (_dymUIData.Count > 0)
            {
                foreach (var id in _dymUIData)
                {
                    _idUIData.Remove(id);
                }

                _dymUIData.Clear();
            }

            //清理掉正在下载的资源
            if (_idDownloader.Count > 0)
            {
                foreach (var kv in _idDownloader)
                {
                    var downloader = kv.Value;
                    downloader.CancelDownload();
                }

                _idDownloader.Clear();
            }
        }

        public void Add(List<UIParse> uis)
        {
            uis.ForEach(ui => { ui.Add(_idUIData); });
            uis.ForEach(ui => { ui.Parse(_idUIData); });
#if UNITY_EDITOR
            __Debugger_UI_Event();
#endif
        }

        public GComponent GetLayer(UILayer layer)
        {
            if (_layerCom.TryGetValue(layer, out var com)) return com;
            return GRoot.inst;
        }

        public T GetUI<T>() where T : IUI
        {
            return GetUI<T>(ConverterID(typeof(T)));
        }

        //获取UI
        public T GetUI<T>(int id) where T : IUI
        {
            if (!_showed.ContainsKey(id)) return default(T);
            return (T)_showed[id];
        }
        public IUI GetUI(int id)
        {
            if (!_showed.ContainsKey(id)) return null;
            return _showed[id];
        }

        public bool IsShow<T>() where T : UIBase
        {
            return IsShow(ConverterID(typeof(T)));
        }

        public bool IsShow(int id)
        {
            return _showed.TryGetValue(id, out var ui) && (ui.State == UIState.ShowAnim || ui.State == UIState.Show);
        }

        int ConverterID(Type type)
        {
            if (_typeId.TryGetValue(type, out var id))
            {
                return id;
            }
            id = type.FullName.ToHash();
            _typeId.Add(type, id);
#if UNITY_EDITOR
            _idTypeName.Add(id, type.FullName);
#endif
            return id;
        }
        public UITask<T> Show<T>(object param = null, bool isAnim = true) where T : IUI
        {
            return Show<T>(ConverterID(typeof(T)), param, isAnim);
        }

        public UITask<T> Show<T>(int id, object param = null, bool isAnim = true) where T : IUI
        {
            var task = ShowAsync<T>(true, id, param, isAnim);
            return new UITask<T>(task);
        }

        public UITask<IUI> Show(int id, object param = null, bool isAnim = true)
        {
            var task = ShowAsync<IUI>(true, id, param, isAnim);
            return new UITask<IUI>(task);
        }

        UITask<IUI> _ShowByStack(int id, object param = null)
        {
            var task = ShowAsync<IUI>(false, id, param, false);
            return new UITask<IUI>(task);
        }
        void _ShowCallBack(IUI ui, object param, bool isStack)
        {
            _ShowCallBack_Stack(ui, param, isStack);
            _ShowCallBack_Blur(ui);
        }

        private async UniTask<T> ShowAsync<T>(bool isStack, int id, object param = null, bool isAnim = true) where T : IUI
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

            if (_createdDels.Contains(id))
            {
                _createdDels.Remove(id);
            }

            var uis = Pool.Get<List<IUI>>();
            var succ = await ShowAsync(childID, uis);
            if (succ)
            {
                foreach (var uiid in uis.Select(ui => ui.ID).Where(uiid => _createdDels.Contains(uiid)))
                {
                    succ = false;
                    _createdDels.Remove(uiid);
                }
            }

            foreach (var ui in uis)
            {
                var uiid = ui.ID;
                if (!succ)
                {
                    _CheckDestroy(ui);
                    _showing.Remove(uiid);
                    continue;
                }

                ui.DoShow(isAnim, id, uiid == id ? param : null, isStack);
                if (_showed.ContainsKey(uiid))
                {
                    continue;
                }
                _showed.Add(uiid, ui);
                _showing.Remove(uiid);
                EventMgr.Ins.Send(MainEventType.UI_SHOW, uiid);
                EventMgr.Ins.Send(MainEventType.UI_SHOW, ui.GetType());
            }

            uis.Clear();
            Pool.Push(uis);

#if UNITY_EDITOR
            __Debugger_Showing_Event();
            __Debugger_Showed_Event();
#endif
            return succ ? (T)_showed[id] : default;
        }

        private async UniTask<bool> ShowAsync(int id, ICollection<IUI> uis)
        {
            var data = GetUIData(id);
            if (data == null)
            {
                return false;
            }

            if (data.TabData != null && data.TabData.PID != 0)
            {
                if (!await ShowAsync(data.TabData.PID, uis))
                {
                    _showing.Remove(id);
#if UNITY_EDITOR
                    __Debugger_Showing_Event();
#endif
                    return false;
                }
            }

            if (_showed.TryGetValue(id, out var ui))
            {
                //switch (ui.State)
                //{
                //    //如果在关闭中，等待关闭后再重新打开
                //    case UIState.HideAnim:
                //    case UIState.Hide:
                //        _waitHideAnimCompleteReShow.Add(id);
                //        while (true)
                //        {
                //            await UniTask.Yield();
                //            if (!_waitHideAnimCompleteReShow.Contains(id)) return false;
                //            if (!_showed.ContainsKey(id))
                //            {
                //                _waitHideAnimCompleteReShow.Remove(id);
                //                break;
                //            }
                //        }
                //        break;
                //    default:
                uis.Add(ui);
                return true;
                //}
            }

            if (_showing.Contains(id))
            {
                float time = Time.unscaledTime;
                while (true)
                {
                    await UniTask.Yield();
                    if (_showed.TryGetValue(id, out ui)) break;
                    if (!_showing.Contains(id)) break;
                    if (_createdDels.Contains(id)) break;
                    if (Time.unscaledTime - time > _showTimeout) break; //超时
                }

                if (ui == null) return false;
                uis.Add(ui);
                return true;
            }

            _showing.Add(id);
#if UNITY_EDITOR
            __Debugger_Showing_Event();
#endif

            if (_waitDels.TryGetValue(id, out var wd))
            {
                wd.GetUI(out ui);
            }
            else
            {
                if (_temCacel.TryGetValue(id, out ui))
                {
                    _temCacel.Remove(id);
                    var temBottomId = ui.Data.GetParentID();
                    if (_parentTemCacel.TryGetValue(temBottomId, out var temList))
                    {
                        if (temList.Remove(id))
                        {
                            if (temList.Count == 0)
                            {
                                _parentTemCacel.Remove(temBottomId);
                            }
                        }
                    }
#if UNITY_EDITOR
                    __Debugger_TemCacel_Event();
#endif
                }
                else if (_cacel.TryGetValue(id, out ui))
                {
                    _cacel.Remove(id);
#if UNITY_EDITOR
                    __Debugger_Cacel_Event();
#endif
                }
                else
                {
                    ui = await CreateUI(data);
                }
            }

            if (ui == null)
            {
                _showing.Remove(id);
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
                if (!await LoaUIdPackage(data.Pkgs))
                {
                    Log.Error($"[{data.Name}]包加载错误");
                    return null;
                }
            }
            var ui = (IUI)Activator.CreateInstance(data.CType);
            ui.InitData(data, _initData);
            return ui;
        }

        public void HideAll(IList<int> ignoreList = null)
        {
            bool Func(int id)
            {
                return ignoreList is { Count: > 0 } && ignoreList.Contains(id);
            }

            _HideAll(Func);
        }
        public void HideAll(List<string> ignoreList = null)
        {
            bool Func(int id)
            {
                return ignoreList is { Count: > 0 } && ignoreList.FindIndex(x => x.ToHash() == id) >= 0;
            }
            _HideAll(Func);
        }
        public void HideAll(List<Type> ignoreList = null)
        {
            bool Func(int id)
            {
                return ignoreList is { Count: > 0 } && ignoreList.FindIndex(x => ConverterID(x) == id) >= 0;
            }
            _HideAll(Func);
        }
        void _HideAll(Func<int, bool> func)
        {
            _stack.Clear();
            foreach (var id in _showing.Where(id => !func(id)))
            {
                Hide(id, false);
            }

            var ids = _showed.Keys.ToList();
            foreach (var id in ids.Where(id => !func(id)))
            {
                Hide(id, false);
            }
        }

        public void Hide<T>(bool isAnim = true) where T : UIBase
        {
            Hide(ConverterID(typeof(T)), isAnim);
        }
        public void Hide(int id, bool isAnim = true)
        {
            _Hide(false, id, isAnim);
        }
        void _Hide(bool isStack, int id, bool isAnim = true)
        {
            if (_showing.Contains(id))
            {
                if (!_createdDels.Contains(id)) _createdDels.Add(id);
                return;
            }

            if (!_showed.TryGetValue(id, out IUI ui))
            {
                return;
            }

            if (ui.State == UIState.HideAnim || ui.State == UIState.Hide)
            {
                //if (_waitHideAnimCompleteReShow.Contains(id))
                //{
                //    _waitHideAnimCompleteReShow.Remove(id);
                //}

                //var ids = ui.Data.GetParentIDs();
                //if (ids != null)
                //{
                //    foreach (var tid in ids)
                //    {
                //        if (_waitHideAnimCompleteReShow.Contains(tid))
                //        {
                //            _waitHideAnimCompleteReShow.Remove(tid);
                //        }
                //    }
                //}
                return;
            }

            var parentID = ui.Data.GetParentID();
            if (parentID != 0 && _showed.TryGetValue(parentID, out ui))
            {
                ui.DoHide(isAnim, isStack);
            }
        }
        private void _HideCallBack(IUI ui)
        {
            var id = ui.ID;
            _showed.Remove(id);
            _CheckDestroy(ui);
#if UNITY_EDITOR
            __Debugger_Showed_Event();
#endif
            EventMgr.Ins.Send(MainEventType.UI_HIDE, id);
            EventMgr.Ins.Send(MainEventType.UI_HIDE, ui.GetType());

            _HideCallBack_Blur(ui);
        }
        private void _CheckDestroy(IUI ui)
        {
            var id = ui.ID;
            var parentID = ui.Data.GetParentID();
            if (ui.IsDestroy)
            {
                //存在父界面，且父界面还没关闭，则放入临时缓存中
                if (parentID != id && IsShow(parentID))
                {
                    if (_temCacel.ContainsKey(id))
                    {
                        Log.Error($"界面[{ui.Name}]多次放入临时缓存列表");
                        return;
                    }

                    _temCacel.Add(id, ui);
                    if (!_parentTemCacel.TryGetValue(parentID, out var temList))
                    {
                        temList = new List<int>();
                        _parentTemCacel.Add(parentID, temList);
                    }

                    temList.Add(id);
#if UNITY_EDITOR
                    __Debugger_TemCacel_Event();
#endif
                }
                else
                {
                    if (_waitDels.ContainsKey(id))
                    {
                        Log.Error($"界面[{ui.Name}]多次放入待删除列表");
                        return;
                    }

                    var wd = Pool.Get<WaitDel>();
                    wd.Init(ui);
                    _waitDels.Add(id, wd);
#if UNITY_EDITOR
                    __Debugger_WaitDel_Event();
#endif
                }
            }
            else
            {
                //不销毁的界面放进缓存列表
                if (_cacel.ContainsKey(id))
                {
                    Log.Error($"界面[{ui.Name}]多次放入缓存列表");
                    return;
                }

                _cacel.Add(id, ui);
#if UNITY_EDITOR
                __Debugger_Cacel_Event();
#endif
            }

            //如果此界面是最底层的界面，则将属于此界面的临时缓存界面从列表清除
            if (id == parentID)
            {
                if (_parentTemCacel.TryGetValue(id, out var temList) && temList.Count > 0)
                {
                    foreach (var cacelId in temList)
                    {
                        if (_temCacel.TryGetValue(cacelId, out var temUI))
                        {
                            _temCacel.Remove(cacelId);
                            _CheckDestroy(temUI);
                        }
                    }
                    temList.Clear();
#if UNITY_EDITOR
                    __Debugger_TemCacel_Event();
#endif
                }
            }
        }

        private void Dispose(IUI ui)
        {
            var id = ui.ID;
            ui.Dispose();
            var data = GetUIData(id);
            if (data == null) return;
            if (data.Pkgs == null || data.Pkgs.Length == 0) return;
            RemoveUIPackage(data.Pkgs);
            if (ui is UIDialog)
            {
                RemoveUIData(id);
            }
        }

        /// <summary>
        /// 获取懒加载标签列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        List<string> _GetDependenciesLazyload(int id)
        {
            if (id == 0) return null;
            if (!_idLazyloads.TryGetValue(id, out var lazyloads))
            {
                var data = GetUIData(id);
                if (data == null)
                {
                    _idLazyloads.Add(id, null);
                    return null;
                }

                lazyloads = new List<string>();
                while (data != null)
                {
                    if (data.Lazyloads != null)
                    {
                        foreach (var lazyload in data.Lazyloads)
                        {
                            if (!lazyloads.Contains(lazyload))
                            {
                                lazyloads.Add(lazyload);
                            }
                        }
                    }

                    if (data.TabData == null)
                    {
                        break;
                    }

                    if (data.TabData.PID == 0)
                    {
                        break;
                    }

                    data = GetUIData(data.TabData.PID);
                }

                _idLazyloads.Add(id, lazyloads);
            }

            return lazyloads;
        }

        bool _CheckDownload(int id, object param, bool isAnim)
        {
            if (_idDownloader.TryGetValue(id, out var download))
            {
                if (download.IsDone)
                {
                    _idDownloader.Remove(id);
                    return false;
                }

                //TODO 显示下载界面
                return true;
            }

            var tags = _GetDependenciesLazyload(id);
            if (tags == null || tags.Count == 0) return false;
            download = ResMgr.Lazyload.GetDownloaderByTags(tags);
            if (download == null) return false;
            Log.Debug($"一共发现了{download.TotalDownloadCount}个资源需要更新下载。");
            Dialog.DoubleBtn(
                "下载",
                $"一共发现了{download.TotalDownloadCount}个资源需要更新下载。",
                "下载",
                () =>
                {
                    //TODO 显示下载界面
                    _idDownloader.Add(id, download);
                    download.BeginDownload(_DownloadComplete, new DownloadData(id, param, isAnim));
                },
                "取消", null);
            return true;
        }

        void _DownloadComplete(bool succeed, object args)
        {
            if (succeed)
            {
                if (args is DownloadData data)
                {
                    Show(data.UIID, data.Param, data.IsAnim);
                }
            }
            else
            {
                Log.Error("下载懒加载资源失败");
            }
        }
    }
}