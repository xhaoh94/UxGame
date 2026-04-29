using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cysharp.Threading.Tasks;
using System;
using FairyGUI;
using static Ux.UIMgr;

namespace Ux
{
    public partial class UIMgr : Singleton<UIMgr>, IUIStackHandlerCallback, IUIBlurHandlerCallback, IUICacheHandlerCallback, IUIMgrDebuggerAccess
    {
        const float _showTimeout = 5f;
        public static readonly UIDialogFactory Dialog = new UIDialogFactory();
        public static readonly UITipFactory Tip = new UITipFactory();

        private readonly Dictionary<Type, string> _itemUrls = new Dictionary<Type, string>();
        private readonly Dictionary<Type, int> _typeId = new Dictionary<Type, int>();
#if UNITY_EDITOR
        private readonly Dictionary<int, string> _idTypeName = new Dictionary<int, string>();
#endif

        private readonly HashSet<int> _showing = new HashSet<int>();
        private readonly Dictionary<int, AutoResetUniTaskCompletionSource<IUI>> _pendingShows = new Dictionary<int, AutoResetUniTaskCompletionSource<IUI>>();
        private readonly Dictionary<int, IUI> _showed = new Dictionary<int, IUI>();
        private readonly Dictionary<int, List<string>> _idLazyloads = new Dictionary<int, List<string>>();
        private readonly Dictionary<int, Downloader> _idDownloader = new Dictionary<int, Downloader>();
        private HashSet<int> _ignoreSet;
        private readonly CallBackData _initData;

        private readonly UIResourceHandler _resourceHandler;
        private readonly UIStackHandler _stackHandler;
        private readonly UIBlurHandler _blurHandler;
        private readonly UICacheHandler _cacheHandler;


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
            UIObjectFactory.SetLoaderExtension(typeof(UxLoader));
            if (PatchMgr.Ins.IsDone)
            {
                StageCamera.main.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            }

            _resourceHandler = new UIResourceHandler();
            _cacheHandler = new UICacheHandler(this);
            _stackHandler = new UIStackHandler(this);
            _blurHandler = new UIBlurHandler(this);

            _initData = new CallBackData(_ShowCallBack, _HideCallBack, _HideBeforePopStack);
        }
        protected override void OnCreated()
        {
            GameMethod.LowMemory += _LowMemory;
        }

        void _LowMemory()
        {
            Dialog?.Clear();
            Tip?.Clear();
            _cacheHandler.ClearMemory();

#if UNITY_EDITOR
            __Debugger_Event();
#endif
        }

