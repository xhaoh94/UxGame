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
    }
}
