using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 动作内所有逻辑窗口的公共部分：稳定身份与 [StartFrame, EndFrame) 半开帧区间。
    ///
    /// 区间语义只在这里定义一次，子类只补自己的业务字段。字段名与历史资产保持一致，
    /// 旧资产反序列化后直接落到这些字段上，因此继承层次对现有资源是透明的。
    /// </summary>
    [Serializable]
    public abstract class CombatLogicWindow
    {
        [SerializeField, HideInInspector] private string stableId = string.Empty;
        [Min(0)] public int StartFrame;
        [Min(1)] public int EndFrame = 1;

        public string StableId => stableId;

        /// <summary>当前帧是否落在窗口内。半开区间：[StartFrame, EndFrame)。</summary>
        public bool ContainsFrame(int actionFrame)
        {
            return actionFrame >= StartFrame && actionFrame < EndFrame;
        }

        public virtual void ValidateData()
        {
            if (string.IsNullOrEmpty(stableId))
            {
                RegenerateStableId();
            }
            StartFrame = Mathf.Clamp(StartFrame, 0, int.MaxValue - 1);
            EndFrame = (int)Math.Min(
                int.MaxValue,
                Math.Max((long)StartFrame + 1, EndFrame));
        }

        internal void RegenerateStableId()
        {
            stableId = Guid.NewGuid().ToString("N");
        }
    }
}
