using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class DirectionalBlurRenderFeature : ScriptableRendererFeature
{
    private DirectionalBlurPass _directionalBlurPass;
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;      // 设置Pass渲染的位置-初始值
    public Shader shader;
    public class DirectionalBlurPass : ScriptableRenderPass
    {
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int BufferRT = Shader.PropertyToID("_BufferRT");

        private const string PROFILER_TAG = "DirectionalBlur";

        private DirectionalBlur _directionalBlur;
        private Material directionalBlurMat;

        #if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
#endif

        public DirectionalBlurPass(RenderPassEvent evt, Shader shader)
        {
            renderPassEvent = evt;
            directionalBlurMat = CoreUtils.CreateEngineMaterial(shader);
        }

        #if !UNITY_2023_3_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (directionalBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _directionalBlur = stack.GetComponent<DirectionalBlur>();
            if (_directionalBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_directionalBlur.IsActive())
            {
                var source = currentTarget;
                int RTWidth = (int)(renderingData.cameraData.camera.pixelWidth / _directionalBlur.RTDownScaling.value);
                int RTHeight = (int)(renderingData.cameraData.camera.pixelHeight / _directionalBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(BufferRT, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                cmd.Blit(source, BufferRT);

                float sinVal = (Mathf.Sin(_directionalBlur.Angle.value) * _directionalBlur.BlurRadius.value * 0.05f) / _directionalBlur.Iteration.value;
                float cosVal = (Mathf.Cos(_directionalBlur.Angle.value) * _directionalBlur.BlurRadius.value * 0.05f) / _directionalBlur.Iteration.value;
                directionalBlurMat.SetVector(Params, new Vector3(_directionalBlur.Iteration.value, sinVal, cosVal));
                cmd.Blit(BufferRT, source, directionalBlurMat, 0);
                cmd.ReleaseTemporaryRT(BufferRT);
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
            public TextureHandle dst;
            public Material material;
            public Vector3 paramsVec;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (directionalBlurMat == null) return;
            
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;
            
            var stack = VolumeManager.instance.stack;
            _directionalBlur = stack.GetComponent<DirectionalBlur>();
            if (_directionalBlur == null || !_directionalBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.width = (int)(cameraData.camera.pixelWidth / _directionalBlur.RTDownScaling.value);
            desc.height = (int)(cameraData.camera.pixelHeight / _directionalBlur.RTDownScaling.value);
            desc.depthBufferBits = 0;

            TextureHandle bufferRT = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT", false, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + "_Copy", out var passData))
            {
                passData.src = source;
                passData.dst = bufferRT;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(bufferRT, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }

            float sinVal = (Mathf.Sin(_directionalBlur.Angle.value) * _directionalBlur.BlurRadius.value * 0.05f) / _directionalBlur.Iteration.value;
            float cosVal = (Mathf.Cos(_directionalBlur.Angle.value) * _directionalBlur.BlurRadius.value * 0.05f) / _directionalBlur.Iteration.value;
            Vector3 paramsVec = new Vector3(_directionalBlur.Iteration.value, sinVal, cosVal);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG, out var passData))
            {
                passData.src = bufferRT;
                passData.dst = source;
                passData.material = directionalBlurMat;
                passData.paramsVec = paramsVec;

                builder.UseTexture(bufferRT, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetVector(Params, data.paramsVec);
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

    public override void Create()
    {
        _directionalBlurPass = new DirectionalBlurPass(renderPassEvent, shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_directionalBlurPass);
    }
    #if !UNITY_2023_3_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _directionalBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        base.SetupRenderPasses(renderer, renderingData);
    }
#endif
}

