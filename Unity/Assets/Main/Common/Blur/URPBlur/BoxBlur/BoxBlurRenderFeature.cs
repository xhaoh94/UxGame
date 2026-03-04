using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class BoxBlurRenderFeature : ScriptableRendererFeature
{
    private BoxBlurPass _boxBlurPass;
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;      // 设置Pass渲染的位置-初始值
    public Shader shader;
    public class BoxBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurRadius = Shader.PropertyToID("_BlurOffset");
        internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
        internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");
        internal static readonly int TempTargetId = Shader.PropertyToID("_TempTargetBoxBlur");

        private const string PROFILER_TAG = "BoxBlur";

        private BoxBlur _boxBlur;
        private Material _boxBlurMat;
#if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
#endif

        public BoxBlurPass(RenderPassEvent evt, Shader shader)
        {
            renderPassEvent = evt; 
            _boxBlurMat = CoreUtils.CreateEngineMaterial(shader);
        }

        #if !UNITY_2023_3_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_boxBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _boxBlur = stack.GetComponent<BoxBlur>();
            if (_boxBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            var source = currentTarget;
            int destination = TempTargetId;

            if (_boxBlur.IsActive())
            {
                cmd.BeginSample(PROFILER_TAG);

                int RTWidth = (int)(camera.pixelWidth / _boxBlur.RTDownScaling.value);
                int RTHeight = (int)(camera.pixelHeight / _boxBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                cmd.GetTemporaryRT(destination, RTWidth, RTHeight, 0, FilterMode.Bilinear);

                cmd.Blit(source, BufferRT1);

                for (int i = 0; i < _boxBlur.Iteration.value; i++)
                {
                    if (_boxBlur.Iteration.value > 20)
                    {
                        return;
                    }

                    Vector4 BlurRadiusValue = new Vector4(_boxBlur.BlurRadius.value / camera.pixelWidth,
                        _boxBlur.BlurRadius.value / camera.pixelHeight, 0, 0);

                    _boxBlurMat.SetVector(BlurRadius, BlurRadiusValue);
                    cmd.Blit(BufferRT1, destination, _boxBlurMat, 0);

                    _boxBlurMat.SetVector(BlurRadius, BlurRadiusValue);
                    cmd.Blit(destination, BufferRT1, _boxBlurMat, 0);
                }

                cmd.Blit(BufferRT1, destination, _boxBlurMat, 1);
                cmd.Blit(destination, source);

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
            public TextureHandle dst;
            public Material material;
            public int passIndex;
            public Vector4 blurRadius;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_boxBlurMat == null) return;
            
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;
            
            var stack = VolumeManager.instance.stack;
            _boxBlur = stack.GetComponent<BoxBlur>();
            if (_boxBlur == null || !_boxBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.width = (int)(cameraData.camera.pixelWidth / _boxBlur.RTDownScaling.value);
            desc.height = (int)(cameraData.camera.pixelHeight / _boxBlur.RTDownScaling.value);
            desc.depthBufferBits = 0;

            TextureHandle rt1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BufferRT1", false);
            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_TempTargetBoxBlur", false);

            AddBlitPass(renderGraph, source, rt1, null, 0, Vector4.zero);

            int iterations = Mathf.Min(_boxBlur.Iteration.value, 20);
            Vector4 blurRadiusValue = new Vector4(_boxBlur.BlurRadius.value / cameraData.camera.pixelWidth,
                _boxBlur.BlurRadius.value / cameraData.camera.pixelHeight, 0, 0);

            for (int i = 0; i < iterations; i++)
            {
                AddBlitPass(renderGraph, rt1, destination, _boxBlurMat, 0, blurRadiusValue);
                AddBlitPass(renderGraph, destination, rt1, _boxBlurMat, 0, blurRadiusValue);
            }

            AddBlitPass(renderGraph, rt1, destination, _boxBlurMat, 1, Vector4.zero);
            
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG + "_BlitBack", out var passData))
            {
                passData.src = destination;
                passData.dst = source;

                builder.UseTexture(destination, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }
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
                    if (data.material != null)
                    {
                        if (data.blurRadius != Vector4.zero)
                        {
                            data.material.SetVector(BlurRadius, data.blurRadius);
                        }
                        Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                    }
                    else
                    {
                        Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0.0f, false);
                    }
                });
            }
        }
#endif
    }


    public override void Create()
    {
        _boxBlurPass = new BoxBlurPass(renderPassEvent, shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_boxBlurPass);
    }
    #if !UNITY_2023_3_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _boxBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        base.SetupRenderPasses(renderer, renderingData);
    }
#endif
}

