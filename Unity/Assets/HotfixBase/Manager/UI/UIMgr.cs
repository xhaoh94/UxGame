using System.Collections.Generic;
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
        public static readonly UIDialogFactory Dialog = new UIDialogFactory();
        public static readonly UITipFactory Tip = new UITipFactory();

        private readonly Dictionary<Type, string> _itemUrls = new Dictionary<Type, string>();
        private readonly Dictionary<Type, int> _typeId = new Dictionary<Type, int>();
#if UNITY_EDITOR
        private readonly Dictionary<int, string> _idTypeName = new Dictionary<int, string>();
#endif

        // private readonly HashSet<int> _showing = new HashSet<int>();
        private readonly Dictionary<int, AutoResetUniTaskCompletionSource<IUI>> _pendingShows = new Dictionary<int, AutoResetUniTaskCompletionSource<IUI>>();
        private readonly Dictionary<int, IUI> _showed = new Dictionary<int, IUI>();
        private readonly Dictionary<int, List<string>> _idLazyloads = new Dictionary<int, List<string>>();
        private readonly Dictionary<int, Downloader> _idDownloader = new Dictionary<int, Downloader>();
        private HashSet<int> _ignoreSet;
        private readonly CallbackData _initData;        
        private readonly UIStackHandler _stackHandler;
        private readonly UIBlurHandler _blurHandler;
        private readonly UICacheHandler _cacheHandler;


        private readonly Dictionary<UILayer, GComponent> _layerCom = new Dictionary<UILayer, GComponent>(4)
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
            
            _cacheHandler = new UICacheHandler(this);
            _stackHandler = new UIStackHandler(this);
            _blurHandler = new UIBlurHandler(this);

            _initData = new CallbackData(_ShowCallback, _HideCallback, _HideBeforePopStack);
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
            if (!_showed.TryGetValue(id, out var ui)) return default(T);
            return (T)ui;
        }
        public IUI GetUI(int id)
        {
            if (!_showed.TryGetValue(id, out var ui)) return null;
            return ui;
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


#region Handler代理方法


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
            return _pendingShows.ContainsKey(id);
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
            _cacheHandler.CreatedDels.Add(id);
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
            foreach (var (id,_) in _pendingShows)
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

        #endregion

        bool _HideBeforePopStack(IUI ui, bool checkStack)
        {
            return _stackHandler.OnHide(ui, checkStack);
        }
    }
}