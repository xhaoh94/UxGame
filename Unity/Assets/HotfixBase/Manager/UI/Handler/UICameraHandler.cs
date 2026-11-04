using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Ux
{
    /// <summary>
    /// UI 相机双形态：有场景相机时切成 Overlay 挂进它的栈，没有时自己当 Base 渲染，
    /// 这样「无相机阶段 / 相机晚激活 / 宿主被销毁」UI 都画得出来。
    /// 切换只在三个确定性时机发生，不每帧轮询：相机创建、场景加载 AttachUIStack、界面上屏。
    /// </summary>
    internal sealed class UICameraHandler
    {
        /// <summary>自持 Base 时的深度：排在场景相机之前，真有世界相机出现时由它 clear 覆盖。</summary>
        private const float SelfHostedDepth = -100f;

        private Camera _uiCamera;
        private Camera _stageHost;
        private Camera[] _buffer = new Camera[8];

        public Camera UICamera => _uiCamera;

        /// <summary>创建 UI 相机（幂等），并把根 Canvas 配成 Screen Space - Camera。</summary>
        public void ConfigureRoot(Canvas canvas, Transform root)
        {
            EnsureCamera(root);
            if (canvas == null) return;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCamera;
            canvas.sortingOrder = 100;
        }

        private void EnsureCamera(Transform root)
        {
            if (_uiCamera != null) return;

            _uiCamera = new GameObject("[UICamera]", typeof(Camera)).GetComponent<Camera>();
            if (root != null)
                _uiCamera.transform.SetParent(root, false);
            _uiCamera.gameObject.layer = LayerMask.NameToLayer(Layers.UI);
            _uiCamera.orthographic = true;
            _uiCamera.orthographicSize = 5f;
            // near 为负：直挂模型跟在 UI 节点下，世界 z 可能落在相机原点身后，不扩近裁剪面会被裁掉。
            // far 要包住 Canvas 平面（planeDistance 默认 100），否则整个 UI 不渲染。
            _uiCamera.nearClipPlane = -50f;
            _uiCamera.farClipPlane = 200f;
            _uiCamera.cullingMask = LayerMask.GetMask(Layers.UI, Layers.UIModel);

            // 相机创建早于任何场景加载；Overlay 在没有宿主时什么都不画，所以先按自持 Base 起步。
            ApplySelfHosted();

            UnityEngine.Object.DontDestroyOnLoad(_uiCamera.gameObject);
            TryAttachAny();
        }

        /// <summary>场景相机加载后显式挂载（CameraComponent 调用）。</summary>
        public bool Attach(Camera baseCamera)
        {
            if (_uiCamera == null || baseCamera == null || baseCamera == _uiCamera) return false;

            var data = baseCamera.GetUniversalAdditionalCameraData();
            if (data == null || data.renderType != CameraRenderType.Base)
            {
                Log.Error($"AttachUIStack: {baseCamera.name} 不是 Base 相机，UI 相机无法挂载");
                return false;
            }

            if (_stageHost == baseCamera && data.cameraStack.Contains(_uiCamera)) return true;

            Detach();

            // 必须先切 Overlay 再入栈：Base 相机放进别人的 cameraStack，URP 直接忽略。
            _uiCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            _uiCamera.clearFlags = CameraClearFlags.Nothing;
            _uiCamera.depth = 100f;
            if (!data.cameraStack.Contains(_uiCamera))
                data.cameraStack.Add(_uiCamera);
            _stageHost = baseCamera;
            return true;
        }

        /// <summary>
        /// 还没有有效宿主时扫描一次场景里的 Base 相机；一个都没有就回到自持 Base。
        /// 界面首次上屏时调用，不每帧跑。
        /// </summary>
        public bool TryAttachAny()
        {
            if (_uiCamera == null) return false;
            if (_stageHost != null && _stageHost.isActiveAndEnabled) return true;

            var count = Camera.allCamerasCount;
            if (count > 0)
            {
                if (_buffer.Length < count)
                    _buffer = new Camera[count * 2];
                Camera.GetAllCameras(_buffer);

                for (int i = 0; i < count; i++)
                {
                    var cam = _buffer[i];
                    if (cam == null || cam == _uiCamera) continue;
                    if (!cam.isActiveAndEnabled) continue;

                    var data = cam.GetUniversalAdditionalCameraData();
                    if (data == null || data.renderType != CameraRenderType.Base) continue;

                    return Attach(cam);
                }
            }

            ApplySelfHosted();
            return false;
        }

        /// <summary>无宿主：自己当 Base。清黑底 + 只画 UI 层，避免残留上一帧的画面。</summary>
        private void ApplySelfHosted()
        {
            Detach();
            if (_uiCamera == null) return;

            var data = _uiCamera.GetUniversalAdditionalCameraData();
            if (data.renderType != CameraRenderType.Base)
                data.renderType = CameraRenderType.Base;

            _uiCamera.clearFlags = CameraClearFlags.SolidColor;
            _uiCamera.backgroundColor = Color.black;
            _uiCamera.depth = SelfHostedDepth;
        }

        private void Detach()
        {
            var host = _stageHost;
            _stageHost = null;
            if (host == null) return;

            var data = host.GetUniversalAdditionalCameraData();
            if (data != null && data.cameraStack != null)
                data.cameraStack.Remove(_uiCamera);
        }
    }
}
