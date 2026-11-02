using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Ux
{
    /// <summary>
    /// UI管理器类，负责管理整个UI系统的生命周期、显示、隐藏、栈管理等核心功能    
    /// </summary>
    public partial class UIMgr : Singleton<UIMgr>, IUIBlurHandlerCallback, IUICacheHandlerCallback, IUIMgrDebuggerAccess, IUIFocusHandlerCallback
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
        private static int _nextTypeId = 100000; // 递增ID分配器，保证绝对不碰撞
        private readonly Dictionary<int, UIRecord> _records = new();// 实例ID到UI记录的映射

        /// <summary>
        /// 类型ID的起始值。实例ID从这里往下分配，两段 ID 空间不重叠，
        /// 于是任何一个 int 都能无歧义地判断出是实例 ID 还是类型 ID。
        /// </summary>
        internal const int TypeIdBase = 100000;

        private int _nextInstanceId = 1; // 实例ID分配器

        /// <summary>
        /// 类型ID到实例ID列表的索引。
        /// 同一类型可以存在多个实例（弹窗/飘字），按创建顺序保存，
        /// 查询时优先返回当前可见（或正在显示）的那个，保证「按类型取界面」的行为符合直觉。
        /// key: 类型ID
        /// value: 该类型下的实例ID列表（创建顺序）
        /// </summary>
        private readonly Dictionary<int, List<int>> _typeInstances = new();

        /// <summary>
        /// 根界面到记录集合的反向索引。
        /// 隐藏界面时可以直接定位到对应根链路下的全部记录，        
        /// key: 根界面记录ID（实例ID）
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
        private readonly UIFocusHandler _focusHandler;        // UI聚焦处理器

#if UNITY_EDITOR
        /// <summary>
        /// ID到类型名称的映射（仅在编辑器模式下使用）
        /// 用于调试和错误信息显示
        /// </summary>
        private readonly Dictionary<int, string> _idTypeName = new Dictionary<int, string>();
        private readonly Dictionary<string, IUIData> _debugAllUIData = new Dictionary<string, IUIData>();
        private readonly List<string> _debugShowedUI = new List<string>();
        private readonly List<UIStack> _debugUIStacks = new List<UIStack>();
        private readonly List<string> _debugCacheUI = new List<string>();
        private readonly List<string> _debugWaitDelUI = new List<string>();
#endif

        private readonly Dictionary<UILayer, RectTransform> _uguiLayers = new Dictionary<UILayer, RectTransform>(7);
        private bool _uiInitialized;

        /// <summary>是否已完成 UI 初始化（UI Root 与各层级已创建）。</summary>
        public bool IsInitialized => _uiInitialized;

        private static RectTransform _CreateUILayer(Transform root, UILayer layer)
        {
            var go = new GameObject(layer.ToString(), typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        private const string UiRootName = "[UIRoot]";
        private static RectTransform _uiRoot;

        /// <summary>
        /// 获取或创建 UI 专用根节点。
        /// 不复用场景里其他 Canvas（例如调试控制台），避免 UI 层级混入无关对象。
        /// </summary>
        private static RectTransform EnsureUiRoot()
        {
            if (_uiRoot != null)
                return _uiRoot;

            var existing = GameObject.Find(UiRootName);
            if (existing != null)
            {
                _uiRoot = existing.transform as RectTransform;
                if (existing.GetComponent<UIRelationDriver>() == null)
                    existing.AddComponent<UIRelationDriver>();
                return _uiRoot;
            }

            var go = new GameObject(UiRootName,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
                typeof(UIRelationDriver));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            UnityEngine.Object.DontDestroyOnLoad(go);
            _uiRoot = (RectTransform)go.transform;
            return _uiRoot;
        }

        /// <summary>初始化 UI 系统（幂等）。单例创建时调用一次。</summary>
        private void Initialize()
        {
            // 已初始化且根节点仍存活时直接返回；根节点被销毁时重建，避免层级字典指向失效对象。
            if (_uiInitialized && _uiRoot != null)
                return;

            var root = EnsureUiRoot();

            _uiInitialized = false;
            _uguiLayers.Clear();
            _uguiLayers[UILayer.Root] = root;
            _uguiLayers[UILayer.Bottom] = _CreateUILayer(root, UILayer.Bottom);
            _uguiLayers[UILayer.BlurBackdrop] = _CreateUILayer(root, UILayer.BlurBackdrop);
            _uguiLayers[UILayer.View] = _CreateUILayer(root, UILayer.View);
            _uguiLayers[UILayer.Window] = _CreateUILayer(root, UILayer.Window);
            _uguiLayers[UILayer.Tip] = _CreateUILayer(root, UILayer.Tip);
            _uguiLayers[UILayer.Top] = _CreateUILayer(root, UILayer.Top);
            _uiInitialized = true;
        }

        public UIMgr()
        {
            // 构造函数只做纯字段初始化；Unity 对象在 Initialize 中创建。
            _cacheHandler = new UICacheHandler(this);
            _blurHandler = new UIBlurHandler(this);
            _stackHandler = new UIStackHandler(_stackEntries);
            _focusHandler = new UIFocusHandler(this);
            _initData = new CallbackData(_OnUIShown, _OnUIHidden);
        }

        protected override void OnCreated()
        {
            Initialize();
            GameMethod.LowMemory += _LowMemory;
        }

        void _LowMemory()
        {
            Dialog?.Clear();
            Tip?.Clear();
            _cacheHandler.ClearMemory();
            _blurHandler.ClearSnapshots();

#if UNITY_EDITOR
            __Debugger_Event();
#endif
        }

        public void Release()
        {            
            // 1. 先收起所有界面，让「隐藏 -> 提交隐藏 -> 进入缓存」的链路完整走一遍。
            //    否则可见界面会继续挂在 _showed 上，下一个游戏状态仍然认为它们在显示。
            HideAllInternal(null);

            // 2. 清空缓存、延迟销毁、模糊快照以及弹窗/飘字池（会走到 Dispose，释放资源包引用）。
            _LowMemory();

            // 3. 动态 UIData 必须在对应界面销毁之后再移除。
            //    否则会出现「UI 还活着、UIData 已经没了」的状态：界面既无法隐藏，资源包引用也释放不掉。
            if (_dymUIData.Count > 0)
            {
                foreach (var id in _dymUIData)
                {
                    _idUIData.Remove(id);
                }

                _dymUIData.Clear();
            }

            // 4. 重置全部索引与栈，避免把上一个会话的状态带进登录态。
            //    先清索引：清空之后晚到的显示/隐藏回调会因 GetRecord 返回 null 而按「游离实例」处理，
            //    不会再把状态写回已经被重置的容器。
            var pendingRecords = Pool.Get<List<UIRecord>>();
            foreach (var kv in _records)
            {
                if (kv.Value != null)
                {
                    pendingRecords.Add(kv.Value);
                }
            }

            _records.Clear();
            _showed.Clear();
            _rootRecordIds.Clear();
            _typeInstances.Clear();
            _hideAllIdBuffer.Clear();
            _stackHandler.Clear();

            // 索引清空后再完成挂起的等待对象，避免调用方（await Show/Hide）永久等待。
            for (int i = 0; i < pendingRecords.Count; i++)
            {
                CompletePendingShow(pendingRecords[i], false);
                CompletePendingHide(pendingRecords[i], false);
            }

            pendingRecords.Clear();
            Pool.Push(pendingRecords);

            _focusHandler.Clear();
            _blurHandler.ClearSnapshots();

            // 5. 取消未完成的下载。
            if (_idDownloader.Count > 0)
            {
                foreach (var kv in _idDownloader)
                {
                    kv.Value.CancelDownload();
                }

                _idDownloader.Clear();
            }

#if UNITY_EDITOR
            __Debugger_Event();
            AssertShowedConsistency("Release");
#endif
        }

        /// <summary>
        /// 获取或创建UI记录。
        /// 参数是「类型 ID」：默认复用该类型已有的实例，forceNew 为 true 时新建一个实例。
        /// </summary>
        /// <param name="typeId">UI 类型 ID</param>
        /// <param name="forceNew">是否强制新建实例（同一类型多实例）</param>
        /// <returns>UI记录对象</returns>
        internal UIRecord GetOrCreateRecord(int typeId, bool forceNew = false)
        {
            if (!forceNew && TryGetPrimaryRecord(typeId, out var existing))
            {
                return existing;
            }

            var record = new UIRecord
            {
                Id = AllocateInstanceId(),
                TypeId = typeId,
                ParentRootId = 0,
            };

            _records.Add(record.Id, record);
            if (!_typeInstances.TryGetValue(typeId, out var instances))
            {
                instances = new List<int>(1);
                _typeInstances.Add(typeId, instances);
            }

            instances.Add(record.Id);
            return record;
        }

        /// <summary>
        /// 分配一个实例 ID。实例 ID 始终小于 <see cref="TypeIdBase"/>，
        /// 超过上限说明实例泄漏（没有按 Release/Dispose 回收），这里只报错不抛异常，避免线上直接崩。
        /// </summary>
        private int AllocateInstanceId()
        {
            var id = _nextInstanceId++;
            if (id >= TypeIdBase)
            {
                Log.Error("UI 实例 ID 已耗尽（{0}），疑似实例泄漏。已回绕到 1，请检查界面的销毁路径。", TypeIdBase);
                _nextInstanceId = 1;
                id = _nextInstanceId++;
            }

            return id;
        }

        /// <summary>
        /// 按类型ID取「主实例」记录：优先返回可见（或正在显示）的实例，否则返回最近创建的一个。
        /// </summary>
        private bool TryGetPrimaryRecord(int typeId, out UIRecord record)
        {
            record = null;
            if (!_typeInstances.TryGetValue(typeId, out var instances) || instances.Count == 0)
            {
                return false;
            }

            for (int i = instances.Count - 1; i >= 0; i--)
            {
                if (!_records.TryGetValue(instances[i], out var candidate) || candidate == null)
                {
                    continue;
                }

                record ??= candidate;
                if (candidate.IsVisibleCommitted || candidate.IsShowingLike)
                {
                    record = candidate;
                    return true;
                }
            }

            return record != null;
        }

        /// <summary>
        /// 获取UI记录。
        /// 允许传实例 ID（精确命中）或类型 ID（落到主实例），
        /// 这样「按实例管理」的内部代码和「按类型调用」的外部接口可以共存。
        /// </summary>
        /// <param name="id">实例 ID 或 类型 ID</param>
        /// <returns>UI记录对象，如果不存在则返回null</returns>
        internal UIRecord GetRecord(int id)
        {
            if (id == 0)
            {
                return null;
            }

            if (_records.TryGetValue(id, out var record))
            {
                return record;
            }

            return TryGetPrimaryRecord(id, out var byType) ? byType : null;
        }

        /// <summary>
        /// 取记录的实例 ID（传实例 ID 原样返回，传类型 ID 落到主实例）。
        /// </summary>
        internal int ResolveRecordId(int id)
        {
            return GetRecord(id)?.Id ?? 0;
        }

        /// <summary>
        /// 取 ID 对应的类型 ID。
        /// </summary>
        internal int GetRecordTypeId(int id)
        {
            var record = GetRecord(id);
            if (record != null)
            {
                return record.TypeId;
            }

            return id >= TypeIdBase ? id : 0;
        }

        /// <summary>
        /// 开始显示事务并返回本次请求版本。
        /// </summary>
        internal int BeginShowTransition(UIRecord record)
        {
            return record.Transition.BeginShow(record);
        }

        /// <summary>
        /// 开始隐藏事务并返回本次请求版本。
        /// </summary>
        internal int BeginHideTransition(UIRecord record)
        {
            return record.Transition.BeginHide(record);
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
            return record != null && record.RequestVersion == version && record.Transition.Version == version;
        }

        //获取当前请求的子页签
        internal int GetRequestedRootChildId(int rootId)
        {
            return GetRequestedRootChildId(GetRecord(rootId));
        }
        //获取当前请求的子页签
        internal int GetRequestedRootChildId(UIRecord record)
        {
            return record?.CurrentChildId ?? 0;
        }

        internal int GetActiveRootChildId(UIRecord record)
        {
            if (record == null)
            {
                return 0;
            }

            return record.CurrentChildId == 0 ? record.Id : record.CurrentChildId;
        }

        internal int GetMountedRootChildId(int rootId)
        {
            var rootRecord = GetRecord(rootId);
            return rootRecord?.MountedChildId ?? 0;
        }

        internal bool IsRootChildRequested(int rootId, int childId)
        {
            return GetRequestedRootChildId(rootId) == childId;
        }

        internal void SetRootChildRequest(int rootId, int childId)
        {
            var rootRecord = GetRecord(rootId);
            if (rootRecord == null)
            {
                return;
            }

            rootRecord.CurrentChildId = childId == 0 ? rootId : childId;
        }

        internal void BindRecordToRootRequest(UIRecord record, int rootId, int childId)
        {
            if (record == null)
            {
                return;
            }

            RegisterRecordToRoot(record, rootId);
            record.CurrentChildId = childId == 0 ? rootId : childId;
        }

        internal void ResetRootChildRequestIfCurrent(int rootId, int childId)
        {
            var rootRecord = GetRecord(rootId);
            if (rootRecord == null || rootRecord.CurrentChildId != childId)
            {
                return;
            }

            rootRecord.CurrentChildId = rootId;
        }

        internal void MarkRootChildMounted(int rootId, int childId)
        {
            if (rootId == 0 || childId == 0 || rootId == childId)
            {
                return;
            }

            var rootRecord = GetRecord(rootId);
            if (rootRecord != null)
            {
                rootRecord.MountedChildId = childId;
            }
        }

        internal void ClearMountedRootChild(UIRecord record)
        {
            if (record == null)
            {
                return;
            }

            ClearMountedRootChild(record.ParentRootId, record.Id);
        }

        internal void ClearMountedRootChild(int rootId, int childId)
        {
            if (rootId == 0 || childId == 0 || rootId == childId)
            {
                return;
            }

            var rootRecord = GetRecord(rootId);
            if (rootRecord != null && rootRecord.MountedChildId == childId)
            {
                rootRecord.MountedChildId = 0;
            }
        }

        /// <summary>
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

            var currentChildId = GetRequestedRootChildId(rootRecord);
            if (currentChildId != activeId)
            {
                Log.Error("UI根节点当前子界面不一致。RootId[{0}] CurrentChild[{1}] ActiveId[{2}]",
                    rootId, currentChildId, activeId);
            }
        }
#endif

        /// <summary>
        /// 显示/隐藏抖动折叠窗口（秒）。
        /// 在这个窗口内「刚显示又立刻隐藏」会被折叠成一次直接关闭（不播隐藏动画），
        /// 避免界面闪一下。用时间而不是帧号，保证不同帧率下行为一致。
        /// </summary>
        public static float ShowHideCollapseSeconds { get; set; } = 0.1f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 记录阶段迁移回调（仅编辑器 / 开发版）。
        /// 排查「界面卡在 Showing / Hiding」这类问题时，把它接上就能拿到完整的迁移序列。
        /// </summary>
        internal static event Action<UIRecord, UIPhase, UIPhase> OnPhaseChanged;
#endif

        /// <summary>
        /// 统一修改记录阶段。所有阶段迁移都必须走这里：
        /// 开发版可以在这里输出迁移序列、抓非法迁移，排查状态机问题时不用到处下断点。
        /// </summary>
        internal static void SetPhase(UIRecord record, UIPhase phase)
        {
            if (record == null || record.Phase == phase)
            {
                return;
            }

            var previous = record.Phase;
            record.Phase = phase;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsInvalidPhaseTransition(previous, phase))
            {
                Log.Error("UI 记录阶段非法迁移。InstanceId[{0}] {1} -> {2}", record.Id, previous, phase);
            }

            OnPhaseChanged?.Invoke(record, previous, phase);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 明显非法的阶段迁移（只抓「销毁后复活」和「从非中间态直接销毁」这类一定会出问题的，
        /// 不做严格状态机校验，避免误报太多把真正的错误淹掉）。
        /// </summary>
        private static bool IsInvalidPhaseTransition(UIPhase from, UIPhase to)
        {
            if (from == UIPhase.Disposed)
            {
                return true;
            }

            return to == UIPhase.Disposed && from != UIPhase.Hidden && from != UIPhase.Idle;
        }
#endif

        /// <summary>
        /// 检查UI是否刚刚变为可见状态（在折叠窗口内）
        /// </summary>
        /// <param name="record">UI记录</param>
        /// <returns>如果在折叠窗口内变为可见返回true，否则返回false</returns>
        internal bool IsFreshlyVisible(UIRecord record)
        {
            return record != null && record.LastVisibleTime > 0f &&
                   Time.unscaledTime - record.LastVisibleTime <= ShowHideCollapseSeconds;
        }

        /// <summary>
        /// 检查UI是否刚刚开始显示流程（在折叠窗口内）
        /// </summary>
        /// <param name="record">UI记录</param>
        /// <returns>如果在折叠窗口内开始显示返回true，否则返回false</returns>
        internal bool IsFreshlyShown(UIRecord record)
        {
            return record != null && record.LastShowStartTime > 0f &&
                   Time.unscaledTime - record.LastShowStartTime <= ShowHideCollapseSeconds;
        }

        /// <summary>
        /// 是否需要把本次 Show -> Hide 折叠成一次直接关闭（不播隐藏动画）。
        /// 把四处判断收敛到一个入口，隐藏流程只依赖这一个决策。
        /// </summary>
        internal bool ShouldSkipHideAnim(UIRecord record)
        {
            return IsFreshlyShown(record) || IsFreshlyVisible(record);
        }

        internal bool IsShowPhase(UIRecord record)
        {
            return record != null && (record.Phase == UIPhase.Showing || record.Phase == UIPhase.Visible);
        }

        internal void CompletePendingShow(UIRecord record, bool result)
        {
            if (record?.PendingShow == null)
            {
                return;
            }

            record.Transition.CompleteShow(result);
        }

        internal void CompletePendingHide(UIRecord record, bool result)
        {
            if (record?.PendingHide == null)
            {
                return;
            }

            record.Transition.CompleteHide(result);
        }

        /// <summary>
        /// 将UI记录附加到可见记录集合。
        /// 这是 _showed 唯一的写入入口之一（另一个是 RemoveShowed），
        /// 两处都做了「记录状态先就位、索引后更新」的排序，保证编辑器断言看到的永远是一致状态。
        /// </summary>
        /// <param name="record">UI记录</param>
        internal void AttachVisibleRecord(UIRecord record)
        {
            if (record?.UI == null)
            {
                return;
            }

            SetPhase(record, UIPhase.Visible);
            _showed[record.Id] = record.UI;
#if UNITY_EDITOR
            __Debugger_Showed_Event();
            AssertShowedConsistency("AttachVisibleRecord");
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

            RemoveShowed(record.Id);
        }

        /// <summary>
        /// 从可见索引中移除指定 Id。
        /// 所有移除都必须走这里，避免出现「某一处漏删 → 界面再也关不掉」。
        /// </summary>
        private void RemoveShowed(int id)
        {
            if (!_showed.Remove(id))
            {
                return;
            }

#if UNITY_EDITOR
            __Debugger_Showed_Event();
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下校验「可见索引」与「记录表」是否一致。
        /// _showed 是 _records 的派生视图，任何一处漏写都会表现为「界面关不掉」，
        /// 与其线上排查，不如在每次变更后当场报出来。
        /// </summary>
        private void AssertShowedConsistency(string context)
        {
            foreach (var kv in _showed)
            {
                if (!_records.TryGetValue(kv.Key, out var record) || record == null || record.UI == null)
                {
                    Log.Warning("可见索引残留了没有记录的实例，Context[{0}] Id[{1}]", context, kv.Key);
                    continue;
                }

                if (!ReferenceEquals(record.UI, kv.Value))
                {
                    Log.Warning("可见索引与记录指向不同实例，Context[{0}] Id[{1}]", context, kv.Key);
                    continue;
                }

                if (record.Phase != UIPhase.Showing && record.Phase != UIPhase.Visible && record.Phase != UIPhase.Hiding)
                {
                    Log.Warning("可见索引中的记录阶段异常，Context[{0}] Id[{1}] Phase[{2}]", context, kv.Key, record.Phase);
                }
            }

            foreach (var kv in _records)
            {
                var record = kv.Value;
                if (record == null || !record.IsVisibleCommitted)
                {
                    continue;
                }

                if (!_showed.TryGetValue(kv.Key, out var ui) || !ReferenceEquals(ui, record.UI))
                {
                    Log.Error("界面已提交可见却不在可见索引中，Context[{0}] Id[{1}]", context, kv.Key);
                }
            }
        }
#endif

        /// <summary>
        /// 移除UI记录
        /// 从所有相关集合中清理记录数据
        /// </summary>
        /// <param name="id">要移除的UI ID</param>
        internal void RemoveRecord(int id)
        {
            if (_records.TryGetValue(id, out var record))
            {
                ClearMountedRootChild(record);
                ResetRootChildRequestIfCurrent(record.ParentRootId, id);

                if (_rootRecordIds.TryGetValue(record.ParentRootId, out var rootSet))
                {
                    rootSet.Remove(id);
                    if (rootSet.Count == 0)
                    {
                        _rootRecordIds.Remove(record.ParentRootId);
                    }
                }

                // 同步维护类型索引，避免「已销毁的实例」还被按类型查询到。
                if (_typeInstances.TryGetValue(record.TypeId, out var instances))
                {
                    instances.Remove(id);
                    if (instances.Count == 0)
                    {
                        _typeInstances.Remove(record.TypeId);
                    }
                }
            }

            _records.Remove(id);
            RemoveShowed(id);
#if UNITY_EDITOR
            AssertShowedConsistency("RemoveRecord");
#endif
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

        /// <summary>
        /// 获取 UI 层级。UI 页面通过 RectTransform.SetParent 挂载。
        /// </summary>
        public RectTransform GetUILayer(UILayer layer)
        {
            // 调用方都经由 UIMgr.Ins，单例创建时已完成初始化，这里不再做防御性补齐。
            if (_uguiLayers.TryGetValue(layer, out var rect))
            {
                return rect;
            }

            Log.Error("UI UI 层级未创建，已回退到 Root。Layer[{0}]", layer);
            return _uguiLayers[UILayer.Root];
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
            return TryGetTypeId(typeof(T), out var id) ? GetUI<T>(id) : default;
        }

        /// <summary>
        /// 获取指定ID的已显示UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="id">UI ID</param>
        /// <returns>UI实例，如果未显示则返回default</returns>
        public T GetUI<T>(int id) where T : IUI
        {
            return GetUI(id) is T target ? target : default;
        }

        /// <summary>
        /// 获取指定ID的已显示UI。
        /// 传实例 ID 精确命中；传类型 ID 时落到该类型当前可见的实例。
        /// </summary>
        /// <param name="id">UI 实例 ID 或 类型 ID</param>
        /// <returns>UI实例，如果未显示则返回null</returns>
        public IUI GetUI(int id)
        {
            var recordId = ResolveRecordId(id);
            return recordId != 0 && _showed.TryGetValue(recordId, out var ui) ? ui : null;
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
            return TryGetTypeId(typeof(T), out var id) && IsShow(id);
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
        /// 查询已注册的UI类型Id。
        /// 与 GetTypeId 不同，这里不会给未注册类型临时分配 Id：
        /// 查询类 API 拿到一个凭空分配的 Id 只会静默返回失败，反而把「类型没注册」的配置错误藏起来。
        /// </summary>
        internal bool TryGetTypeId(Type type, out int id)
        {
            if (type != null && _typeId.TryGetValue(type, out id))
            {
                return true;
            }

            id = 0;
#if UNITY_EDITOR
            if (type != null)
            {
                Log.Error("UI 类型未注册，请检查 UIMgr.Add 的注册表。Type[{0}]", type.FullName);
            }
#endif
            return false;
        }

        internal int GetTypeId(Type type)
        {
            if (_typeId.TryGetValue(type, out var id))
            {
                return id;
            }

            id = _nextTypeId++;
            _typeId.Add(type, id);
#if UNITY_EDITOR
            _idTypeName[id] = type.FullName;
#endif
            return id;
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
                    output.Add(_idUIData.TryGetValue(kv.Value.TypeId, out var data) ? data.Name : kv.Key.ToString());
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

        /// <summary>
        /// 手动聚焦指定类型的UI
        /// </summary>
        public bool Focus<T>() where T : UIBase
        {
            return TryGetTypeId(typeof(T), out var id) && _focusHandler.Focus(ResolveRecordId(id));
        }

        /// <summary>
        /// 手动聚焦指定ID的UI（实例 ID 或 类型 ID 都可以）
        /// </summary>
        public bool Focus(int id)
        {
            var recordId = ResolveRecordId(id);
            return recordId != 0 && _focusHandler.Focus(recordId);
        }

        /// <summary>
        /// 获取当前聚焦的UI
        /// </summary>
        public IUI GetFocusedUI()
        {
            return _focusHandler.GetFocusedUI();
        }

        /// <summary>
        /// 获取当前聚焦的UI（泛型）
        /// </summary>
        public T GetFocusedUI<T>() where T : class, IUI
        {
            return _focusHandler.GetFocusedUI<T>();
        }

        /// <summary>
        /// 检查指定ID的UI是否处于聚焦状态
        /// </summary>
        public bool IsFocused(int id)
        {
            return _focusHandler.IsFocused(ResolveRecordId(id));
        }

        /// <summary>
        /// 检查指定类型的UI是否处于聚焦状态
        /// </summary>
        public bool IsFocused<T>() where T : UIBase
        {
            return _focusHandler.IsFocused<T>();
        }

        /// <summary>
        /// 清空当前聚焦状态
        /// </summary>
        public void ClearFocus()
        {
            _focusHandler.Clear();
        }

        /// <summary>
        /// 获取当前显示的UI字典（用于聚焦处理器回调）
        /// </summary>
        Dictionary<int, IUI> IUIFocusHandlerCallback.GetShowedDict()
        {
            return _showed;
        }

        /// <summary>
        /// 获取指定ID的已显示UI（用于聚焦处理器回调）
        /// </summary>
        IUI IUIFocusHandlerCallback.GetShownUI(int id)
        {
            return _showed.TryGetValue(id, out var ui) ? ui : null;
        }

        /// <summary>
        /// 检查指定ID的UI是否可见（用于聚焦处理器回调）
        /// </summary>
        bool IUIFocusHandlerCallback.IsVisible(int id)
        {
            return IsShowPhase(GetRecord(id));
        }
    }
}
