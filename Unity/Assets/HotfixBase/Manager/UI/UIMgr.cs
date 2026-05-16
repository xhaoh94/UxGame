using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FairyGUI;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Ux
{
    /// <summary>
    /// UI管理器类，负责管理整个UI系统的生命周期、显示、隐藏、栈管理等核心功能    
    /// </summary>
    public partial class UIMgr : Singleton<UIMgr>, IUIBlurHandlerCallback, IUICacheHandlerCallback, IUIMgrDebuggerAccess
    {    
        /// <summary>
        /// 对话框工厂实例，用于创建和管理对话框
        /// </summary>
        public static readonly UIDialogFactory Dialog = new();

        /// <summary>
        /// 提示工厂实例，用于创建和管理提示信息
        /// </summary>
        public static readonly UITipFactory Tip = new();

        // UI系统核心数据存储
        private readonly Dictionary<Type, string> _itemUrls = new(); // 类型到Item URL的映射
        private readonly Dictionary<Type, int> _typeId = new(); // 类型到ID的映射
        private readonly Dictionary<int, UIRecord> _records = new();// ID到UI记录的映射

        /// <summary>
        /// 根界面到记录集合的反向索引。
        /// 隐藏界面时可以直接定位到对应根链路下的全部记录，        
        /// key: 根界面ID
        /// value: 属于该根链路的UI记录ID集合
        /// </summary>
        private readonly Dictionary<int, HashSet<int>> _rootRecordIds = new();

        private readonly Dictionary<int, IUI> _showed = new(); // 当前显示的UI字典
        private readonly Dictionary<int, List<string>> _idLazyloads = new (); // UI懒加载资源字典
        private readonly Dictionary<int, Downloader> _idDownloader = new ();    // UI下载器字典
        private readonly List<StackEntry> _stackEntries = new ();   // UI栈条目列表
        private readonly CallbackData _initData;              // UI初始化回调数据
        private readonly UICacheHandler _cacheHandler;      // UI缓存处理器
        private readonly UIBlurHandler _blurHandler;          // UI模糊效果处理器
        private readonly UIStackHandler _stackHandler;        // UI栈处理器
        private HashSet<int> _ignoreSet;                      // 忽略集合，用于HideAll时排除特定UI

#if UNITY_EDITOR
        /// <summary>
        /// ID到类型名称的映射（仅在编辑器模式下使用）
        /// 用于调试和错误信息显示
        /// </summary>
        private readonly Dictionary<int, string> _idTypeName = new Dictionary<int, string>();
        private readonly Dictionary<string, IUIData> _debugAllUIData = new Dictionary<string, IUIData>();
        private readonly List<string> _debugShowedUI = new List<string>();
        private readonly List<UIStack> _debugUIStacks = new List<UIStack>();
        private readonly List<string> _debugShowingUI = new List<string>();
        private readonly List<string> _debugCacheUI = new List<string>();
        private readonly List<string> _debugWaitDelUI = new List<string>();
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
            _cacheHandler = new UICacheHandler(this);
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

        /// <summary>
        /// 获取或创建UI记录
        /// 如果记录已存在则直接返回，否则创建新的记录
        /// </summary>
        /// <param name="id">UI的ID</param>
        /// <returns>UI记录对象</returns>
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

        /// <summary>
        /// 获取UI记录
        /// </summary>
        /// <param name="id">UI的ID</param>
        /// <returns>UI记录对象，如果不存在则返回null</returns>
        internal UIRecord GetRecord(int id)
        {
            _records.TryGetValue(id, out var record);
            return record;
        }

        /// <summary>
        /// 递增请求版本号并返回新版本
        /// </summary>
        /// <param name="record">UI记录</param>
        /// <returns>新的请求版本号</returns>
        internal int NextRequestVersion(UIRecord record)
        {
            record.RequestVersion++;
            return record.RequestVersion;
        }

        /// <summary>
        /// 检查请求版本是否当前有效
        /// 用于判断异步回调是否仍然对应当前的请求
        /// </summary>
        /// <param name="record">UI记录</param>
        /// <param name="version">要检查的版本号</param>
        /// <returns>如果版本匹配返回true，否则返回false</returns>
        internal bool IsRequestCurrent(UIRecord record, int version)
        {
            return record != null && record.RequestVersion == version;
        }

        /// <summary>fa
        /// 将UI记录注册到根界面
        /// 更新记录的根界面关系，维护反向索引
        /// </summary>
        /// <param name="record">UI记录</param>
        /// <param name="rootId">新的根界面ID</param>
        internal void RegisterRecordToRoot(UIRecord record, int rootId)
        {
            if (record == null)
            {
                return;
            }

            var currentRootId = record.ParentRootId;
            // 如果根界面没有变化且已注册，直接返回
            if (currentRootId == rootId && _rootRecordIds.TryGetValue(rootId, out var existingSet) && existingSet.Contains(record.Id))
            {
                return;
            }

            // 从旧的根界面集合中移除
            if (_rootRecordIds.TryGetValue(currentRootId, out var oldSet))
            {
                oldSet.Remove(record.Id);
                if (oldSet.Count == 0)
                {
                    _rootRecordIds.Remove(currentRootId);
                }
            }

            // 添加到新的根界面集合
            record.ParentRootId = rootId;
            if (!_rootRecordIds.TryGetValue(rootId, out var newSet))
            {
                newSet = new HashSet<int>();
                _rootRecordIds.Add(rootId, newSet);
            }
            newSet.Add(record.Id);
#if UNITY_EDITOR
            ValidateRecordRootBinding(record);
#endif
        }

#if UNITY_EDITOR
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        internal void ValidateRecordRootBinding(UIRecord record)
        {
            if (record == null)
            {
                return;
            }

            if (!_rootRecordIds.TryGetValue(record.ParentRootId, out var rootSet) || !rootSet.Contains(record.Id))
            {
                Log.Error("UIRecord 根链路索引不一致。Id[{0}] RootId[{1}]", record.Id, record.ParentRootId);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        internal void ValidateCurrentChild(int rootId, int activeId)
        {
            if (rootId == 0 || activeId == 0)
            {
                return;
            }

            if (!_records.TryGetValue(rootId, out var rootRecord))
            {
                return;
            }

            if (rootRecord.CurrentChildId != activeId)
            {
                Log.Error("UI根节点当前子界面不一致。RootId[{0}] CurrentChild[{1}] ActiveId[{2}]",
                    rootId, rootRecord.CurrentChildId, activeId);
            }
        }
#endif

        /// <summary>
        /// 检查UI是否刚刚变为可见状态（最近1帧内）
        /// </summary>
        /// <param name="record">UI记录</param>
        /// <returns>如果最近1帧内变为可见返回true，否则返回false</returns>
        internal bool IsFreshlyVisible(UIRecord record)
        {
            return record != null && record.LastVisibleFrame > 0 && Time.frameCount - record.LastVisibleFrame <= 1;
        }

        /// <summary>
        /// 检查UI是否刚刚开始显示流程（最近1帧内）
        /// </summary>
        /// <param name="record">UI记录</param>
        /// <returns>如果最近1帧内开始显示返回true，否则返回false</returns>
        internal bool IsFreshlyShown(UIRecord record)
        {
            return record != null && record.LastShowStartFrame > 0 && Time.frameCount - record.LastShowStartFrame <= 1;
        }

        /// <summary>
        /// 检查是否应该跳过显示请求
        /// 用于避免同一帧内的重复显示请求
        /// </summary>
        /// <param name="record">UI记录</param>
        /// <returns>如果需要跳过返回true，否则返回false</returns>
        internal bool ShouldSkipShowRequest(UIRecord record)
        {
            if (record == null)
            {
                return false;
            }

            // 同一帧内的重复显示请求直接忽略。
            // 只要当前记录已经处于"正在显示"或"已经可见"状态，
            // 就不再重复进入显示流程，避免重复创建动画、重复刷新层级或产生状态竞争。
            return record.LastShowRequestFrame == Time.frameCount && (record.IsVisibleCommitted || record.IsShowingLike);
        }

        /// <summary>
        /// 检查是否应该跳过隐藏请求
        /// 用于避免同一帧内的重复隐藏请求
        /// </summary>
        /// <param name="record">UI记录</param>
        /// <returns>如果需要跳过返回true，否则返回false</returns>
        internal bool ShouldSkipHideRequest(UIRecord record)
        {
            if (record == null)
            {
                return false;
            }

            // 同一帧内的重复隐藏请求直接忽略。
            // 如果当前记录已经进入隐藏流程或已经处于隐藏状态，
            // 再次收到同帧隐藏请求没有意义，直接跳过以避免重复回收和状态覆盖。
            return record.LastHideRequestFrame == Time.frameCount && (record.IsHidingLike || record.Phase == UIPhase.Hidden);
        }

        internal bool IsShowPhase(UIRecord record)
        {
            return record != null && (record.Phase == UIPhase.Showing || record.Phase == UIPhase.Visible);
        }

        internal UniTaskCompletionSource<bool> ResetPendingShow(UIRecord record)
        {
            record.PendingShow?.TrySetResult(false);
            record.PendingShow = new UniTaskCompletionSource<bool>();
            return record.PendingShow;
        }

        internal UniTaskCompletionSource<bool> ResetPendingHide(UIRecord record)
        {
            record.PendingHide?.TrySetResult(false);
            record.PendingHide = new UniTaskCompletionSource<bool>();
            return record.PendingHide;
        }

        internal void CompletePendingShow(UIRecord record, bool result)
        {
            if (record?.PendingShow == null)
            {
                return;
            }

            record.PendingShow.TrySetResult(result);
            record.PendingShow = null;
        }

        internal void CompletePendingHide(UIRecord record, bool result)
        {
            if (record?.PendingHide == null)
            {
                return;
            }

            record.PendingHide.TrySetResult(result);
            record.PendingHide = null;
        }

        /// <summary>
        /// 将UI记录附加到可见记录集合
        /// </summary>
        /// <param name="record">UI记录</param>
        internal void AttachVisibleRecord(UIRecord record)
        {
            if (record?.UI == null)
            {
                return;
            }

            _showed[record.Id] = record.UI;
            record.Phase = UIPhase.Visible;
#if UNITY_EDITOR
            __Debugger_Showed_Event();
#endif
        }

        /// <summary>
        /// 从可见记录集合中分离UI记录
        /// </summary>
        /// <param name="record">UI记录</param>
        internal void DetachVisibleRecord(UIRecord record)
        {
            if (record == null)
            {
                return;
            }

            _showed.Remove(record.Id);
        }

        /// <summary>
        /// 移除UI记录
        /// 从所有相关集合中清理记录数据
        /// </summary>
        /// <param name="id">要移除的UI ID</param>
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

        /// <summary>
        /// 注册UI列表到UI管理器
        /// 解析UI数据并建立父子关系
        /// </summary>
        /// <param name="uis">UI解析数据列表</param>
        /// <param name="itemUrls">Item URL解析数据列表</param>
        public void Add(List<UIParse> uis, List<ItemUrlParse> itemUrls)
        {
            // 第一步：将所有UIData添加到字典中
            foreach (var ui in uis)
            {
                ui.Add(_idUIData);
            }

            // 第二步：解析父子关系
            foreach (var ui in uis)
            {
                ui.Parse(_idUIData);
            }

            // 第三步：添加Item URL映射
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

        /// <summary>
        /// 获取Item的URL
        /// </summary>
        /// <param name="type">Item类型</param>
        /// <returns>Item的URL，如果不存在则返回null</returns>
        public string GetItemUrl(Type type)
        {
            return type != null && _itemUrls.TryGetValue(type, out var url) ? url : null;
        }

        /// <summary>
        /// 获取指定类型的已显示UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>UI实例，如果未显示则返回default</returns>
        public T GetUI<T>() where T : IUI
        {
            return GetUI<T>(GetTypeId(typeof(T)));
        }

        /// <summary>
        /// 获取指定ID的已显示UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="id">UI ID</param>
        /// <returns>UI实例，如果未显示则返回default</returns>
        public T GetUI<T>(int id) where T : IUI
        {
            return _showed.TryGetValue(id, out var ui) ? (T)ui : default;
        }

        /// <summary>
        /// 获取指定ID的已显示UI
        /// </summary>
        /// <param name="id">UI ID</param>
        /// <returns>UI实例，如果未显示则返回null</returns>
        public IUI GetUI(int id)
        {
            return _showed.TryGetValue(id, out var ui) ? ui : null;
        }

        /// <summary>
        /// 获取准备好的UI实例（可能在缓存中）
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="id">UI ID</param>
        /// <returns>UI实例，如果不存在则返回null</returns>
        internal T GetPreparedUI<T>(int id) where T : class, IUI
        {
            var record = GetRecord(id);
            return record?.UI as T;
        }

        /// <summary>
        /// 检查指定类型的UI是否正在显示
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>如果正在显示返回true，否则返回false</returns>
        public bool IsShow<T>() where T : UIBase
        {
            return IsShow(GetTypeId(typeof(T)));
        }

        /// <summary>
        /// 检查指定ID的UI是否正在显示
        /// </summary>
        /// <param name="id">UI ID</param>
        /// <returns>如果正在显示返回true，否则返回false</returns>
        public bool IsShow(int id)
        {
            return IsShowPhase(GetRecord(id));
        }

        /// <summary>
        /// 查看栈顶元素
        /// </summary>
        /// <returns>栈顶元素，如果栈为空则返回null</returns>
        internal StackEntry? PeekStack()
        {
            return _stackEntries.Count > 0 ? _stackEntries[_stackEntries.Count - 1] : (StackEntry?)null;
        }

        /// <summary>
        /// 获取所有栈条目（只读列表）
        /// </summary>
        /// <returns>栈条目列表</returns>
        internal IReadOnlyList<StackEntry> GetStackEntries()
        {
            return _stackEntries;
        }

        /// <summary>
        /// 将类型转换为ID
        /// 使用类型的完整名称生成哈希值作为ID
        /// </summary>
        /// <param name="type">UI类型</param>
        /// <returns>对应的ID</returns>
        int GetTypeId(Type type)
        {
            if (_typeId.TryGetValue(type, out var id))
            {
                return id;
            }

            id = type.FullName.ToHash();
            _typeId.Add(type, id);
#if UNITY_EDITOR
            if (_idTypeName.TryGetValue(id, out var existName) && existName != type.FullName)
            {
                Log.Error("UI类型ID哈希冲突。TypeA[{0}] TypeB[{1}] Id[{2}]", existName, type.FullName, id);
            }
            else
            {
                _idTypeName[id] = type.FullName;
            }
#endif
            return id;
        }

        /// <summary>
        /// 设置场景相机，用于模糊效果处理
        /// </summary>
        /// <param name="mainCamera">主相机</param>
        public void SetSceneCamera(Camera mainCamera)
        {
            _blurHandler.SetSceneCamera(mainCamera);
        }

        /// <summary>
        /// 获取当前显示的UI字典（用于模糊效果处理回调）
        /// </summary>
        Dictionary<int, IUI> IUIBlurHandlerCallback.GetShowedDict()
        {
            return _showed;
        }

        /// <summary>
        /// 释放UI（用于缓存处理回调）
        /// </summary>
        /// <param name="ui">要释放的UI实例</param>
        void IUICacheHandlerCallback.DisposeUI(IUI ui)
        {
            Dispose(ui);
        }

        /// <summary>
        /// 获取所有UIData（用于调试访问）
        /// </summary>
        void IUIMgrDebuggerAccess.FillAllUIData(Dictionary<string, IUIData> output)
        {
            output.Clear();
            foreach (var kv in _idUIData)
            {
                output[kv.Value.Name] = kv.Value;
            }
        }

        /// <summary>
        /// 获取当前显示的UI名称列表（用于调试访问）
        /// </summary>
        void IUIMgrDebuggerAccess.FillShowedUI(List<string> output)
        {
            output.Clear();
            foreach (var kv in _showed)
            {
                output.Add(kv.Value.Name);
            }
        }

        /// <summary>
        /// 获取UI栈列表（用于调试访问）
        /// </summary>
        void IUIMgrDebuggerAccess.FillUIStacks(List<UIStack> output)
        {
            output.Clear();
            for (int i = 0; i < _stackEntries.Count; i++)
            {
                var entry = _stackEntries[i];
                output.Add(new UIStack(entry.RootId, entry.ActiveId, entry.Param, entry.Type));
            }
        }

        /// <summary>
        /// 获取正在显示的UI名称列表（用于调试访问）
        /// </summary>
        void IUIMgrDebuggerAccess.FillShowingUI(List<string> output)
        {
            output.Clear();
            foreach (var kv in _records)
            {
                if (kv.Value.IsShowingLike)
                {
                    output.Add(GetUIData(kv.Key)?.Name ?? kv.Key.ToString());
                }
            }
        }

        /// <summary>
        /// 获取缓存的UI名称列表（用于调试访问）
        /// </summary>
        void IUIMgrDebuggerAccess.FillCacheUI(List<string> output)
        {
            _cacheHandler.FillCacheDebugNames(output);
        }

        /// <summary>
        /// 获取等待销毁的UI名称列表（用于调试访问）
        /// </summary>
        void IUIMgrDebuggerAccess.FillWaitDelUI(List<string> output)
        {
            _cacheHandler.FillWaitDestroyDebugNames(output);
        }
    }
}
