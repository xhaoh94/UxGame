using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 弹窗背景模糊的业务门面：参数入口 + 截图相机选择。
    /// 具体实现见 <see cref="UIBlurHandler"/>。
    /// </summary>
    public partial class UIMgr
    {
        /// <summary>
        /// 模糊强度（0 关闭，1~8 递增）。控制截图后 Kawase 的迭代次数，
        /// 不是每帧的采样半径——模糊已经烘在纹理里了。
        /// </summary>
        public float BlurSize
        {
            get => _blurHandler.BlurSize;
            set => _blurHandler.BlurSize = value;
        }

        /// <summary>
        /// 全局覆盖弹窗背景模糊模式；null 表示按界面自己的 UIBase.Blur 声明。
        /// 低端机设 Mask 即可把所有已声明模糊的界面降级为半透明遮罩。
        /// </summary>
        public UIBlurMode? BlurModeOverride
        {
            get => _blurHandler.Override;
            set => _blurHandler.Override = value;
        }

        /// <summary>模糊截图用的相机：优先场景 Base（含 UI 栈），否则 UI 相机。</summary>
        internal Camera BlurCaptureCamera =>
            _cameraHandler.StageHost != null && _cameraHandler.StageHost.isActiveAndEnabled
                ? _cameraHandler.StageHost
                : _cameraHandler.UICamera;
    }
}
