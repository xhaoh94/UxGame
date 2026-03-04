using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class GaussianBlurRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;      // 设置Pass渲染的位置-初始值
    public Shader shader;
    public class GaussianBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurRadius = Shader.PropertyToID("_BlurOffset");
        internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
        internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");

        private const string PROFILER_TAG = "GaussianBlur";
#if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
#endif
private GaussianBlur _gaussianBlur;

        const int k_MaxPyramidSize = 16;

        internal static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");
        private Material gaussianBlurMat;

        public GaussianBlurPass(RenderPassEvent evt, Shader shader)
        {
            renderPassEvent = evt;
            if (shader)
            {
                gaussianBlurMat = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        #if !UNITY_2023_3_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (gaussianBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _gaussianBlur = stack.GetComponent<GaussianBlur>();
            if (_gaussianBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_gaussianBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;

                cmd.BeginSample(PROFILER_TAG);

                int RTWidth = (int)(camera.pixelWidth / _gaussianBlur.RTDownScaling.value);
                int RTHeight = (int)(camera.pixelHeight / _gaussianBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                cmd.GetTemporaryRT(BufferRT2, RTWidth, RTHeight, 0, FilterMode.Bilinear);

                // downsample screen copy into smaller RT
                cmd.Blit(source, BufferRT1);


                for (int i = 0; i < _gaussianBlur.Iteration.value; i++)
                {
                    // horizontal blur
                    gaussianBlurMat.SetVector(BlurRadius,
                        new Vector4(_gaussianBlur.BlurRadius.value / camera.pixelWidth, 0, 0, 0));
                    cmd.Blit(BufferRT1, BufferRT2, gaussianBlurMat, 0);

                    // vertical blur
                    gaussianBlurMat.SetVector(BlurRadius,
                        new Vector4(0, _gaussianBlur.BlurRadius.value / camera.pixelHeight, 0, 0));
                    cmd.Blit(BufferRT2, BufferRT1, gaussianBlurMat, 0);
                }

                // Render blurred texture in blend pass
                cmd.Blit(BufferRT1, source, gaussianBlurMat, 1);

                // release
                cmd.ReleaseTemporaryRT(BufferRT1);
                cmd.ReleaseTemporaryRT(BufferRT2);

                cmd.EndSample(PROFILER_TAG);
            }
        }

        public void Setup(in RenderTargetIdentifier currentTarget)
        {
            this.currentTarget = currentTarget;
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
        }
#else
        private class PassData
        {
            public TextureHandle src;
            public TextureHandle dst;
            public Material material;
            public int passIndex;
            public Vector4 blurRadius;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (gaussianBlurMat == null) return;
            
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;
            
            var stack = VolumeManager.instance.stack;
            _gaussianBlur = stack.GetComponent<GaussianBlur>();
            if (_gaussianBlur == null || !_gaussianBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.width = (int)(cameraData.camera.pixelWidth / _gaussianBlur.RTDownScaling.value);
            desc.height = (int)(cameraData.camera.pixelHeight / _gaussianBlur.RTDownScaling.value);
            desc.depthBufferBits = 0;

            TextureHandle rt1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT1", false);
            TextureHandle rt2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT2", false);

            AddBlitPass(renderGraph, source, rt1, null, -1, Vector4.zero);

            int iterations = _gaussianBlur.Iteration.value;

            for (int i = 0; i < iterations; i++)
            {
                Vector4 horizontalBlur = new Vector4(_gaussianBlur.BlurRadius.value / cameraData.camera.pixelWidth, 0, 0, 0);
                AddBlitPass(renderGraph, rt1, rt2, gaussianBlurMat, 0, horizontalBlur);

                Vector4 verticalBlur = new Vector4(0, _gaussianBlur.BlurRadius.value / cameraData.camera.pixelHeight, 0, 0);
                AddBlitPass(renderGraph, rt2, rt1, gaussianBlurMat, 0, verticalBlur);
            }

            AddBlitPass(renderGraph, rt1, source, gaussianBlurMat, 1, Vector4.zero);
        }

        private void AddBlitPass(RenderGraph renderGraph, TextureHandle src, TextureHandle dst, Material mat, int passIndex, Vector4 blurRadius)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG, out var passData))
            {
                passData.src = src;
                passData.dst = dst;
                passData.material = mat;
                passData.passIndex = passIndex;
                passData.blurRadius = blurRadius;

                builder.UseTexture(src, AccessFlags.Read);
                builder.SetRenderAttachment(dst, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if (data.blurRadius != Vector4.zero && data.material != null)
                    {
                        data.material.SetVector(BlurRadius, data.blurRadius);
                    }
                    
                    if (data.material == null || data.passIndex == -1)
                    {
                        Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0.0f, false);
                    }
                    else
                    {
                        Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                    }
                });
            }
        }
#endif
    }

    private GaussianBlurPass _gaussianBlurPass;

    public override void Create()
    {
        _gaussianBlurPass = new GaussianBlurPass(renderPassEvent, shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_gaussianBlurPass);
    }
#if !UNITY_2023_3_OR_NEWER
public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
{
_gaussianBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
base.SetupRenderPasses(renderer, renderingData);
}
#endif
}