        public void Release()
        {
            _LowMemory();
            if (_dymUIData.Count > 0)
            {
                foreach (var id in _dymUIData)
                {
                    _idUIData.Remove(id);
                }

                _dymUIData.Clear();
            }

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

        public void Add(List<UIParse> uis, List<ItemUrlParse> itemUrls)
        {
            foreach (var ui in uis)
            {
                ui.Add(_idUIData);
            }

            foreach (var ui in uis)
            {
                ui.Parse(_idUIData);
            }
            foreach (var itemUrl in itemUrls)
            {
                itemUrl.Add(_itemUrls);
            }
#if UNITY_EDITOR
            __Debugger_UI_Event();
#endif
        }

        public GComponent GetLayer(UILayer layer)
        {
            if (_layerCom.TryGetValue(layer, out var com)) return com;
            return GRoot.inst;
        }
        public string GetItemUrl(Type type)
        {
            if (type != null && _itemUrls.TryGetValue(type, out var url)) return url;
            return null;
        }
        public T GetUI<T>() where T : IUI
        {
            return GetUI<T>(ConverterID(typeof(T)));
        }

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

        public UITask<T> Show<T>(IUIParam param = null, bool isAnim = true) where T : IUI
        {
            return Show<T>(ConverterID(typeof(T)), param, isAnim);
        }

        public UITask<T> Show<T>(int id, IUIParam param = null, bool isAnim = true) where T : IUI
        {
            var task = _ShowAsync<T>(id, param, isAnim, true);
            return new UITask<T>(task);
        }

        public UITask<IUI> Show(int id, IUIParam param = null, bool isAnim = true)
        {
            var task = _ShowAsync<IUI>(id, param, isAnim, true);
            return new UITask<IUI>(task);
        }

        void _ShowCallBack(IUI ui, IUIParam param, bool isStack)
        {
            _stackHandler.OnShowed(ui, param, isStack);
            _blurHandler.OnShowed(ui);
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
                    _cacheHandler.CheckDestroy(ui);
                    _showing.Remove(uiid);
                    _FinishPendingShow(uiid, null);
                    continue;
                }
                await ui.DoShow(isAnim, id, uiid == id ? param : null, checkStack);

                if (_showed.ContainsKey(uiid))
                {
                    continue;
                }
                _showed.Add(uiid, ui);
                _showing.Remove(uiid);
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
                    _showing.Remove(id);
#if UNITY_EDITOR
                    __Debugger_Showing_Event();
#endif
                    return false;
                }
            }

            if (_showed.TryGetValue(id, out var ui))
            {
                uis.Add(ui);
                return true;
            }

            if (_showing.Contains(id))
            {
                if (_pendingShows.TryGetValue(id, out var pendingTcs))
                {
                    ui = await pendingTcs.Task;
                    if (ui == null) return false;
                    uis.Add(ui);
                    return true;
                }

                return false;
            }

            _showing.Add(id);
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
                _showing.Remove(id);
                tcs.TrySetResult(null);
                _pendingShows.Remove(id);
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

        void _HideAll(Func<int, bool> func)
        {
            _stackHandler.Clear();

            // 遍历 _showing 列表
            foreach (var id in _showing)
            {
                if (!func(id))
                {
                    Hide(id, false);
                }
            }

            // 遍历 _showed 字典
            foreach (var kv in _showed)
            {
                var id = kv.Key;
                if (!func(id))
                {
                    Hide(id, false);
                }
            }
        }


        private void _HideAllWithSet()
        {
            _stackHandler.Clear();

            foreach (var id in _showing)
            {
                if (!_ignoreSet.Contains(id))
                {
                    Hide(id, false);
                }
            }

            foreach (var kv in _showed)
            {
                var id = kv.Key;
                if (!_ignoreSet.Contains(id))
                {
                    Hide(id, false);
                }
            }
        }


        public void HideAll(IList<int> ignoreList = null)
        {
            if (ignoreList == null || ignoreList.Count == 0)
            {
                _HideAll(_ => false);
                return;
            }

            _ignoreSet ??= new HashSet<int>();
            _ignoreSet.Clear();
            int cnt = ignoreList.Count;
            for (int i = 0; i < cnt; i++)
            {
                _ignoreSet.Add(ignoreList[i]);
            }
            _HideAllWithSet();
        }

        public void HideAll(IList<Type> ignoreList = null)
        {
            if (ignoreList == null || ignoreList.Count == 0)
            {
                _HideAll(_ => false);
                return;
            }

            _ignoreSet ??= new HashSet<int>();
            _ignoreSet.Clear();
            int cnt = ignoreList.Count;
            for (int i = 0; i < cnt; i++)
            {
                _ignoreSet.Add(ConverterID(ignoreList[i]));
            }
            _HideAllWithSet();
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
            if (_showing.Contains(id))
            {
                if (!_cacheHandler.CreatedDels.Contains(id)) _cacheHandler.CreatedDels.Add(id);
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

        private void _HideCallBack(IUI ui)
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
            RemoveUIPackage(data.Pkgs);
            if (ui is UIDialog)
            {
                RemoveUIData(id);
            }
        }

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

        bool _CheckDownload(int id, IUIParam param, bool isAnim)
        {
            if (_idDownloader.TryGetValue(id, out var download))
            {
                if (download.IsDone)
                {
                    _idDownloader.Remove(id);
                    return false;
                }

                return true;
            }

            var tags = _GetDependenciesLazyload(id);
            if (tags == null || tags.Count == 0) return false;
            download = ResMgr.Lazyload.GetDownloaderByTags(tags);
            if (download == null) return false;
            Log.Debug($"一共发现了{download.TotalDownloadCount}个资源需要更新下载。");


            Dialog.Get()
            .WithTitle("下载")
            .WithContent($"一共发现了{download.TotalDownloadCount}个资源需要更新下载。")
            .WithBtn1("下载", () =>
            {
                 _idDownloader.Add(id, download);
                 download.BeginDownload(_DownloadComplete, new DownloadData(id, param, isAnim));
            });
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

        #region Handler代理方法

        public async UniTask<bool> LoaUIdPackage(string[] pkgs)
        {
            return await _resourceHandler.LoadUIPackage(pkgs);
        }

        public void RemoveUIPackage(string[] pkgs)
        {
            _resourceHandler.RemoveUIPackage(pkgs);
        }

        public void SetSceneCamera(Camera mainCamera)
        {
            _blurHandler.SetSceneCamera(mainCamera);
        }

        #endregion

        #region IUIStackHandlerCallback实现

        void IUIStackHandlerCallback.ShowByStack(int id, IUIParam param)
        {
            _ShowAsync<IUI>(id, param, false, false).Forget();
        }

        bool IUIStackHandlerCallback.IsShowing(int id)
        {
            return _showing.Contains(id);
        }

        IUI IUIStackHandlerCallback.GetShowed(int id)
        {
            return _showed.TryGetValue(id, out var ui) ? ui : null;
        }

        bool IUIStackHandlerCallback.IsInCreatedDels(int id)
        {
            return _cacheHandler.CreatedDels.Contains(id);
        }

        void IUIStackHandlerCallback.AddToCreatedDels(int id)
        {
            if (!_cacheHandler.CreatedDels.Contains(id)) _cacheHandler.CreatedDels.Add(id);
        }

        #endregion

        #region IUIBlurHandlerCallback实现

        Dictionary<int, IUI> IUIBlurHandlerCallback.GetShowedDict()
        {
            return _showed;
        }

        #endregion

        #region IUICacheHandlerCallback实现

        void IUICacheHandlerCallback.DisposeUI(IUI ui)
        {
            Dispose(ui);
        }

        IUIData IUICacheHandlerCallback.GetUIData(int id)
        {
            return GetUIData(id);
        }

        void IUICacheHandlerCallback.RemoveUIPackage(string[] pkgs)
        {
            RemoveUIPackage(pkgs);
        }

        void IUICacheHandlerCallback.RemoveUIData(int id)
        {
            RemoveUIData(id);
        }

        IUI IUICacheHandlerCallback.GetShowed(int id)
        {
            return _showed.TryGetValue(id, out var ui) ? ui : null;
        }

        bool IUICacheHandlerCallback.IsShow(int id)
        {
            return _showed.TryGetValue(id, out var ui) && (ui.State == UIState.ShowAnim || ui.State == UIState.Show);
        }

        #endregion

        #region IUIMgrDebuggerAccess implementation

        Dictionary<string, IUIData> IUIMgrDebuggerAccess.GetAllUIData()
        {
            var dict = new Dictionary<string, IUIData>();
            foreach (var kv in _idUIData)
            {
                dict.Add(kv.Value.Name, kv.Value);
            }
            return dict;
        }

        List<string> IUIMgrDebuggerAccess.GetShowedUI()
        {
            var list = new List<string>();
            foreach (var kv in _showed)
            {
                list.Add(kv.Value.Name);
            }
            return list;
        }

        List<UIStack> IUIMgrDebuggerAccess.GetUIStacks()
        {
            return _stackHandler.UIStacks;
        }

        List<string> IUIMgrDebuggerAccess.GetShowingUI()
        {
            var list = new List<string>();
            foreach (var id in _showing)
            {
                list.Add(GetUIData(id).Name);
            }
            return list;
        }

        List<string> IUIMgrDebuggerAccess.GetCacheUI()
        {
            var list = new List<string>();
            foreach (var kv in _cacheHandler.Cache)
            {
                list.Add(kv.Value.Name);
            }
            return list;
        }

        List<string> IUIMgrDebuggerAccess.GetWaitDelUI()
        {
            var list = new List<string>();
            foreach (var kv in _cacheHandler.WaitDels)
            {
                list.Add(kv.Value.Name);
            }
            return list;
        }

        Dictionary<string, UIPkgRef> IUIMgrDebuggerAccess.GetPkgRefs()
        {
            return _resourceHandler.PkgToRef;
        }

        #endregion

        bool _HideBeforePopStack(IUI ui, bool checkStack)
        {
            return _stackHandler.OnHide(ui, checkStack);
        }
    }
}