using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ux
{
    /// <summary>
    /// 挂在 [UIRoot] 上：Canvas 根尺寸变化（分辨率 / 窗口模式 / 安全区）时，
    /// 下一帧把保存的比例布局重新应用到所有窗口。
    ///
    /// 用 UGUI 的 <see cref="UIBehaviour.OnRectTransformDimensionsChange"/> 派发，
    /// **不需要每帧 Update**，也不依赖任何平台 API。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIScreenWatcher : UIBehaviour
    {
        private Vector2 _lastSize;
        private bool _pending;

        protected override void OnRectTransformDimensionsChange()
        {
            if (transform is not RectTransform rect)
                return;

            var size = rect.rect.size;
            if (size.x <= 0f || size.y <= 0f || size == _lastSize)
                return;

            _lastSize = size;

            if (_pending)
                return;

            _pending = true;
            StartCoroutine(ApplyNextFrame());
        }

        private IEnumerator ApplyNextFrame()
        {
            // 等一帧：Canvas 尺寸在 OnRectTransformDimensionsChange 当帧可能还没最终确定。
            yield return null;

            _pending = false;

            if (UIMgr.IsCreated)
                UIMgr.Ins.Windows.ReapplyLayouts();
        }
    }
}
