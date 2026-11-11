using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>
    /// 「每帧更新」对象列表的通用容器。
    ///
    /// 用途：某个域（ECS、UI、特效…）有一批需要每帧驱动的对象时，
    /// 用它的实例 + 一条 GameMethod 入口统一遍历，避免每个对象各自挂全局委托。
    ///
    /// 特性：
    /// 1) Add/Remove 均摊 O(1)（HashSet 去重 + List 顺序遍历），
    ///    不像多播委托那样每次 Combine/Remove 都复制整条委托链；
    /// 2) Tick 只遍历本轮开始时的数量：遍历中 Add 的对象留到下一帧，
    ///    避免 OnUpdate 里不断 Add 造成同帧死循环；
    /// 3) Tick 遍历中 Remove 的对象先进 _pendingRemove，遍历结束后统一删，索引不会错位；
    /// 4) Clear 只能在非 Tick 状态下调用。
    ///
    /// 用法：
    /// <code>
    /// // ① 域内声明一个实例（整个域一个）
    /// static readonly FrameTickList&lt;IUpdateSystem&gt; _updates = new();
    ///
    /// // ② 域初始化时挂一条全局入口（整个域只挂这一条）
    /// GameMethod.Update += TickUpdate;
    ///
    /// static void TickUpdate() => _updates.Tick(_OnUpdate);
    /// static void _OnUpdate(IUpdateSystem s) => s.OnUpdate();
    ///
    /// // ③ 对象创建/销毁时登记、摘除
    /// _updates.Add(system);
    /// _updates.Remove(system);
    /// </code>
    ///
    /// 什么时候该用：对象级数量大（几十以上）、或需要成组暂停/清理时。
    /// 数量少（几~十几个）并且生命周期等于进程的，直接 GameMethod.Update += 即可，不必套这一层。
    /// </summary>
    public sealed class FrameTickList<T> where T : class
    {
        private readonly HashSet<T> _set;
        private readonly List<T> _list = new();
        private readonly List<T> _pendingRemove = new();
        private int _iterating;

        public FrameTickList() : this(null) { }

        /// <param name="comparer">默认用 EqualityComparer&lt;T&gt;.Default；需要引用相等时传 ReferenceEqualityComparer&lt;T&gt;.Instance。</param>
        public FrameTickList(IEqualityComparer<T> comparer)
        {
            _set = new HashSet<T>(comparer ?? EqualityComparer<T>.Default);
        }

        /// <summary>当前登记数量（含本轮待移除的项）。</summary>
        public int Count => _list.Count;

        public bool Contains(T item) => item != null && _set.Contains(item);

        /// <summary>登记一个对象。重复登记会被忽略。</summary>
        public void Add(T item)
        {
            if (item == null) return;
            if (_set.Add(item)) _list.Add(item);
        }

        /// <summary>摘除一个对象。Tick 遍历中调用是安全的（延迟到遍历结束再删）。</summary>
        public void Remove(T item)
        {
            if (item == null) return;
            if (!_set.Remove(item)) return;

            if (_iterating > 0) _pendingRemove.Add(item);
            else _list.Remove(item);
        }

        /// <summary>
        /// 驱动一帧。
        /// onTick 建议传静态方法或方法组（编译器会缓存委托），避免每次 Tick 分配 lambda。
        /// </summary>
        public void Tick(Action<T> onTick)
        {
            if (onTick == null || _list.Count == 0) return;

            int count = _list.Count;

            _iterating++;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    onTick(_list[i]);
                }
            }
            finally
            {
                _iterating--;
                if (_iterating == 0 && _pendingRemove.Count > 0)
                {
                    for (int i = 0; i < _pendingRemove.Count; i++)
                    {
                        _list.Remove(_pendingRemove[i]);
                    }
                    _pendingRemove.Clear();
                }
            }
        }

        /// <summary>清空列表。不要在 Tick 遍历过程中调用。</summary>
        public void Clear()
        {
            _set.Clear();
            _list.Clear();
            _pendingRemove.Clear();
            _iterating = 0;
        }
    }
}
