using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// 弹窗背景模糊：
    /// - High：把相机渲染到半分辨率 RenderTexture（零 CPU 回读），UI 材质做 9-tap 模糊；
    /// - Low：低端机兜底，半透明纯色遮罩；
    /// - Off：不显示背景。
    ///
    /// 不用 _CameraOpaqueTexture：UI 是 Overlay 相机渲染，拿不到 Base 相机的 Copy Color 结果，
    /// 采样会返回白色。半分辨率 RT 多一次渲染，但零回读、结果稳定。
    /// </summary>
    internal sealed class UIBlurHandler
    {
        /// <summary>截图分辨率比例：模糊会掩盖低分辨率，半分辨率足够。</summary>
        public float CaptureScale { get; set; } = 0.5f;

        /// <summary>全局覆盖模式（低端机设 Mask）；null 表示按界面自己的 Blur 声明。</summary>
        public UIBlurMode? Override { get; set; }

        private UIBlurMode _currentMode = UIBlurMode.None;
        public float BlurSize { get; set; } = 8f;
        public Color BlurTint { get; set; } = Color.white;

        private readonly UIMgr _manager;
        private readonly List<UIMgr.BlurStack> _blurStacks = new();

        private RectTransform _backdropRoot;
        private RawImage _backdropImage;
        private Material _blurMaterial;
        private Material _globalBlurMaterial;

        private RenderTexture _pendingTexture;   // 截图完成、等待显示
        private RenderTexture _currentTexture;   // 当前背景使用的纹理

        public UIBlurHandler(UIMgr manager)
        {
            _manager = manager;
        }

        /// <summary>界面声明的模式 + 全局覆盖，得到实际使用的模式。</summary>
        private UIBlurMode ResolveMode(IUI ui)
        {
            var mode = ui != null ? ui.Blur : UIBlurMode.None;
            if (mode == UIBlurMode.None)
                return UIBlurMode.None;

            return Override ?? mode;
        }

        /// <summary>
        /// 实际取图方式。RendererFeature 采样的是场景不透明纹理：
        /// 没有场景 Base 相机（UI 相机自持 Base，画面里只有 UI）时那里永远是黑的，
        /// 或者 Feature 这一帧还没产出全局纹理，都要退回「显示前把相机截到 RT」。
        /// </summary>
        private UIBlurMode ResolveCaptureMode(UIBlurMode declared)
        {
            if (declared != UIBlurMode.RendererFeature)
                return declared;

            return _manager.HasSceneCamera && Shader.GetGlobalTexture(GlobalBlurTexId) != null
                ? UIBlurMode.RendererFeature
                : UIBlurMode.RenderTexture;
        }

        public async UniTask PrepareBeforeShowAsync(IUI ui)
        {
            var mode = ResolveMode(ui);
            if (ui == null || mode == UIBlurMode.None)
                return;

            // 已经在显示中的界面（重复 Show）不能截图：会把窗口自己截进背景。
            if (ui is UIBase { Visible: true })
                return;

            // 只有实际取图方式是 RenderTexture 时才需要帧末截图；RendererFeature / Mask 不需要。
            if (ResolveCaptureMode(mode) != UIBlurMode.RenderTexture)
                return;

            // 帧末场景已经渲染完，此时渲染相机才能截到当前画面。
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            ReleaseTexture(_pendingTexture);
            _pendingTexture = CaptureToRenderTexture(CaptureScale);
        }

        public void OnShowed(IUI ui)
        {
            var mode = ResolveMode(ui);
            if (mode == UIBlurMode.None)
                return;

            // 窗口上屏后 StageSorting* 一定有效；用它做快照，排序不再依赖查询。
            var baseUi = ui as UIBase;
            var sortingLayer = baseUi != null && !string.IsNullOrEmpty(baseUi.StageSortingLayerName)
                ? baseUi.StageSortingLayerName
                : "sl" + (int)UILayer.Window;
            var sortingOrder = baseUi != null && baseUi.StageSortingOrder > 0
                ? baseUi.StageSortingOrder
                : 100;


            _blurStacks.RemoveAll(x => x.ID == ui.ID);
#if UNITY_EDITOR
            _blurStacks.Add(new UIMgr.BlurStack(ui.Name, ui.ID, mode, sortingLayer, sortingOrder));
#else
            _blurStacks.Add(new UIMgr.BlurStack(ui.ID, mode, sortingLayer, sortingOrder));
#endif
            _currentMode = ResolveCaptureMode(mode);

            if (_pendingTexture != null)
            {
                ReleaseTexture(_currentTexture);
                _currentTexture = _pendingTexture;
                _pendingTexture = null;
            }

            ShowBackdrop();
        }

        public void OnHide(IUI ui)
        {
            if (ui == null)
                return;

            _blurStacks.RemoveAll(x => x.ID == ui.ID);
            if (_blurStacks.Count > 0)
            {
                _currentMode = ResolveCaptureMode(_blurStacks[^1].Blur);
                ShowBackdrop();
                return;
            }

            _currentMode = UIBlurMode.None;
            HideBackdrop();
            ReleaseTexture(_currentTexture);
            _currentTexture = null;
        }

        public void ClearSnapshots()
        {
            _blurStacks.Clear();
            ReleaseTexture(_pendingTexture);
            ReleaseTexture(_currentTexture);
            _pendingTexture = null;
            _currentTexture = null;
            HideBackdrop();
        }

        /// <summary>
        /// 背景插在「触发模糊的最上层窗口」之下：同它的 SortingLayer、order 少一半步长。
        /// 窗口重排后由 UIMgr 调用本方法重新对齐。
        /// </summary>
        internal void RefreshBackdropSorting()
        {
            if (_backdropRoot == null)
                return;

            var canvas = _backdropRoot.GetComponent<Canvas>();
            if (canvas == null)
                return;

            if (_blurStacks.Count == 0)
                return;

            var top = _blurStacks[^1];

            // 每次设置前重新打开：renderMode / 父 Canvas 变化都可能把它重置。
            canvas.overrideSorting = true;
            canvas.sortingLayerName = top.SortingLayer;
            canvas.sortingOrder = top.SortingOrder - UIStageHandler.ModelOffset;

        }

        private void ShowBackdrop()
        {
            EnsureBackdrop();
            if (_backdropRoot == null)
                return;

            if (_currentMode == UIBlurMode.None)
            {
                HideBackdrop();
                return;
            }

            _backdropRoot.gameObject.SetActive(true);

            if (_currentMode == UIBlurMode.RendererFeature &&
                _globalBlurMaterial != null &&
                TryGetGlobalBlurTexture(out var blurSource))
            {
                ApplyMaterialParams(_globalBlurMaterial, null);
                // 全局纹理不会自动带 _TexelSize，不设的话 9 个采样点重叠在中心，看起来就是没模糊。
                _globalBlurMaterial.SetVector(GlobalBlurTexelSizeId, TexelSizeOf(blurSource));
                _backdropImage.material = _globalBlurMaterial;
                _backdropImage.texture = Texture2D.whiteTexture;
                _backdropImage.color = Color.white;
            }
            else if (_currentMode == UIBlurMode.RenderTexture && _blurMaterial != null && _currentTexture != null)
            {
                ApplyMaterialParams(_blurMaterial, _currentTexture);
                _backdropImage.material = _blurMaterial;
                _backdropImage.texture = _currentTexture;
                _backdropImage.color = Color.white;
            }
            else
            {
                // Mask，或材质/纹理缺失时的兜底：半透明纯色。
                _backdropImage.material = null;
                _backdropImage.texture = Texture2D.whiteTexture;
                _backdropImage.color = new Color(0f, 0f, 0f, 0.55f);
            }

#if UNITY_EDITOR
            // 排查用：对照代码就能看出落在哪个分支（mode=RenderTexture 且 captureTex=false 即 Mask）。定稿可删。
            Log.Info("[UIBlur] mode={0} sceneCamera={1} globalTex={2} captureTex={3} blurMat={4}",
                _currentMode, _manager.HasSceneCamera,
                TryGetGlobalBlurTexture(out _), _currentTexture != null, _blurMaterial != null);
#endif

            RefreshBackdropSorting();
        }

        private static readonly int GlobalBlurTexId = Shader.PropertyToID("_UIBlurTex");
        private static readonly int GlobalBlurTexelSizeId = Shader.PropertyToID("_UIBlurTex_TexelSize");

        /// <summary>Feature 没执行（没有场景 Base 相机）时，_UIBlurTex 不存在，需要降级。</summary>
        private static bool TryGetGlobalBlurTexture(out Texture texture)
        {
            texture = Shader.GetGlobalTexture(GlobalBlurTexId);
            return texture != null;
        }

        private static Vector4 TexelSizeOf(Texture texture)
        {
            return new Vector4(1f / texture.width, 1f / texture.height, texture.width, texture.height);
        }

        private void ApplyMaterialParams(Material material, Texture texture)
        {
            if (material == null)
                return;

            material.SetFloat("_BlurSize", Mathf.Max(0f, BlurSize));
            material.SetColor("_BlurTint", BlurTint);

            // _MainTex_TexelSize 不一定随 RawImage 的纹理自动更新，
            // 为 0 时 9 个采样点会重合在中心，看起来就是“没有模糊”。
            if (texture != null && texture.width > 0 && texture.height > 0)
                material.SetVector("_MainTex_TexelSize", TexelSizeOf(texture));
        }

        /// <summary>
        /// 相机渲染到半分辨率 RT，不做 ReadPixels：RT 直接交给 UI 材质采样。
        /// </summary>
        private RenderTexture CaptureToRenderTexture(float scale)
        {
            var camera = _manager.BlurCaptureCamera;
            if (camera == null)
                return null;

            scale = Mathf.Clamp(scale, 0.1f, 1f);
            var width = Mathf.Max(1, Mathf.RoundToInt(camera.pixelWidth * scale));
            var height = Mathf.Max(1, Mathf.RoundToInt(camera.pixelHeight * scale));

            // 截图前收起旧背景，避免把上一层模糊图又截进去（递归模糊）。
            var backdropVisible = _backdropRoot != null && _backdropRoot.gameObject.activeSelf;
            if (backdropVisible)
                _backdropRoot.gameObject.SetActive(false);

            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prevTarget = camera.targetTexture;

            try
            {
                camera.targetTexture = rt;
                camera.Render();
                return rt;
            }
            catch (System.Exception ex)
            {
                Log.Warning("UI 模糊截图（相机渲染）失败，已降级为半透明遮罩：{0}", ex.Message);
                RenderTexture.ReleaseTemporary(rt);
                return null;
            }
            finally
            {
                camera.targetTexture = prevTarget;

                if (backdropVisible)
                    _backdropRoot.gameObject.SetActive(true);
            }
        }

        private void EnsureBackdrop()
        {
            if (_backdropRoot != null)
                return;

            // 背景自己是独立 Canvas：各界面都有了自己的 Canvas + SortingLayer 后，
            // 挂在层容器下不加 Canvas 会被所有界面压在下面。
            var go = new GameObject("BlurBackdrop",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            // 保持默认 renderMode（跟随父 Canvas），只开 overrideSorting 控制排序。
            // 显式改 ScreenSpaceCamera 会让这个子 Canvas 不渲染。
            canvas.overrideSorting = true;

            _backdropRoot = go.transform as RectTransform;
            // 背景独立于层容器，直接挂根 Canvas 下：
            // 层容器的 Canvas 设置（ScreenSpaceCamera/overrideSorting）会影响子 Canvas，
            // 挂根节点后背景排序完全由自己的 Canvas 控制。
            _backdropRoot.SetParent(UIMgr.Ins.GetUILayer(UILayer.Root), false);
            _backdropRoot.gameObject.SetLayer(LayerMask.NameToLayer(Layers.UI));
            _backdropRoot.anchorMin = Vector2.zero;
            _backdropRoot.anchorMax = Vector2.one;
            _backdropRoot.offsetMin = Vector2.zero;
            _backdropRoot.offsetMax = Vector2.zero;

            var imageGo = new GameObject("BackdropImage", typeof(RectTransform), typeof(RawImage));
            var rect = imageGo.transform as RectTransform;
            rect.SetParent(_backdropRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _backdropImage = imageGo.GetComponent<RawImage>();
            // RawImage 的 texture 为 null 时 UGUI 不会生成绘制，先给白纹理占位。
            _backdropImage.texture = Texture2D.whiteTexture;
            _backdropImage.raycastTarget = true;

            _blurMaterial = CreateBlurMaterial("Ux/UIBlur");
            _globalBlurMaterial = CreateBlurMaterial("Ux/UIBlurGlobal");

        }

        private static Material CreateBlurMaterial(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Log.Warning("找不到 {0} Shader，模糊背景将降级为半透明遮罩。" +
                            "请把该 Shader 加入 Always Included Shaders 或资源收集。", shaderName);
                return null;
            }

            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        private void HideBackdrop()
        {
            if (_backdropRoot != null)
                _backdropRoot.gameObject.SetActive(false);
        }

        private static void ReleaseTexture(Texture texture)
        {
            if (texture == null)
                return;

            if (texture is RenderTexture rt)
            {
                RenderTexture.ReleaseTemporary(rt);
                return;
            }

            Object.Destroy(texture);
        }
    }
}
