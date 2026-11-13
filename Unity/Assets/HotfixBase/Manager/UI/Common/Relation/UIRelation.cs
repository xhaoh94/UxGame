using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// UGUI 版的关系组件，语义对齐 FairyGUI 的 Relations：
    /// 勾选“自身某条边 → 目标某条边”后，两者之间的距离保持不变；
    /// 勾选时按当前位置自动记录距离，所以对象不会跳动。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Ux/UI/UI Relation")]
    public sealed class UIRelation : MonoBehaviour
    {
        [Tooltip("编辑器里实时计算并写入，方便调整；关闭后只在运行时生效。")]
        [SerializeField] private bool _enableInEditor = true;

        [SerializeField] private UIRelationItem[] _items = Array.Empty<UIRelationItem>();

        private RectTransform _rect;
        private Vector2[] _targetSizeCache = Array.Empty<Vector2>();
        private Vector3[] _targetPositionCache = Array.Empty<Vector3>();
        private Vector3[] _targetScaleCache = Array.Empty<Vector3>();
        private Quaternion[] _targetRotationCache = Array.Empty<Quaternion>();
        private string[] _validationWarnings = Array.Empty<string>();
        private Vector2 _selfSizeCache;
        private Vector3 _selfPositionCache;
        private Vector3 _selfScaleCache;
        private Quaternion _selfRotationCache;
        private bool _dirty = true;
        private bool _validationDirty = true;
        private bool _applying;
        private string _lastWarning;

        public UIRelationItem[] Items
        {
            get => _items;
            set
            {
                _items = value ?? Array.Empty<UIRelationItem>();
                SetDirty();
            }
        }

        public bool EnableInEditor
        {
            get => _enableInEditor;
            set => _enableInEditor = value;
        }

        /// <summary>
        /// 是否由外部（虚拟列表测量链）主动驱动。置位后本组件不再把“自身几何被改动”当成权威：
        /// 列表每轮都会写 anchoredPosition 与交叉轴尺寸，若仍在这里 relink，
        /// 配置的距离与比例会被重记成列表摆好之后的值。驱动器的兜底应用仍然保留。
        /// </summary>
        internal bool ExternalDriven { get; set; }

        /// <summary>
        /// 自身几何被外部改写时，是否把当前几何重记成新的配置基线。
        /// 编辑器里拖动/改尺寸就是在调配置值，需要重记；运行时以关联配置为准，
        /// 外部写入不应改基线（显式改基线请调用 Relink / SetFollow* 等 API）。
        /// 置 null 时按是否播放决定；测试可强制指定以模拟播放模式。
        /// </summary>
        internal bool? RelinkOnSelfChangeOverride { get; set; }

        private bool RelinkOnSelfChange => RelinkOnSelfChangeOverride ?? !Application.isPlaying;

        private RectTransform Rect
        {
            get
            {
                if (_rect == null) _rect = transform as RectTransform;
                return _rect;
            }
        }

        private void OnEnable()
        {
            _rect = transform as RectTransform;
            SetDirty();
            UIRelationDriver.Register(this);
        }

        private void OnDisable()
        {
            UIRelationDriver.Unregister(this);
        }

        private void LateUpdate()
        {
            // 有统一驱动器时由驱动器集中执行，避免每个关系组件都各自做一次调度检查。
            if (UIRelationDriver.IsRunning) return;

            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                // 编译、资源刷新、切换播放期间不要写场景对象：
                // 此时 Unity 正在域重载/序列化，写 RectTransform 会让编辑器状态错乱甚至崩溃。
                if (!_enableInEditor ||
                    UnityEditor.EditorApplication.isCompiling ||
                    UnityEditor.EditorApplication.isUpdating ||
                    UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                ApplyIfNeeded();
#endif
                return;
            }

            ApplyIfNeeded();
        }

        public void SetDirty()
        {
            // 缓存延迟到下一次 Apply 时刷新，避免编辑器 Inspector 连续修改时反复重建数组。
            _dirty = true;
            _validationDirty = true;
        }

        /// <summary>有变化时才重算；返回是否写入了 RectTransform。</summary>
        public bool ApplyIfNeeded()
        {
            var rect = Rect;
            if (rect == null) return false;

            // 自身几何被外部改动（拖动、Inspector 输入、布局系统调整、缩放或旋转）时：
            // - 编辑器里这是“在调配置值”，需要把当前几何重记成新基线，对象不会跳动；
            // - 运行时以关联配置为准，重记会把外部写入变成新配置，所以只跟随不重记
            //   （要改基线请显式调用 Relink / SetFollow* 等 API）。
            // 目标同轴也变了时同样不重记：目标用 left/right/top/bottom 拉伸时，父面板尺寸
            // 变化会同时带动自身移动，重记等于把目标尺寸的变化吸收进比例/差值里，表现为完全不跟随。
            var selfDrifted = !ExternalDriven && SelfChanged();
            if (selfDrifted && RelinkOnSelfChange) RelinkAll(true);

            // selfDrifted 已经比过自身四个属性；运行时不重记基线，但仍要靠它触发一次“拉回配置值”。
            if (!_dirty && !selfDrifted && !TargetsChanged()) return false;
            return Apply();
        }

        /// <summary>自身的变换或尺寸是否被外部改动过（关联自身的写入会在 Apply 后刷新缓存）。</summary>
        private bool SelfChanged()
        {
            var rect = Rect;
            if (rect == null) return false;
            return rect.rect.size != _selfSizeCache ||
                   rect.position != _selfPositionCache ||
                   rect.lossyScale != _selfScaleCache ||
                   rect.rotation != _selfRotationCache;
        }

        /// <summary>
        /// 勾选了“宽/高跟随”但数值还没记录（老数据或首次勾选）时补记一次，
        /// 避免直接用 0 当比例把控件压没。
        /// </summary>
        private static void EnsureSizeRecorded(RectTransform rect, ref UIRelationItem item)
        {
            var target = item.Target;
            if (target == null) return;
            if (item.FollowWidth && item.WidthUsePercent && item.WidthRatio <= UIRelationCore.Epsilon)
                item.WidthRatio = UIRelationCore.MeasureWidthRatio(rect, target);
            if (item.FollowHeight && item.HeightUsePercent && item.HeightRatio <= UIRelationCore.Epsilon)
                item.HeightRatio = UIRelationCore.MeasureHeightRatio(rect, target);
        }

        /// <summary>立即重算一次；返回是否写入了 RectTransform。</summary>
        public bool Apply()
        {
#if UNITY_EDITOR
            // 编译、资源刷新、切换播放期间不要写场景对象：此时 Unity 正在域重载/序列化。
            // 只在非播放模式拦截；播放中（含编辑器 Play）必须放行，否则运行时关系全部不生效。
            if (!Application.isPlaying &&
                (UnityEditor.EditorApplication.isCompiling ||
                 UnityEditor.EditorApplication.isUpdating ||
                 UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode))
                return false;
#endif
            if (_applying) return false;
            _applying = true;
            try
            {
                return ApplyInternal();
            }
            finally
            {
                _applying = false;
            }
        }

        private bool ApplyInternal()
        {
            var rect = Rect;
            if (rect == null)
            {
                _dirty = false;
                return false;
            }

            EnsureCacheCapacity(_items?.Length ?? 0);
            if (_items == null || _items.Length == 0)
            {
                _dirty = false;
                _validationDirty = false;
                _validationWarnings = Array.Empty<string>();
                RefreshCaches();
                return false;
            }

            RebuildValidationCache(rect);

            var changed = false;
            for (int i = 0; i < _items.Length; i++)
            {
                ref var item = ref _items[i];
                if (item.IsEmpty) continue;
                EnsureSizeRecorded(rect, ref item);

                // 布局组的驱动会覆盖同一轴上的关联，实时判定（不缓存），冲突优先于其它校验。
                var warning = UIRelationCore.DescribeLayoutConflict(rect, item) ??
                              _validationWarnings[i];
                if (warning != null)
                {
                    if (_lastWarning != warning)
                    {
                        _lastWarning = warning;
                        Debug.LogWarning($"[UIRelation] {name}: {warning}", this);
                    }
                    continue;
                }

                if (UIRelationCore.Apply(rect, item) != RelationChange.None)
                    changed = true;
            }

            _dirty = false;
            RefreshCaches();
            return changed;
        }

        /// <summary>目标的变换/尺寸是否变化（自身部分由 SelfChanged 负责，这里不重复比较）。</summary>
        private bool TargetsChanged()
        {
            if (_items == null) return false;

            for (int i = 0; i < _items.Length; i++)
            {
                var target = _items[i].Target;
                if (i >= _targetSizeCache.Length ||
                    i >= _targetScaleCache.Length ||
                    i >= _targetRotationCache.Length)
                    return true;
                if (target == null)
                {
                    if (_targetSizeCache[i] != default ||
                        _targetPositionCache[i] != default ||
                        _targetScaleCache[i] != default ||
                        _targetRotationCache[i] != default)
                        return true;
                    continue;
                }
                if (target.rect.size != _targetSizeCache[i] ||
                    target.position != _targetPositionCache[i] ||
                    target.lossyScale != _targetScaleCache[i] ||
                    target.rotation != _targetRotationCache[i])
                    return true;
            }
            return false;
        }

        private void EnsureCacheCapacity(int length)
        {
            if (_targetSizeCache.Length == length &&
                _targetPositionCache.Length == length &&
                _targetScaleCache.Length == length &&
                _targetRotationCache.Length == length &&
                _validationWarnings.Length == length)
                return;

            _targetSizeCache = new Vector2[length];
            _targetPositionCache = new Vector3[length];
            _targetScaleCache = new Vector3[length];
            _targetRotationCache = new Quaternion[length];
            _validationWarnings = new string[length];
            _validationDirty = true;
        }

        private void RebuildValidationCache(RectTransform rect)
        {
            if (!_validationDirty) return;

            EnsureCacheCapacity(_items?.Length ?? 0);
            _lastWarning = null;
            for (int i = 0; i < _items.Length; i++)
            {
                ref var item = ref _items[i];
                EnsureSizeRecorded(rect, ref item);
                _validationWarnings[i] = UIRelationCore.Describe(rect, item);
            }
            _validationDirty = false;
        }

        private void RefreshCaches()
        {
            var rect = Rect;
            if (rect != null)
            {
                _selfSizeCache = rect.rect.size;
                _selfPositionCache = rect.position;
                _selfScaleCache = rect.lossyScale;
                _selfRotationCache = rect.rotation;
            }

            var length = _items?.Length ?? 0;
            EnsureCacheCapacity(length);
            for (int i = 0; i < length; i++)
            {
                var target = _items[i].Target;
                if (target == null)
                {
                    _targetSizeCache[i] = default;
                    _targetPositionCache[i] = default;
                    _targetScaleCache[i] = default;
                    _targetRotationCache[i] = default;
                    continue;
                }
                _targetSizeCache[i] = target.rect.size;
                _targetPositionCache[i] = target.position;
                _targetScaleCache[i] = target.lossyScale;
                _targetRotationCache[i] = target.rotation;
            }
        }

        // ------------------------------------------------------------ 运行时配置

        /// <summary>
        /// 勾选一条边对：记录当前位置的距离，之后目标变化时保持该距离。
        /// 同一目标重复调用会复用同一项。
        /// </summary>
        public void Add(RectTransform target, RelationSide selfSide, RelationSide targetSide,
            bool percent = false)
        {
            if (target == null) return;
            var rect = Rect;
            if (rect == null) return;
            if (!IsSameAxis(selfSide, targetSide))
            {
                Debug.LogWarning("[UIRelation] 自身边与目标边必须同为水平或同为垂直。", this);
                return;
            }

            var offset = UIRelationCore.MeasureOffset(rect, selfSide, target, targetSide, percent);
            int index = EnsureItem(target);
            var item = _items[index];
            var links = item.Links ?? Array.Empty<UIRelationLink>();

            for (int i = 0; i < links.Length; i++)
            {
                if (links[i].Self != selfSide || links[i].Target != targetSide) continue;
                links[i].Offset = offset;
                links[i].Percent = percent;
                item.Links = links;
                _items[index] = item;
                SetDirty();
                return;
            }

            var expanded = new UIRelationLink[links.Length + 1];
            Array.Copy(links, expanded, links.Length);
            expanded[links.Length] = new UIRelationLink
            {
                Self = selfSide,
                Target = targetSide,
                Offset = offset,
                Percent = percent,
            };
            item.Links = expanded;
            _items[index] = item;
            SetDirty();
        }

        /// <summary>取消一条边对。</summary>
        public void Remove(RectTransform target, RelationSide selfSide, RelationSide targetSide)
        {
            int index = IndexOf(target);
            if (index < 0) return;

            var item = _items[index];
            var links = item.Links;
            if (links == null) return;

            int write = 0;
            var kept = new UIRelationLink[links.Length];
            for (int i = 0; i < links.Length; i++)
                if (links[i].Self != selfSide || links[i].Target != targetSide)
                    kept[write++] = links[i];

            if (write == links.Length) return;
            if (write == 0)
            {
                item.Links = Array.Empty<UIRelationLink>();
            }
            else
            {
                Array.Resize(ref kept, write);
                item.Links = kept;
            }
            _items[index] = item;
            SetDirty();
        }

        /// <summary>重新记录目标下所有边对的当前距离（整理布局后使用）。</summary>
        public void Relink(RectTransform target)
        {
            int index = IndexOf(target);
            if (index < 0) return;
            Relink(index, false);
        }

        /// <summary>重新记录全部边对的当前距离与宽高比例。</summary>
        public void RelinkAll() => RelinkAll(false);

        /// <summary>
        /// 重新记录全部边对的距离与宽高比例。
        /// </summary>
        /// <param name="respectTargetChange">
        /// true 时逐轴跳过“目标本轮也变了”的轴：这种变化应该由跟随逻辑响应，
        /// 把自身被带着走的结果记成新基线会把目标的变化抵消掉。
        /// </param>
        private void RelinkAll(bool respectTargetChange)
        {
            if (_items == null) return;
            for (int i = 0; i < _items.Length; i++) Relink(i, respectTargetChange);
            // 重新记录距离不会改变关联结构，无需让下一帧重复做整套校验。
            _dirty = true;
        }

        private void Relink(int index, bool respectTargetChange)
        {
            var item = _items[index];
            var target = item.Target;
            var rect = Rect;
            if (target == null || rect == null) return;

            // 目标在该轴上也变了时，这一轴交给跟随逻辑，不重记基线。
            var relinkX = !respectTargetChange || !TargetChangedOn(index, true);
            var relinkY = !respectTargetChange || !TargetChangedOn(index, false);

            // 只勾了“宽 -> 宽”时 Links 可能为空，不能因此跳过比例记录。
            var links = item.Links;
            if (links != null && links.Length > 0)
            {
                for (int i = 0; i < links.Length; i++)
                {
                    var link = links[i];
                    if (!link.IsValid) continue;
                    if (link.IsHorizontal ? !relinkX : !relinkY) continue;
                    link.Offset = UIRelationCore.MeasureOffset(rect, link.Self, target, link.Target,
                        link.Percent);
                    links[i] = link;
                }
                item.Links = links;
            }
            if (item.FollowWidth && relinkX)
            {
                if (item.WidthUsePercent)
                    item.WidthRatio = UIRelationCore.MeasureWidthRatio(rect, target);
                else
                    item.WidthDelta = UIRelationCore.MeasureWidthDelta(rect, target);
            }
            if (item.FollowHeight && relinkY)
            {
                if (item.HeightUsePercent)
                    item.HeightRatio = UIRelationCore.MeasureHeightRatio(rect, target);
                else
                    item.HeightDelta = UIRelationCore.MeasureHeightDelta(rect, target);
            }
            _items[index] = item;
        }

        /// <summary>
        /// 目标在某个轴上的参照（该轴尺寸或坐标）是否相对上次应用后变了。
        /// 没有可比对的缓存（首次或数组重建）时按“变了”处理，宁可先跟随也不吞掉目标变化。
        /// </summary>
        private bool TargetChangedOn(int index, bool horizontal)
        {
            var target = _items[index].Target;
            if (target == null) return false;
            if (index >= _targetSizeCache.Length || index >= _targetPositionCache.Length) return true;
            if (horizontal)
            {
                return target.rect.width != _targetSizeCache[index].x ||
                       target.position.x != _targetPositionCache[index].x;
            }
            return target.rect.height != _targetSizeCache[index].y ||
                   target.position.y != _targetPositionCache[index].y;
        }

        public void SetPercent(RectTransform target, RelationSide selfSide, RelationSide targetSide,
            bool percent)
        {
            int index = IndexOf(target);
            if (index < 0) return;
            var item = _items[index];
            var links = item.Links;
            if (links == null) return;
            for (int i = 0; i < links.Length; i++)
            {
                if (links[i].Self != selfSide || links[i].Target != targetSide) continue;
                if (links[i].Percent == percent) return;
                links[i].Percent = percent;
                // 切换量纲时按当前距离重新记录，避免数值被当成另一种单位。
                links[i].Offset = UIRelationCore.MeasureOffset(Rect, selfSide, item.Target,
                    targetSide, percent);
                item.Links = links;
                _items[index] = item;
                SetDirty();
                return;
            }
        }

        /// <summary>
        /// 写入“宽/高跟随”的数值。只保留当前模式使用的值，另一份清零，
        /// 避免切换模式时把上一种模式的残留值带进公式。
        /// </summary>
        private static void SetFollowValue(ref UIRelationItem item, bool widthAxis, bool usePercent,
            float ratio, float delta)
        {
            if (widthAxis)
            {
                item.WidthUsePercent = usePercent;
                if (usePercent)
                {
                    item.WidthRatio = ratio;
                    item.WidthDelta = 0f;
                }
                else
                {
                    item.WidthDelta = delta;
                    item.WidthRatio = 1f;
                }
            }
            else
            {
                item.HeightUsePercent = usePercent;
                if (usePercent)
                {
                    item.HeightRatio = ratio;
                    item.HeightDelta = 0f;
                }
                else
                {
                    item.HeightDelta = delta;
                    item.HeightRatio = 1f;
                }
            }
        }

        /// <summary>
        /// 勾选“宽 -> 宽”。勾选时同时记录当前比例与差值，对象不会跳动；
        /// usePercent 决定之后按百分比还是固定差值跟随。value 传 null 时自动测量。
        /// </summary>
        public void SetFollowWidth(RectTransform target, bool follow, bool usePercent = false,
            float? value = null)
        {
            int index = IndexOf(target);
            if (index < 0) return;
            var rect = Rect;
            if (rect == null) return;

            var item = _items[index];
            item.FollowWidth = follow;
            if (follow)
            {
                SetFollowValue(ref item, true, usePercent,
                    value ?? UIRelationCore.MeasureWidthRatio(rect, target),
                    value ?? UIRelationCore.MeasureWidthDelta(rect, target));
            }
            _items[index] = item;
            SetDirty();
        }

        /// <summary>勾选“高 -> 高”，行为与 <see cref="SetFollowWidth"/> 一致。</summary>
        public void SetFollowHeight(RectTransform target, bool follow, bool usePercent = false,
            float? value = null)
        {
            int index = IndexOf(target);
            if (index < 0) return;
            var rect = Rect;
            if (rect == null) return;

            var item = _items[index];
            item.FollowHeight = follow;
            if (follow)
            {
                SetFollowValue(ref item, false, usePercent,
                    value ?? UIRelationCore.MeasureHeightRatio(rect, target),
                    value ?? UIRelationCore.MeasureHeightDelta(rect, target));
            }
            _items[index] = item;
            SetDirty();
        }

        /// <summary>
        /// 切换某条边对的距离量纲：像素 或 相对于目标尺寸的百分比。
        /// 切换时按当前实际距离换算，对象不会跳动。
        /// </summary>
        public void SetLinkPercent(RectTransform target, RelationSide selfSide,
            RelationSide targetSide, bool percent)
        {
            int index = IndexOf(target);
            if (index < 0) return;
            var rect = Rect;
            if (rect == null || target == null) return;

            var item = _items[index];
            var links = item.Links;
            if (links == null) return;
            for (int i = 0; i < links.Length; i++)
            {
                if (links[i].Self != selfSide || links[i].Target != targetSide) continue;
                if (links[i].Percent == percent) return;
                // 按当前实际距离换算到新量纲，切换后位置不变。
                links[i].Offset = UIRelationCore.MeasureOffset(rect, selfSide, target, targetSide,
                    percent);
                links[i].Percent = percent;
                item.Links = links;
                _items[index] = item;
                SetDirty();
                return;
            }
        }

        /// <summary>
        /// 切换跟随模式（百分比 / 绝对值）。
        /// 只按“两个组件此刻的实际宽高”记录数值，不会改动任何组件的尺寸：
        /// 切到百分比记录 自身宽/目标宽，切到绝对值记录 自身宽-目标宽。
        /// </summary>
        public void SetSizeFollowMode(RectTransform target, bool? widthUsePercent,
            bool? heightUsePercent)
        {
            int index = IndexOf(target);
            if (index < 0) return;
            var rect = Rect;
            if (rect == null || target == null) return;

            var item = _items[index];
            if (widthUsePercent.HasValue && item.FollowWidth)
            {
                SetFollowValue(ref item, true, widthUsePercent.Value,
                    UIRelationCore.MeasureWidthRatio(rect, target),
                    UIRelationCore.MeasureWidthDelta(rect, target));
            }
            if (heightUsePercent.HasValue && item.FollowHeight)
            {
                SetFollowValue(ref item, false, heightUsePercent.Value,
                    UIRelationCore.MeasureHeightRatio(rect, target),
                    UIRelationCore.MeasureHeightDelta(rect, target));
            }
            _items[index] = item;
            SetDirty();
        }

        /// <summary>直接修改宽高比例（编辑器里的百分比输入）。</summary>
        public void SetSizeRatio(RectTransform target, float? widthRatio, float? heightRatio)
        {
            int index = IndexOf(target);
            if (index < 0) return;
            var item = _items[index];
            if (widthRatio.HasValue) item.WidthRatio = widthRatio.Value;
            if (heightRatio.HasValue) item.HeightRatio = heightRatio.Value;
            _items[index] = item;
            SetDirty();
        }

        /// <summary>直接修改宽高差值（绝对值输入）。</summary>
        public void SetSizeDelta(RectTransform target, float? widthDelta, float? heightDelta)
        {
            int index = IndexOf(target);
            if (index < 0) return;
            var item = _items[index];
            if (widthDelta.HasValue) item.WidthDelta = widthDelta.Value;
            if (heightDelta.HasValue) item.HeightDelta = heightDelta.Value;
            _items[index] = item;
            SetDirty();
        }

        /// <summary>新增一个空的关联目标槽位，返回索引。</summary>
        public int AddTarget(RectTransform target = null)
        {
            var index = _items?.Length ?? 0;
            Array.Resize(ref _items, index + 1);
            _items[index] = CreateItem(target);
            SetDirty();
            return index;
        }

        /// <summary>修改某个槽位的关联目标。</summary>
        public void SetTarget(int index, RectTransform target)
        {
            if (_items == null || index < 0 || index >= _items.Length) return;
            _items[index].Target = target;
            SetDirty();
        }

        /// <summary>移除某个槽位。</summary>
        public void RemoveAt(int index)
        {
            if (_items == null || index < 0 || index >= _items.Length) return;
            var list = new UIRelationItem[_items.Length - 1];
            var cursor = 0;
            for (int i = 0; i < _items.Length; i++)
                if (i != index) list[cursor++] = _items[i];
            _items = list;
            SetDirty();
        }

        /// <summary>移除某个目标的全部关联。</summary>
        public void RemoveTarget(RectTransform target)
        {
            int index = IndexOf(target);
            if (index < 0) return;
            var list = new UIRelationItem[_items.Length - 1];
            var cursor = 0;
            for (int i = 0; i < _items.Length; i++)
                if (i != index) list[cursor++] = _items[i];
            _items = list;
            SetDirty();
        }

        public void Clear()
        {
            if (_items == null || _items.Length == 0) return;
            _items = Array.Empty<UIRelationItem>();
            SetDirty();
        }

        public bool Has(RectTransform target, RelationSide selfSide, RelationSide targetSide)
        {
            int index = IndexOf(target);
            if (index < 0) return false;
            var links = _items[index].Links;
            if (links == null) return false;
            for (int i = 0; i < links.Length; i++)
                if (links[i].Self == selfSide && links[i].Target == targetSide) return true;
            return false;
        }

        private int IndexOf(RectTransform target)
        {
            if (target == null || _items == null) return -1;
            for (int i = 0; i < _items.Length; i++)
                if (_items[i].Target == target) return i;
            return -1;
        }

        private int EnsureItem(RectTransform target)
        {
            int index = IndexOf(target);
            if (index >= 0) return index;
            index = _items?.Length ?? 0;
            Array.Resize(ref _items, index + 1);
            _items[index] = CreateItem(target);
            return index;
        }

        /// <summary>新关联默认使用绝对值（记录固定差值），百分比需要手动点亮 %。</summary>
        private static UIRelationItem CreateItem(RectTransform target) => new UIRelationItem
        {
            Target = target,
            Links = Array.Empty<UIRelationLink>(),
            WidthUsePercent = false,
            WidthRatio = 1f,
            HeightUsePercent = false,
            HeightRatio = 1f,
        };

        private static bool IsSameAxis(RelationSide selfSide, RelationSide targetSide)
        {
            if (UIRelationCore.IsHorizontal(selfSide) && UIRelationCore.IsHorizontal(targetSide))
                return true;
            return UIRelationCore.IsVertical(selfSide) && UIRelationCore.IsVertical(targetSide);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 只标记脏：实际计算放到 LateUpdate（编辑器模式下 _enableInEditor 时执行）。
            // 不在这里直接改 RectTransform，否则会让 Inspector 在绘制过程中被重入修改，
            // 触发 Unity 内部的样式初始化异常。
            _dirty = true;
            _validationDirty = true;
        }
#endif
    }
}
