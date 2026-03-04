using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class DualKawaseBlurRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;      // 设置Pass渲染的位置-初始值
    public Shader shader;
    public class DualKawaseBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurOffset = Shader.PropertyToID("_Offset");

        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 16;

        private const string PROFILER_TAG = "DualKawaseBlur";
        private DualKawaseBlur _dualKawaseBlur;
        private Material dualKawaseBlurMat;
#if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
#endif

        public DualKawaseBlurPass(RenderPassEvent evt, Shader shader)
        {
            renderPassEvent = evt;

            dualKawaseBlurMat = CoreUtils.CreateEngineMaterial(shader);

            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                m_Pyramid[i] = new Level
                {
                    down = Shader.PropertyToID("_BlurMipDown" + i),
                    up = Shader.PropertyToID("_BlurMipUp" + i)
                };
            }
        }
        
        #if !UNITY_2023_3_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (dualKawaseBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _dualKawaseBlur = stack.GetComponent<DualKawaseBlur>();
            if (_dualKawaseBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_dualKawaseBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);

                int tw = (int)(camera.pixelWidth / _dualKawaseBlur.RTDownScaling.value);
                int th = (int)(camera.pixelHeight / _dualKawaseBlur.RTDownScaling.value);

                dualKawaseBlurMat.SetFloat(BlurOffset, _dualKawaseBlur.BlurRadius.value);


                // Downsample
                RenderTargetIdentifier lastDown = source;
                for (int i = 0; i < _dualKawaseBlur.Iteration.value; i++)
                {
                    int mipDown = m_Pyramid[i].down;
                    int mipUp = m_Pyramid[i].up;
                    cmd.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear);
                    cmd.Blit(lastDown, mipDown, dualKawaseBlurMat, 0);

                    lastDown = mipDown;
                    tw = Mathf.Max(tw / 2, 1);
                    th = Mathf.Max(th / 2, 1);
                }

                // Upsample
                int lastUp = m_Pyramid[_dualKawaseBlur.Iteration.value - 1].down;
                for (int i = _dualKawaseBlur.Iteration.value - 2; i >= 0; i--)
                {
                    int mipUp = m_Pyramid[i].up;

                    cmd.Blit(lastUp, mipUp, dualKawaseBlurMat, 1);
                    lastUp = mipUp;
                }


                // Render blurred texture in blend pass
                cmd.Blit(lastUp, source, dualKawaseBlurMat, 1);

                // Cleanup
                for (int i = 0; i < _dualKawaseBlur.Iteration.value; i++)
                {
                    if (m_Pyramid[i].down != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].down);
                    if (m_Pyramid[i].up != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].up);
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
            public float blurRadius;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (dualKawaseBlurMat == null) return;
            
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;
            
            var stack = VolumeManager.instance.stack;
            _dualKawaseBlur = stack.GetComponent<DualKawaseBlur>();
            if (_dualKawaseBlur == null || !_dualKawaseBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            int tw = (int)(cameraData.camera.pixelWidth / _dualKawaseBlur.RTDownScaling.value);
            int th = (int)(cameraData.camera.pixelHeight / _dualKawaseBlur.RTDownScaling.value);

            TextureHandle[] mipDowns = new TextureHandle[_dualKawaseBlur.Iteration.value];
            TextureHandle[] mipUps = new TextureHandle[_dualKawaseBlur.Iteration.value];

            for (int i = 0; i < _dualKawaseBlur.Iteration.value; i++)
            {
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.width = tw;
                desc.height = th;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;

                mipDowns[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, $"DualKawaseBlur_MipDown_{i}", false);
                mipUps[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, $"DualKawaseBlur_MipUp_{i}", false);

                tw = Mathf.Max(tw / 2, 1);
                th = Mathf.Max(th / 2, 1);
            }

            float blurRadius = _dualKawaseBlur.BlurRadius.value;

            // Downsample
            TextureHandle lastDown = source;
            for (int i = 0; i < _dualKawaseBlur.Iteration.value; i++)
            {
                TextureHandle mipDown = mipDowns[i];
                AddBlitPass(renderGraph, lastDown, mipDown, dualKawaseBlurMat, 0, blurRadius);
                lastDown = mipDown;
            }

            // Upsample
            TextureHandle lastUp = mipDowns[_dualKawaseBlur.Iteration.value - 1];
            for (int i = _dualKawaseBlur.Iteration.value - 2; i >= 0; i--)
            {
                TextureHandle mipUp = mipUps[i];
                AddBlitPass(renderGraph, lastUp, mipUp, dualKawaseBlurMat, 1, blurRadius);
                lastUp = mipUp;
            }

            // Render blurred texture in blend pass
            AddBlitPass(renderGraph, lastUp, source, dualKawaseBlurMat, 1, blurRadius);
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
                    data.material.SetFloat(BlurOffset, data.blurRadius);
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                });
            }
        }
#endif

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
        }

        struct Level
        {
            internal int down;
            internal int up;
        }
    }

    private DualKawaseBlurPass _dualKawaseBlurPass;

    public override void Create()
    {
        _dualKawaseBlurPass = new DualKawaseBlurPass(renderPassEvent, shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_dualKawaseBlurPass);
    }
    #if !UNITY_2023_3_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _dualKawaseBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        base.SetupRenderPasses(renderer, renderingData);
    }
#endif
}

