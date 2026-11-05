using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

/// <summary>
/// UI 背景模糊（Renderer Feature 版）：
/// 在 Base 相机渲染完透明物体后，把 _CameraOpaqueTexture 降采样复制到一个全局纹理
/// <c>_UIBlurTex</c>，UI 材质（Ux/UIBlurGlobal）采样它做模糊。
///
/// 为什么不直接在 UI 材质里采样 _CameraOpaqueTexture：
/// 它只在 Base 相机阶段有效，Overlay UI 相机渲染时拿不到（Unity 6 / URP 17 下是白色或黑色）。
///
/// 用法：把这个 Feature 加到 URP Renderer（如 NormalAssetRenderer），
/// 然后把 UIMgr.Ins.BlurSource 设为 RendererFeature。
/// </summary>
public sealed class UIBlurRendererFeature : ScriptableRendererFeature
{
    public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;

    [Range(1, 4)]
    [Tooltip("降采样倍数：2 = 半分辨率，越大越省，模糊也越明显。")]
    public int DownSample = 2;

    private UIBlurPass _pass;

    public override void Create()
    {
        _pass = new UIBlurPass(passEvent, DownSample);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;
        if (camera == null || camera.cameraType == CameraType.Preview)
            return;

        // UICameraHandler 在“无场景相机”时会自持一个 Base 相机（[UICamera]），
        // 它只渲染 UI，没有可模糊的场景内容；在它上面执行只会生成全黑纹理。
        if (camera.name == "[UICamera]")
            return;

        // 只让 Base 相机生成 _UIBlurTex。
        // Overlay UI 相机如果也执行，会用自己（空）的 OpaqueTexture 覆盖全局纹理。
        var cameraData = camera.GetUniversalAdditionalCameraData();
        if (cameraData == null || cameraData.renderType != CameraRenderType.Base)
            return;

        renderer.EnqueuePass(_pass);
    }

    private sealed class UIBlurPass : ScriptableRenderPass
    {
        private static readonly int BlurTexId = Shader.PropertyToID("_UIBlurTex");
        private readonly int _downSample;

        public UIBlurPass(RenderPassEvent evt, int downSample)
        {
            renderPassEvent = evt;
            _downSample = Mathf.Clamp(downSample, 1, 4);
        }

#if UNITY_2023_3_OR_NEWER
        private sealed class PassData
        {
            public TextureHandle Src;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            // 优先用不透明纹理（不含透明物体）；没有就退化为当前颜色纹理。
            var source = resourceData.cameraOpaqueTexture;
            if (!source.IsValid())
                source = resourceData.activeColorTexture;
            if (!source.IsValid())
                return;

            var desc = cameraData.cameraTargetDescriptor;
            desc.width = Mathf.Max(1, desc.width / _downSample);
            desc.height = Mathf.Max(1, desc.height / _downSample);
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.useMipMap = false;

            var destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_UIBlurTex", false);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("UIBlurCopy", out var passData))
            {
                passData.Src = source;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                // 关键：pass 结束后保留为全局纹理，供后续 UI 渲染采样。
                // 纹素尺寸不能在这里 SetGlobalVector —— raster pass 默认禁止改全局状态，
                // 抛异常会让整张 RenderGraph 被重置（表现就是整个 3D 画面全黑）。
                // 由 UIBlurHandler 按实际纹理写进材质属性。
                builder.SetGlobalTextureAfterPass(destination, BlurTexId);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.Src, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }
        }
#else
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview)
                return;

            var cmd = CommandBufferPool.Get("UIBlurCopy");
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.width = Mathf.Max(1, desc.width / _downSample);
            desc.height = Mathf.Max(1, desc.height / _downSample);
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.useMipMap = false;

            cmd.GetTemporaryRT(BlurTexId, desc, FilterMode.Bilinear);
            cmd.Blit(BuiltinRenderTextureType.CameraTarget, BlurTexId);
            cmd.SetGlobalTexture(BlurTexId, BlurTexId);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#endif
    }
}
