#if UNITY_EDITOR
using System.Collections.Generic;

namespace Ux
{
    /// <summary>
    /// 编辑器侧的 ECS 实体登记表。
    ///
    /// 只用于调试窗口 / 搜索，不持有任何场景对象：
    /// 运行时 Entity 不依赖任何编辑器类型，实体可视化全部走 EntityDebuggerWindow。
    /// </summary>
    public static class EditorEntityRegistry
    {
        private static readonly List<Entity> _all = new();
        private static readonly Dictionary<Entity, int> _index = new();

        /// <summary>当前活着的所有实体（顺序不保证，销毁时会 swap-remove）。</summary>
        public static IReadOnlyList<Entity> All => _all;

        public static int Count => _all.Count;

        public static void Register(Entity entity)
        {
            if (entity == null || _index.ContainsKey(entity)) return;

            _index[entity] = _all.Count;
            _all.Add(entity);
        }

        public static void Unregister(Entity entity)
        {
            if (entity == null) return;
            if (!_index.TryGetValue(entity, out var index)) return;

            int last = _all.Count - 1;
            var moved = _all[last];
            _all[index] = moved;
            _index[moved] = index;
            _all.RemoveAt(last);
            _index.Remove(entity);
        }
    }
}
#endif
