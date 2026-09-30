using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 客户端生成物表现映射。逻辑参数只保存在 CombatSpawnProfile；本类型把生成物关联到客户端资源。
    /// 与 CombatActionPresentation 同构：直接以资产引用做键，不引入额外 ID。
    /// </summary>
    [Serializable]
    public sealed class CombatSpawnPresentation
    {
        [SerializeField] private CombatSpawnProfile spawn;
        [SerializeField] private GameObject prefab;
        [SerializeField] private TimelineAsset timeline;

        public CombatSpawnPresentation()
        {
        }

        public CombatSpawnPresentation(CombatSpawnProfile spawn, GameObject prefab, TimelineAsset timeline)
        {
            this.spawn = spawn;
            this.prefab = prefab;
            this.timeline = timeline;
        }

        public CombatSpawnProfile Spawn => spawn;

        /// <summary>表现对象。为空时由表现层退化为占位对象。</summary>
        public GameObject Prefab => prefab;

        /// <summary>生成物自身的表现时间轴，可为空（只用 prefab 自带的粒子）。</summary>
        public TimelineAsset Timeline => timeline;

        public bool Matches(CombatSpawnProfile target)
        {
            return spawn == target;
        }
    }
}
