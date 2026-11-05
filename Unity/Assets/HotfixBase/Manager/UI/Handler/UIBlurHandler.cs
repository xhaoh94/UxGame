using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// 弹窗背景模糊：
    /// - RenderTexture：显示前把相机渲染到半分辨率 RT（零 CPU 回读），UI 材质做 9-tap 模糊；
    /// - Mask：低端机兜底，半透明纯色遮罩；
    /// - None：不显示背景。
    ///
    /// 不用 _CameraOpaqueTexture / URP RendererFeature：那条路径只有不透明内容，
    /// UI 是相机栈末尾才画的，抓不到，模糊不出 UI。
    /// </summary>
    internal sealed class UIBlurHandler
    {
        /// <summary>截图分辨率比例：模糊会掩盖低分辨率，半分辨率足够。</summary>
        public float CaptureScale { get; set; } = 0.5f;

        /// <summary>全局覆盖模式（低端机设 Mask）；null 表示按界面自己的 Blur 声明。</summary>
        public UIBlurMode? Override { get; set; }

        private UIBlurMode _currentMode = UIBlurMode.Off;
        public float BlurSize { get; set; } = 8f;
        public Color BlurTint { get; set; } = Color.white;

        private readonly UIMgr _manager;
        private readonly List<UIMgr.BlurStack> _blurStacks = new();

        private RectTransform _backdropRoot;
        private RawImage _backdropImage;
        private Material _blurMaterial;
        private Material _kawaseMaterial;
        private bool _materialsReady;

        private static readonly int KawaseOffsetId = Shader.PropertyToID("_Offset");

        private RenderTexture _pendingTexture;   // 截图完成、等待显示
        private RenderTexture _currentTexture;   // 当前背景使用的纹理

        public UIBlurHandler(UIMgr manager)
        {
            _manager = manager;
        }

        /// <summary>界面声明的模式 + 全局覆盖，得到实际使用的模式。</summary>
        private UIBlurMode ResolveMode(IUI ui)
        {
            var mode = ui != null ? ui.Blur : UIBlurMode.Off;
            if (mode == UIBlurMode.Off)
                return UIBlurMode.Off;

            return Override ?? mode;
        }

        public async UniTask PrepareBeforeShowAsync(IUI ui)
        {
            var mode = ResolveMode(ui);
            if (ui == null || mode != UIBlurMode.RenderTexture)
                return;

            // 已经在显示中的界面（重复 Show）不能截图：会把窗口自己截进背景。
            // 注意不能用 Visible 判断 —— 新建出来的界面 GameObject 默认是激活的，
            // 那时还没上屏，state 仍是 Hide。
            if (ui is UIBase { State: UIState.Show or UIState.ShowAnim })
                return;

            // 截图前就要有 Kawase 材质，否则第一次打开会跳过烘焙、背景不糊。
            // 这里只建材质，不建背景对象（那会让白底提前显示）。
            EnsureMaterials();

            // 帧末场景已经渲染完，此时渲染相机才能截到当前画面。
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            ReleaseTexture(_pendingTexture);
            _pendingTexture = BakeBlur(CaptureToRenderTexture(CaptureScale));
        }

        /// <summary>
        /// 把模糊一次性烘进纹理：截图后跑 1~3 次 Kawase（降到 1/4 分辨率），
        /// 之后 UI 材质只要单次采样。背景是静态快照，没必要每帧重复算 9 个采样点。
        /// 返回的纹理可能是传进来的 source（没装 shader / BlurSize 为 0）。
        /// </summary>
        private RenderTexture BakeBlur(RenderTexture source)
        {
            if (source == null || _kawaseMaterial == null || BlurSize <= 0f)
                return source;

            var width = Mathf.Max(1, source.width / 2);
            var height = Mathf.Max(1, source.height / 2);
            var iterations = Mathf.Clamp(Mathf.CeilToInt(BlurSize / 3f), 1, 3);

            var current = source;
            for (int i = 0; i < iterations; i++)
            {
                var dst = RenderTexture.GetTemporary(width, height, 0, source.format);
                dst.filterMode = FilterMode.Bilinear;

                _kawaseMaterial.SetFloat(KawaseOffsetId, i + 1f);
                Graphics.Blit(current, dst, _kawaseMaterial);

                if (!ReferenceEquals(current, source))
                    RenderTexture.ReleaseTemporary(current);

                current = dst;
            }

            if (!ReferenceEquals(current, source))
                ReleaseTexture(source);

            return current;
        }

        public void OnShowed(IUI ui)
        {
            var mode = ResolveMode(ui);
            if (mode == UIBlurMode.Off)
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
            _currentMode = mode;

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
                _currentMode = _blurStacks[^1].Blur;
                ShowBackdrop();
                return;
            }

            _currentMode = UIBlurMode.Off;
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

            if (_currentMode == UIBlurMode.Off)
            {
                HideBackdrop();
                return;
            }

            _backdropRoot.gameObject.SetActive(true);

            if (_currentMode == UIBlurMode.RenderTexture && _blurMaterial != null && _currentTexture != null)
            {
                ApplyMaterialParams(_blurMaterial);
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
            // 排查用：captureTex=false 时就是走了 Mask 兜底。定稿可删。
            Log.Info("[UIBlur] mode={0} captureTex={1} blurMat={2}",
                _currentMode, _currentTexture != null, _blurMaterial != null);
#endif

            RefreshBackdropSorting();
        }

        /// <summary>背景材质只剩调色；模糊已经在 <see cref="BakeBlur"/> 里烘进纹理了。</summary>
        private void ApplyMaterialParams(Material material)
        {
            if (material == null)
                return;

            material.SetColor("_BlurTint", BlurTint);
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

            EnsureMaterials();
        }

        /// <summary>材质只建一次：截图烘焙和背景显示都要用，任一缺失只告警一次。</summary>
        private void EnsureMaterials()
        {
            if (_materialsReady)
                return;

            _materialsReady = true;
            _blurMaterial = CreateBlurMaterial("Ux/UIBlur");
            // Kawase 只用于截图后离线模糊，缺了不影响显示，只是背景不糊。
            _kawaseMaterial = CreateBlurMaterial("Ux/URP_PostProcessing/KawaseBlur");
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
