using System;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public class ParticleClipAsset : TimelineClipAsset
    {
        /// <summary>Clip 激活时覆盖粒子系统的起始颜色。特效属于表现数据，只存在 Timeline 里。</summary>
        public Color startColor = Color.white;

        /// <summary>
        /// 位姿按"相对美术摆放值的偏移"解释：positionOffset 加到 localPosition，
        /// rotationEuler 叠乘到 localRotation。零值即不动，所以旧资产缺字段时行为不变。
        /// </summary>
        public Vector3 positionOffset;
        public Vector3 rotationEuler;

        /// <summary>等比缩放倍率，乘到 localScale。旧资产反序列化为 0，读取时 &lt;= 0 一律按 1 处理。</summary>
        public float scaleFactor = 1f;

        public override Type ClipType => typeof(TLParticleClip);
    }
}
