using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class TiltShiftBlurRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    public Shader shader;

    public class TiltShiftBlurPass : ScriptableRenderPass
    {
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int BlurredTex = Shader.PropertyToID("_BlurredTex");
        internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
        internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");

        private const string PROFILER_TAG = "TiltShiftBlur";
        private Material tiltShiftBlurMat;
        private TiltShiftBlur tiltShiftBlur;

        #if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
        #endif

        public TiltShiftBlurPass(RenderPassEvent evet, Shader shader)
        {
            renderPassEvent = evet;
            if (shader)
            {
                tiltShiftBlurMat = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        #if !UNITY_2023_3_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (tiltShiftBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            tiltShiftBlur = stack.GetComponent<TiltShiftBlur>();
            if (tiltShiftBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (tiltShiftBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);

                if (tiltShiftBlur.Iteration.value == 1)
                {
                    int RTWidth = (int)(camera.pixelWidth / tiltShiftBlur.RTDownScaling.value);
                    int RTHeight = (int)(camera.pixelHeight / tiltShiftBlur.RTDownScaling.value);
                    cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                    tiltShiftBlurMat.SetVector(Params, new Vector2(tiltShiftBlur.AreaSize.value, tiltShiftBlur.BlurRadius.value));
                    cmd.Blit(source, BufferRT1, tiltShiftBlurMat, (int)tiltShiftBlur.QualityLevel.value);
                    cmd.SetGlobalTexture(BlurredTex, BufferRT1);
                    cmd.Blit(BufferRT1, source, tiltShiftBlurMat, 2);
                    cmd.ReleaseTemporaryRT(BufferRT1);
                }
                else
                {
                    int RTWidth = (int)(camera.pixelWidth / tiltShiftBlur.RTDownScaling.value);
                    int RTHeight = (int)(camera.pixelHeight / tiltShiftBlur.RTDownScaling.value);
                    cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(BufferRT2, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                    tiltShiftBlurMat.SetVector(Params, new Vector2(tiltShiftBlur.AreaSize.value, tiltShiftBlur.BlurRadius.value));
                    RenderTargetIdentifier finalBlurID = BufferRT1;
                    RenderTargetIdentifier firstID = source;
                    RenderTargetIdentifier secondID = BufferRT1;
                    for (int i = 0; i < tiltShiftBlur.Iteration.value; i++)
                    {
                        cmd.Blit(firstID, secondID, tiltShiftBlurMat, (int)tiltShiftBlur.QualityLevel.value);
                        finalBlurID = secondID;
                        firstID = secondID;
                        secondID = (secondID == BufferRT1) ? BufferRT2 : BufferRT1;
                    }
                    cmd.SetGlobalTexture(BlurredTex, finalBlurID);
                    cmd.Blit(finalBlurID, source, tiltShiftBlurMat, 2);
                    cmd.ReleaseTemporaryRT(BufferRT1);
                    cmd.ReleaseTemporaryRT(BufferRT2);
                }

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
            public Vector2 parameters;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (tiltShiftBlurMat == null) return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;

            var stack = VolumeManager.instance.stack;
            tiltShiftBlur = stack.GetComponent<TiltShiftBlur>();
            if (tiltShiftBlur == null || !tiltShiftBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            int RTWidth = (int)(cameraData.camera.pixelWidth / tiltShiftBlur.RTDownScaling.value);
            int RTHeight = (int)(cameraData.camera.pixelHeight / tiltShiftBlur.RTDownScaling.value);

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.width = RTWidth;
            desc.height = RTHeight;
            desc.depthBufferBits = 0;

            TextureHandle rt1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT1", false);
            TextureHandle rt2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT2", false);

            Vector2 parameters = new Vector2(tiltShiftBlur.AreaSize.value, tiltShiftBlur.BlurRadius.value);
            int iteration = tiltShiftBlur.Iteration.value;

            // First pass - copy source to rt1
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + "_Setup", out var passData))
            {
                passData.src = source;
                passData.material = tiltShiftBlurMat;
                passData.passIndex = (int)tiltShiftBlur.QualityLevel.value;
                passData.parameters = parameters;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(rt1, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetVector(Params, data.parameters);
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                });
            }

            if (iteration > 1)
            {
                bool needSwitch = true;
                TextureHandle finalBlurID = rt1;

                for (int i = 0; i < iteration; i++)
                {
                    TextureHandle src = needSwitch ? rt1 : rt2;
                    TextureHandle dst = needSwitch ? rt2 : rt1;

                    using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + $"_Blur_{i}", out var passData))
                    {
                        passData.src = src;
                        passData.material = tiltShiftBlurMat;
                        passData.passIndex = (int)tiltShiftBlur.QualityLevel.value;
                        passData.parameters = parameters;

                        builder.UseTexture(src, AccessFlags.Read);
                        builder.SetRenderAttachment(dst, 0, AccessFlags.Write);

                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        {
                            data.material.SetVector(Params, data.parameters);
                            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                        });
                    }
                    finalBlurID = dst;
                    needSwitch = !needSwitch;
                }

                // Final pass
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + "_Final", out var passData))
                {
                    passData.src = finalBlurID;
                    passData.material = tiltShiftBlurMat;
                    passData.passIndex = 2;

                    builder.UseTexture(finalBlurID, AccessFlags.Read);
                    builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                    });
                }
            }
            else
            {
                // Single pass final
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + "_Final", out var passData))
                {
                    passData.src = rt1;
                    passData.material = tiltShiftBlurMat;
                    passData.passIndex = 2;

                    builder.UseTexture(rt1, AccessFlags.Read);
                    builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                    });
                }
            }
        }
        #endif

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
        }
    }

    private TiltShiftBlurPass _tiltShiftBlurPass;

    public override void Create()
    {
        _tiltShiftBlurPass = new TiltShiftBlurPass(renderPassEvent, shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_tiltShiftBlurPass);
    }

    #if !UNITY_2023_3_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _tiltShiftBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        base.SetupRenderPasses(renderer, renderingData);
    }
    #endif
}
