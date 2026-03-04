#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrainyBlurRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;      // 设置Pass渲染的位置-初始值
    public Shader shader;
    public class GrainyBlurPass : ScriptableRenderPass
    {
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int BufferRT = Shader.PropertyToID("_BufferRT");

        private const string PROFILER_TAG = "GrainyBlur";
        #if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
        #endif
        private GrainyBlur _grainyBlur;
        private Material grainyBlurMat;

        public GrainyBlurPass(RenderPassEvent evet, Shader shader)
        {
            renderPassEvent = evet;            
            if (shader)
            {
                grainyBlurMat = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        #if !UNITY_2023_3_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (grainyBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _grainyBlur = stack.GetComponent<GrainyBlur>();
            if (_grainyBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_grainyBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;

                cmd.BeginSample(PROFILER_TAG);

                int RTWidth = (int) (camera.pixelWidth / _grainyBlur.RTDownScaling.value);
                int RTHeight = (int) (camera.pixelHeight / _grainyBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(BufferRT, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                // downsample screen copy into smaller RT
                cmd.Blit(source, BufferRT);

                grainyBlurMat.SetVector(Params,
                    new Vector2(_grainyBlur.BlurRadius.value / camera.pixelHeight, _grainyBlur.Iteration.value));

                cmd.Blit(BufferRT, source, grainyBlurMat, 0);

                cmd.ReleaseTemporaryRT(BufferRT);
                cmd.EndSample(PROFILER_TAG);
            }
        }

        public void Setup(in RenderTargetIdentifier currentTarget)
        {
            this.currentTarget = currentTarget;
        }
        #else
        private class PassData
        {
            public TextureHandle src;
            public Material material;
            public Vector2 paramsVector;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (grainyBlurMat == null) return;
            
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;
            
            var stack = VolumeManager.instance.stack;
            _grainyBlur = stack.GetComponent<GrainyBlur>();
            if (_grainyBlur == null || !_grainyBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.width = (int)(cameraData.camera.pixelWidth / _grainyBlur.RTDownScaling.value);
            desc.height = (int)(cameraData.camera.pixelHeight / _grainyBlur.RTDownScaling.value);
            desc.depthBufferBits = 0;

            TextureHandle bufferRT = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT", false);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + "_Copy", out var passData))
            {
                passData.src = source;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(bufferRT, 0, AccessFlags.Write);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + "_Blur", out var passData))
            {
                passData.src = bufferRT;
                passData.material = grainyBlurMat;
                passData.paramsVector = new Vector2(_grainyBlur.BlurRadius.value / cameraData.camera.pixelHeight, _grainyBlur.Iteration.value);

                builder.UseTexture(bufferRT, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetVector(Params, data.paramsVector);
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }
        #endif

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
        }
    }

    private GrainyBlurPass _grainyBlurPass;

    public override void Create()
    {
        _grainyBlurPass = new GrainyBlurPass(renderPassEvent, shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_grainyBlurPass);
    }
    #if !UNITY_2023_3_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _grainyBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        base.SetupRenderPasses(renderer, renderingData);
    }
    #endif
}

