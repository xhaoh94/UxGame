using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class KawaseBlurRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;      // 设置Pass渲染的位置-初始值
    public Shader shader;
    public class KawaseBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurRadius = Shader.PropertyToID("_Offset");
        internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
        internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");

        private const string PROFILER_TAG = "KawaseBlur";

        private Material kawaseBlurMat;
        private KawaseBlur _kawaseBlur;
        #if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
        #endif

        public KawaseBlurPass(RenderPassEvent evet, Shader shader)
        {
            renderPassEvent = evet;            
            if (shader)
            {
                kawaseBlurMat = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        #if !UNITY_2023_3_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (kawaseBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _kawaseBlur = stack.GetComponent<KawaseBlur>();
            if (_kawaseBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_kawaseBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);


                int RTWidth = (int)(camera.pixelWidth / _kawaseBlur.RTDownScaling.value);
                int RTHeight = (int)(camera.pixelHeight / _kawaseBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                cmd.GetTemporaryRT(BufferRT2, RTWidth, RTHeight, 0, FilterMode.Bilinear);

                cmd.Blit(source, BufferRT1);

                bool needSwitch = true;
                for (int i = 0; i < _kawaseBlur.Iteration.value; i++)
                {
                    kawaseBlurMat.SetFloat(BlurRadius,
                        i / _kawaseBlur.RTDownScaling.value + _kawaseBlur.BlurRadius.value);
                    cmd.Blit(needSwitch ? BufferRT1 : BufferRT2, needSwitch ? BufferRT2 : BufferRT1, kawaseBlurMat, 0);
                    needSwitch = !needSwitch;
                }


                kawaseBlurMat.SetFloat(BlurRadius,
                    _kawaseBlur.Iteration.value / _kawaseBlur.RTDownScaling.value + _kawaseBlur.BlurRadius.value);
                cmd.Blit(needSwitch ? BufferRT1 : BufferRT2, source, kawaseBlurMat, 0);

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
#else
        private class PassData
        {
            public TextureHandle src;
            public Material material;
            public int passIndex;
            public float blurRadius;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (kawaseBlurMat == null) return;
            
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;
            
            var stack = VolumeManager.instance.stack;
            _kawaseBlur = stack.GetComponent<KawaseBlur>();
            if (_kawaseBlur == null || !_kawaseBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.width = (int)(cameraData.camera.pixelWidth / _kawaseBlur.RTDownScaling.value);
            desc.height = (int)(cameraData.camera.pixelHeight / _kawaseBlur.RTDownScaling.value);
            desc.depthBufferBits = 0;

            TextureHandle rt1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT1", false);
            TextureHandle rt2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT2", false);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + "_Copy", out var passData))
            {
                passData.src = source;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(rt1, 0, AccessFlags.Write);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }

            bool needSwitch = true;
            for (int i = 0; i < _kawaseBlur.Iteration.value; i++)
            {
                float radius = i / _kawaseBlur.RTDownScaling.value + _kawaseBlur.BlurRadius.value;
                TextureHandle src = needSwitch ? rt1 : rt2;
                TextureHandle dst = needSwitch ? rt2 : rt1;
                AddBlitPass(renderGraph, src, dst, kawaseBlurMat, 0, radius);
                needSwitch = !needSwitch;
            }

            float finalRadius = _kawaseBlur.Iteration.value / _kawaseBlur.RTDownScaling.value + _kawaseBlur.BlurRadius.value;
            TextureHandle finalSrc = needSwitch ? rt1 : rt2;
            
            AddBlitPass(renderGraph, finalSrc, source, kawaseBlurMat, 0, finalRadius);
        }

        private void AddBlitPass(RenderGraph renderGraph, TextureHandle src, TextureHandle dst, Material mat, int passIndex, float blurRadius)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG, out var passData))
            {
                passData.src = src;
                passData.material = mat;
                passData.passIndex = passIndex;
                passData.blurRadius = blurRadius;

                builder.UseTexture(src, AccessFlags.Read);
                builder.SetRenderAttachment(dst, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetFloat(BlurRadius, data.blurRadius);
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

    private KawaseBlurPass _kawaseBlurPass;

    public override void Create()
    {
        _kawaseBlurPass = new KawaseBlurPass(renderPassEvent, shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        
        renderer.EnqueuePass(_kawaseBlurPass);
    }
    #if !UNITY_2023_3_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _kawaseBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        base.SetupRenderPasses(renderer, renderingData);
    }
    #endif
}

