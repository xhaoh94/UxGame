using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class IrisBlurRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    public Shader shader;

    public class IrisBlurPass : ScriptableRenderPass
    {
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int BlurredTex = Shader.PropertyToID("_BlurredTex");
        internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
        internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");

        private const string PROFILER_TAG = "IrisBlur";
        private Material _irisBlurMat;
        private IrisBlur _irisBlur;

        #if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
        #endif

        public IrisBlurPass(RenderPassEvent evet, Shader shader)
        {
            renderPassEvent = evet;
            if (shader)
            {
                _irisBlurMat = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        #if !UNITY_2023_3_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_irisBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _irisBlur = stack.GetComponent<IrisBlur>();
            if (_irisBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_irisBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);

                if (_irisBlur.Iteration.value == 1)
                {
                    int RTWidth = (int)(camera.pixelWidth / _irisBlur.RTDownScaling.value);
                    int RTHeight = (int)(camera.pixelHeight / _irisBlur.RTDownScaling.value);
                    cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                    _irisBlurMat.SetVector(Params, new Vector4(_irisBlur.AreaSize.value, _irisBlur.BlurRadius.value));
                    cmd.Blit(source, BufferRT1, _irisBlurMat, (int)_irisBlur.QualityLevel.value);
                    cmd.SetGlobalTexture(BlurredTex, BufferRT1);
                    cmd.Blit(BufferRT1, source, _irisBlurMat, 2);
                    cmd.ReleaseTemporaryRT(BufferRT1);
                }
                else
                {
                    int RTWidth = (int)(camera.pixelWidth / _irisBlur.RTDownScaling.value);
                    int RTHeight = (int)(camera.pixelHeight / _irisBlur.RTDownScaling.value);
                    cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(BufferRT2, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                    _irisBlurMat.SetVector(Params, new Vector2(_irisBlur.AreaSize.value, _irisBlur.BlurRadius.value));
                    RenderTargetIdentifier finalBlurID = BufferRT1;
                    RenderTargetIdentifier firstID = source;
                    RenderTargetIdentifier secondID = BufferRT1;
                    for (int i = 0; i < _irisBlur.Iteration.value; i++)
                    {
                        cmd.Blit(firstID, secondID, _irisBlurMat, (int)_irisBlur.QualityLevel.value);
                        finalBlurID = secondID;
                        firstID = secondID;
                        secondID = (secondID == BufferRT1) ? BufferRT2 : BufferRT1;
                    }
                    cmd.SetGlobalTexture(BlurredTex, finalBlurID);
                    cmd.Blit(BlurredTex, source, _irisBlurMat, 2);
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
            public Material material;
            public int passIndex;
            public TextureHandle source;
            public Vector4 parameters;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_irisBlurMat == null) return;

            var universalCameraData = frameData.Get<UniversalCameraData>();
            if (!universalCameraData.postProcessEnabled) return;

            var stack = VolumeManager.instance.stack;
            _irisBlur = stack.GetComponent<IrisBlur>();
            if (_irisBlur == null || !_irisBlur.IsActive()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.activeColorTexture;
            var camera = universalCameraData.camera;

            int RTWidth = (int)(camera.pixelWidth / _irisBlur.RTDownScaling.value);
            int RTHeight = (int)(camera.pixelHeight / _irisBlur.RTDownScaling.value);

            RenderTextureDescriptor desc = universalCameraData.cameraTargetDescriptor;
            desc.width = RTWidth;
            desc.height = RTHeight;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle buffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT1", false);

            if (_irisBlur.Iteration.value == 1)
            {
                Vector4 parameters = new Vector4(_irisBlur.AreaSize.value, _irisBlur.BlurRadius.value);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("IrisBlur_Pass1", out var passData))
                {
                    passData.material = _irisBlurMat;
                    passData.passIndex = (int)_irisBlur.QualityLevel.value;
                    passData.source = source;
                    passData.parameters = parameters;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(buffer1, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        data.material.SetVector(Params, data.parameters);
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                    });
                }

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("IrisBlur_Final", out var passData))
                {
                    passData.material = _irisBlurMat;
                    passData.passIndex = 2;
                    passData.source = buffer1;

                    builder.UseTexture(buffer1, AccessFlags.Read);
                    builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalTexture(BlurredTex, data.source);
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                    });
                }
            }
            else
            {
                TextureHandle buffer2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT2", false);
                Vector2 parameters = new Vector2(_irisBlur.AreaSize.value, _irisBlur.BlurRadius.value);

                bool needSwitch = true;
                TextureHandle finalBlurID = buffer1;

                for (int i = 0; i < _irisBlur.Iteration.value; i++)
                {
                    TextureHandle src = needSwitch ? source : buffer1;
                    TextureHandle dst = needSwitch ? buffer1 : buffer2;

                    using (var builder = renderGraph.AddRasterRenderPass<PassData>($"IrisBlur_Iter_{i}", out var passData))
                    {
                        passData.material = _irisBlurMat;
                        passData.passIndex = (int)_irisBlur.QualityLevel.value;
                        passData.source = src;
                        passData.parameters = parameters;

                        builder.UseTexture(src, AccessFlags.Read);
                        builder.SetRenderAttachment(dst, 0, AccessFlags.Write);

                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        {
                            data.material.SetVector(Params, data.parameters);
                            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                        });
                    }
                    finalBlurID = dst;
                    needSwitch = !needSwitch;
                }

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("IrisBlur_Final", out var passData))
                {
                    passData.material = _irisBlurMat;
                    passData.passIndex = 2;
                    passData.source = finalBlurID;

                    builder.UseTexture(finalBlurID, AccessFlags.Read);
                    builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalTexture(BlurredTex, data.source);
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
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

    private IrisBlurPass _irisBlurPass;

    public override void Create()
    {
        _irisBlurPass = new IrisBlurPass(renderPassEvent, shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_irisBlurPass);
    }

    #if !UNITY_2023_3_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _irisBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        base.SetupRenderPasses(renderer, renderingData);
    }
    #endif
}
