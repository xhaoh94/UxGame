using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class RadialBlurRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    public Shader shader;

    public class RadialBlurPass : ScriptableRenderPass
    {
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int TempID = Shader.PropertyToID("_Temp");
        private const string PROFILER_TAG = "RadialBlur";
        private Material radialBlurMat;
        private RadialBlur radialBlur;

        #if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
        #endif

        public RadialBlurPass(RenderPassEvent evet, Shader shader)
        {
            renderPassEvent = evet;
            Shader radialBlurShader = Shader.Find("Ux/URP_PostProcessing/RadialBlur");
            if (shader)
            {
                radialBlurMat = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        #if !UNITY_2023_3_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (radialBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            radialBlur = stack.GetComponent<RadialBlur>();
            if (radialBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (radialBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);
                cmd.GetTemporaryRT(TempID, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear);
                cmd.Blit(source, TempID);
                radialBlurMat.SetVector(Params, new Vector4(radialBlur.BlurRadius.value * 0.02f, radialBlur.Iteration.value, radialBlur.RadialCenterX.value, radialBlur.RadialCenterY.value));
                cmd.Blit(TempID, source, radialBlurMat, 0);
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
            public int passIndex;
            public Vector4 parameters;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (radialBlurMat == null) return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;

            var stack = VolumeManager.instance.stack;
            radialBlur = stack.GetComponent<RadialBlur>();
            if (radialBlur == null || !radialBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.width = cameraData.camera.pixelWidth;
            desc.height = cameraData.camera.pixelHeight;
            desc.depthBufferBits = 0;

            TextureHandle tempRT = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_Temp", false);

            Vector4 parameters = new Vector4(radialBlur.BlurRadius.value * 0.02f, radialBlur.Iteration.value, radialBlur.RadialCenterX.value, radialBlur.RadialCenterY.value);

            // First pass - copy source to temp
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + "_Copy", out var passData))
            {
                passData.src = source;
                passData.material = radialBlurMat;
                passData.passIndex = 0;
                passData.parameters = parameters;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(tempRT, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }

            // Second pass - apply radial blur
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + "_Blur", out var passData))
            {
                passData.src = tempRT;
                passData.material = radialBlurMat;
                passData.passIndex = 0;
                passData.parameters = parameters;

                builder.UseTexture(tempRT, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetVector(Params, data.parameters);
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                });
            }
        }
        #endif

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
        }
    }

    private RadialBlurPass _RadialBlurPass;

    public override void Create()
    {
        _RadialBlurPass = new RadialBlurPass(renderPassEvent, shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_RadialBlurPass);
    }

    #if !UNITY_2023_3_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _RadialBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        base.SetupRenderPasses(renderer, renderingData);
    }
    #endif
}
