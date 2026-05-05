using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FairyGUI;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Ux
{
    public partial class UIMgr : Singleton<UIMgr>, IUIBlurHandlerCallback, IUICacheHandlerCallback, IUIMgrDebuggerAccess
    {
        internal enum UIPhase
        {
            Idle,
            Creating,
            Showing,
            Visible,
            Hiding,
            Hidden,
            Disposed,
        }

        internal sealed class UIRecord
        {
            public int Id;
            public IUI UI;
            public UIPhase Phase = UIPhase.Idle;
            // Monotonic request version used to invalidate stale async show/hide completions.
            public int RequestVersion;
            // Root panel that owns this UI in the current parent-child chain.
            public int ParentRootId;
            // Current active child under the same root chain.
            public int CurrentChildId;
            public IUIParam LastShowParam;
            public int LastShowRequestFrame;
            public int LastHideRequestFrame;
            // Used to collapse Show->Hide calls issued in the same or next frame.
            public int LastShowStartFrame;
            public int LastVisibleFrame;
            public CacheState CacheState;
            // Shared completion source so later show requests can wait for an in-flight hide.
            public UniTaskCompletionSource<bool> PendingHide;
            // Shared completion source used when multiple callers need to await the same visible commit.
            public UniTaskCompletionSource<bool> PendingVisible;
            public WaitDel WaitDel;

            public bool IsVisibleCommitted => Phase == UIPhase.Visible;
            public bool IsShowingLike => Phase == UIPhase.Creating || Phase == UIPhase.Showing;
            public bool IsHidingLike => Phase == UIPhase.Hiding;
        }

        internal enum CacheState
        {
            None,
            Cached,
            WaitDestroy,
        }

        internal readonly struct StackEntry
        {
            public StackEntry(int rootId, int activeId, IUIParam param, UIType type)
            {
                RootId = rootId;
                ActiveId = activeId;
                Param = param;
                Type = type;
            }

            public int RootId { get; }
            public int ActiveId { get; }
            public IUIParam Param { get; }
            public UIType Type { get; }
        }

        public static readonly UIDialogFactory Dialog = new UIDialogFactory();
        public static readonly UITipFactory Tip = new UITipFactory();

        private readonly Dictionary<Type, string> _itemUrls = new Dictionary<Type, string>();
        private readonly Dictionary<Type, int> _typeId = new Dictionary<Type, int>();
        private readonly Dictionary<int, UIRecord> _records = new Dictionary<int, UIRecord>();
        // Reverse index used by hide to walk a single root chain instead of scanning all records.
        private readonly Dictionary<int, HashSet<int>> _rootRecordIds = new Dictionary<int, HashSet<int>>();
        private readonly Dictionary<int, IUI> _showed = new Dictionary<int, IUI>();
        private readonly Dictionary<int, int> _showing = new Dictionary<int, int>();
        private readonly Dictionary<int, List<string>> _idLazyloads = new Dictionary<int, List<string>>();
        private readonly Dictionary<int, Downloader> _idDownloader = new Dictionary<int, Downloader>();
        private readonly List<StackEntry> _stackEntries = new List<StackEntry>();
        private readonly CallbackData _initData;
        private readonly UILifecycleCache _cacheHandler;
        private readonly UIBlurHandler _blurHandler;
        private readonly UIStackHandler _stackHandler;
        private HashSet<int> _ignoreSet;

#if UNITY_EDITOR
        private readonly Dictionary<int, string> _idTypeName = new Dictionary<int, string>();
#endif

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

            _cacheHandler = new UILifecycleCache(this);
            _blurHandler = new UIBlurHandler(this);
            _stackHandler = new UIStackHandler(_stackEntries);
            _initData = new CallbackData(_OnUIShown, _OnUIHidden);
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
                    kv.Value.CancelDownload();
                }

                _idDownloader.Clear();
            }
        }

        internal UIRecord GetOrCreateRecord(int id)
        {
            if (_records.TryGetValue(id, out var record))
            {
                return record;
            }

            var data = GetUIData(id);
            record = new UIRecord
            {
                Id = id,
                ParentRootId = data?.GetParentID() ?? id,
                CurrentChildId = id,
            };
            _records.Add(id, record);
            RegisterRecordToRoot(record, record.ParentRootId);
            return record;
        }

        internal UIRecord GetRecord(int id)
        {
            _records.TryGetValue(id, out var record);
            return record;
        }

        internal int NextRequestVersion(UIRecord record)
        {
            record.RequestVersion++;
            return record.RequestVersion;
        }

        internal bool IsRequestCurrent(UIRecord record, int version)
        {
            return record != null && record.RequestVersion == version;
        }

        internal void RegisterRecordToRoot(UIRecord record, int rootId)
        {
            if (record == null)
            {
                return;
            }

            var currentRootId = record.ParentRootId;
            if (currentRootId == rootId && _rootRecordIds.TryGetValue(rootId, out var existingSet) && existingSet.Contains(record.Id))
            {
                return;
            }

            if (_rootRecordIds.TryGetValue(currentRootId, out var oldSet))
            {
                oldSet.Remove(record.Id);
                if (oldSet.Count == 0)
                {
                    _rootRecordIds.Remove(currentRootId);
                }
            }

            record.ParentRootId = rootId;
            if (!_rootRecordIds.TryGetValue(rootId, out var newSet))
            {
                newSet = new HashSet<int>();
                _rootRecordIds.Add(rootId, newSet);
            }
            newSet.Add(record.Id);
        }

        internal bool IsFreshlyVisible(UIRecord record)
        {
            return record != null && record.LastVisibleFrame > 0 && Time.frameCount - record.LastVisibleFrame <= 1;
        }

        internal bool IsFreshlyShown(UIRecord record)
        {
            return record != null && record.LastShowStartFrame > 0 && Time.frameCount - record.LastShowStartFrame <= 1;
        }

        internal bool ShouldSkipShowRequest(UIRecord record)
        {
            if (record == null)
            {
                return false;
            }

            // Same-frame duplicate show requests are ignored once the record is already visible or showing.
            return record.LastShowRequestFrame == Time.frameCount && (record.IsVisibleCommitted || record.IsShowingLike);
        }

        internal bool ShouldSkipHideRequest(UIRecord record)
        {
            if (record == null)
            {
                return false;
            }

            // Same-frame duplicate hides are ignored once the record is already hiding or hidden.
            return record.LastHideRequestFrame == Time.frameCount && (record.IsHidingLike || record.Phase == UIPhase.Hidden);
        }

        internal void AttachVisibleRecord(UIRecord record)
        {
            if (record?.UI == null)
            {
                return;
            }

            _showed[record.Id] = record.UI;
            record.Phase = UIPhase.Visible;
        }

        internal void DetachVisibleRecord(UIRecord record)
        {
            if (record == null)
            {
                return;
            }

            _showed.Remove(record.Id);
        }

        internal void RemoveRecord(int id)
        {
            if (_records.TryGetValue(id, out var record) && _rootRecordIds.TryGetValue(record.ParentRootId, out var rootSet))
            {
                rootSet.Remove(id);
                if (rootSet.Count == 0)
                {
                    _rootRecordIds.Remove(record.ParentRootId);
                }
            }
            _records.Remove(id);
            _showed.Remove(id);
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
            return _layerCom.TryGetValue(layer, out var com) ? com : GRoot.inst;
        }

        public string GetItemUrl(Type type)
        {
            return type != null && _itemUrls.TryGetValue(type, out var url) ? url : null;
        }

        public T GetUI<T>() where T : IUI
        {
            return GetUI<T>(ConverterID(typeof(T)));
        }

        public T GetUI<T>(int id) where T : IUI
        {
            return _showed.TryGetValue(id, out var ui) ? (T)ui : default;
        }

        public IUI GetUI(int id)
        {
            return _showed.TryGetValue(id, out var ui) ? ui : null;
        }

        internal T GetPreparedUI<T>(int id) where T : class, IUI
        {
            var record = GetRecord(id);
            return record?.UI as T;
        }

        public bool IsShow<T>() where T : UIBase
        {
            return IsShow(ConverterID(typeof(T)));
        }

        public bool IsShow(int id)
        {
            return _showed.TryGetValue(id, out var ui) && (ui.State == UIState.ShowAnim || ui.State == UIState.Show);
        }

        internal StackEntry? PeekStack()
        {
            return _stackEntries.Count > 0 ? _stackEntries[_stackEntries.Count - 1] : (StackEntry?)null;
        }

        internal IReadOnlyList<StackEntry> GetStackEntries()
        {
            return _stackEntries;
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

        public void SetSceneCamera(Camera mainCamera)
        {
            _blurHandler.SetSceneCamera(mainCamera);
        }

        Dictionary<int, IUI> IUIBlurHandlerCallback.GetShowedDict()
        {
            return _showed;
        }

        void IUICacheHandlerCallback.DisposeUI(IUI ui)
        {
            Dispose(ui);
        }

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
            var list = new List<UIStack>(_stackEntries.Count);
            for (int i = 0; i < _stackEntries.Count; i++)
            {
                var entry = _stackEntries[i];
                list.Add(new UIStack(entry.RootId, entry.ActiveId, entry.Param, entry.Type));
            }
            return list;
        }

        List<string> IUIMgrDebuggerAccess.GetShowingUI()
        {
            var list = new List<string>();
            foreach (var kv in _records)
            {
                if (kv.Value.IsShowingLike)
                {
                    list.Add(GetUIData(kv.Key)?.Name ?? kv.Key.ToString());
                }
            }
            return list;
        }

        List<string> IUIMgrDebuggerAccess.GetCacheUI()
        {
            return _cacheHandler.GetCacheDebugNames();
        }

        List<string> IUIMgrDebuggerAccess.GetWaitDelUI()
        {
            return _cacheHandler.GetWaitDestroyDebugNames();
        }
    }
}
