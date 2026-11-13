using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 关系更新调度器。挂在 [UI Root] 上，用一个 LateUpdate 统一驱动所有 UIRelation，
    /// 并在同一帧内多轮收敛链式关系（A→B→C），避免逐个组件派发消息和逐帧爬。
    /// 运行时和编辑器预览都使用统一调度；没有该组件时 UIRelation 会回退到自己的 LateUpdate。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Ux/UI/UI Relation Driver")]
    public sealed class UIRelationDriver : MonoBehaviour
    {
        /// <summary>单帧最多收敛轮数，防止异常关系导致长时间循环。</summary>
        private const int MaxPasses = 4;

        private static readonly List<UIRelation> Relations = new(64);
        private static UIRelationDriver _instance;

        /// <summary>是否已有驱动实例；UIRelation 据此决定是否自己 LateUpdate。</summary>
        public static bool IsRunning => _instance != null;

        internal static void Register(UIRelation relation)
        {
            if (relation == null) return;
            if (!Relations.Contains(relation))
                Relations.Add(relation);
        }

        internal static void Unregister(UIRelation relation)
        {
            if (relation == null) return;
            Relations.Remove(relation);
        }

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
        }

        private void OnDestroy()
        {
            // 不清空 Relations：驱动器销毁后，关系组件仍需保留注册，
            // 这样新驱动器创建时可以继续接管已有组件；失效对象会在 LateUpdate 中移除。
            if (_instance == this)
                _instance = null;
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying &&
                (UnityEditor.EditorApplication.isCompiling ||
                 UnityEditor.EditorApplication.isUpdating ||
                 UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode))
                return;
#endif

            // 编辑器模式也由统一驱动器执行，保留实时预览的同时避免每个组件各自调度。
            for (int pass = 0; pass < MaxPasses; pass++)
            {
                var anyChanged = false;
                for (int i = Relations.Count - 1; i >= 0; i--)
                {
                    var relation = Relations[i];
                    if (relation == null)
                    {
                        Relations.RemoveAt(i);
                        continue;
                    }
                    if (!Application.isPlaying && !relation.EnableInEditor)
                        continue;
                    if (relation.ApplyIfNeeded())
                        anyChanged = true;
                }
                if (!anyChanged) break;
            }
        }

        /// <summary>
        /// 应用一轮：遍历所有关系一次，不自己循环。
        /// 供测量链做“每轮推进一步”的收敛，避免内层多轮 × 外层多轮的乘积。
        /// 返回本轮是否有写入。
        /// </summary>
        internal static bool ApplyPass(IReadOnlyList<UIRelation> relations)
        {
            if (relations == null || relations.Count == 0) return false;

            bool anyChanged = false;
            for (int i = 0; i < relations.Count; i++)
            {
                var relation = relations[i];
                // 未激活节点不参与布局，写入只会白费开销。
                if (relation == null || !relation.isActiveAndEnabled) continue;
                if (relation.Apply()) anyChanged = true;
            }
            return anyChanged;
        }

        /// <summary>
        /// 同步应用一批关系并多轮收敛链式关系（A→B→C）。供不需要重建布局的调用方使用。
        /// 内部复用 ApplyPass，两个入口不会分叉。
        /// 走 Apply() 而不是 ApplyIfNeeded()：不刷新基线，以配置的距离/比例为权威。
        /// </summary>
        internal static bool ApplyImmediate(IReadOnlyList<UIRelation> relations, int maxPasses = 4)
        {
            if (relations == null || relations.Count == 0 || maxPasses <= 0) return false;

            var changed = false;
            for (int pass = 0; pass < maxPasses; pass++)
            {
                if (!ApplyPass(relations)) break;
                changed = true;
            }
            return changed;
        }
    }
}
