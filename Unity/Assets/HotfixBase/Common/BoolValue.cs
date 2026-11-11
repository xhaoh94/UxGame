using System;

namespace Ux
{
    /// <summary>
    /// 多来源布尔开关：任一来源置位即视为关闭，全部清零才是 true。
    /// reason 位由业务各自定义（1UL &lt;&lt; n），不集中放枚举，避免跨模块耦合。
    /// </summary>
    [EEViewer]
    public sealed class BoolValue
    {
        [NonEEViewer]
        ulong _reasons;
        Action<bool> _onChanged;

        public BoolValue(Action<bool> onChanged)
        {
            _onChanged = onChanged;
        }

        /// <summary>已置位的屏蔽位，十六进制便于按位核对。</summary>
        [EEViewer("屏蔽位")]
        public string ReasonMask => "0x" + _reasons.ToString("X");

        public bool Value => _reasons == 0;

        /// <summary>v 表示该来源是否放行：false 置位屏蔽，true 清除。</summary>
        public void Set(ulong reason, bool v)
        {
            ulong prev = _reasons;
            _reasons = v ? _reasons & ~reason : _reasons | reason;
            if ((prev == 0) != (_reasons == 0))
            {
                _onChanged?.Invoke(_reasons == 0);
            }
        }

        public void Reset()
        {
            _reasons = 0;
        }

        public void Release()
        {
            _reasons = 0;
            _onChanged = null;
        }
    }
}