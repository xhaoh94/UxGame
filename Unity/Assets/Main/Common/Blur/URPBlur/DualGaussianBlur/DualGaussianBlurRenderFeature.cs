using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class DualGaussianBlurRenderFeature : ScriptableRendererFeature
{
    private DualGaussianBlurPass _dualGaussianBlurPass;
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;      // 设置Pass渲染的位置-初始值
    public Shader shader;
    public class DualGaussianBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");

        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 16;

        private const string PROFILER_TAG = "DualGaussianBlur";

        private DualGaussianBlur _dualGaussianBlur;
        private Material dualGaussianBlurMat;
        #if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
#endif

        public DualGaussianBlurPass(RenderPassEvent evt, Shader shader)
        {
            renderPassEvent = evt;

            dualGaussianBlurMat = CoreUtils.CreateEngineMaterial(shader);

            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                m_Pyramid[i] = new Level
                {
                    down_vertical = Shader.PropertyToID("_BlurMipDownV" + i),
                    down_horizontal = Shader.PropertyToID("_BlurMipDownH" + i),
                    up_vertical = Shader.PropertyToID("_BlurMipUpV" + i),
                    up_horizontal = Shader.PropertyToID("_BlurMipUpH" + i),

                };
            }
        }

#if !UNITY_2023_3_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (dualGaussianBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _dualGaussianBlur = stack.GetComponent<DualGaussianBlur>();
            if (_dualGaussianBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_dualGaussianBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);


                int tw = (int)(camera.pixelWidth / _dualGaussianBlur.RTDownScaling.value);
                int th = (int)(camera.pixelHeight / _dualGaussianBlur.RTDownScaling.value);

                Vector4 BlurOffsetValue = new Vector4(_dualGaussianBlur.BlurRadius.value / (float)camera.pixelWidth,
                    _dualGaussianBlur.BlurRadius.value / (float)camera.pixelHeight, 0, 0);
                dualGaussianBlurMat.SetVector(BlurOffset, BlurOffsetValue);
                // Downsample
                RenderTargetIdentifier lastDown = source;
                for (int i = 0; i < _dualGaussianBlur.Iteration.value; i++)
                {
                    int mipDownV = m_Pyramid[i].down_vertical;
                    int mipDowH = m_Pyramid[i].down_horizontal;
                    int mipUpV = m_Pyramid[i].up_vertical;
                    int mipUpH = m_Pyramid[i].up_horizontal;

                    cmd.GetTemporaryRT(mipDownV, tw, th, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(mipDowH, tw, th, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(mipUpV, tw, th, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(mipUpH, tw, th, 0, FilterMode.Bilinear);

                    // horizontal blur
                    dualGaussianBlurMat.SetVector(BlurOffset,
                        new Vector4(_dualGaussianBlur.BlurRadius.value / camera.pixelWidth, 0, 0, 0));
                    cmd.Blit(lastDown, mipDowH, dualGaussianBlurMat, 0);

                    // vertical blur
                    dualGaussianBlurMat.SetVector(BlurOffset,
                        new Vector4(0, _dualGaussianBlur.BlurRadius.value / camera.pixelHeight, 0, 0));
                    cmd.Blit(mipDowH, mipDownV, dualGaussianBlurMat, 0);

                    lastDown = mipDownV;
                    tw = Mathf.Max(tw / 2, 1);
                    th = Mathf.Max(th / 2, 1);
                }

                // Upsample
                int lastUp = m_Pyramid[_dualGaussianBlur.Iteration.value - 1].down_vertical;
                for (int i = _dualGaussianBlur.Iteration.value - 2; i >= 0; i--)
                {

                    int mipUpV = m_Pyramid[i].up_vertical;
                    int mipUpH = m_Pyramid[i].up_horizontal;

                    // horizontal blur
                    dualGaussianBlurMat.SetVector(BlurOffset,
                        new Vector4(_dualGaussianBlur.BlurRadius.value / camera.pixelWidth, 0, 0, 0));
                    cmd.Blit(lastUp, mipUpH, dualGaussianBlurMat, 0);

                    // vertical blur
                    dualGaussianBlurMat.SetVector(BlurOffset,
                        new Vector4(0, _dualGaussianBlur.BlurRadius.value / camera.pixelHeight, 0, 0));
                    cmd.Blit(mipUpH, mipUpV, dualGaussianBlurMat, 0);

                    lastUp = mipUpV;
                }


                // Render blurred texture in blend pass
                cmd.Blit(lastUp, source, dualGaussianBlurMat, 1);

                // Cleanup
                for (int i = 0; i < _dualGaussianBlur.Iteration.value; i++)
                {
                    if (m_Pyramid[i].down_vertical != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].down_vertical);
                    if (m_Pyramid[i].down_horizontal != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].down_horizontal);
                    if (m_Pyramid[i].up_horizontal != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].up_horizontal);
                    if (m_Pyramid[i].up_vertical != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].up_vertical);
                }

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
            if (dualGaussianBlurMat == null) return;
            
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;
            
            var stack = VolumeManager.instance.stack;
            _dualGaussianBlur = stack.GetComponent<DualGaussianBlur>();
            if (_dualGaussianBlur == null || !_dualGaussianBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            int tw = (int)(cameraData.camera.pixelWidth / _dualGaussianBlur.RTDownScaling.value);
            int th = (int)(cameraData.camera.pixelHeight / _dualGaussianBlur.RTDownScaling.value);

            TextureHandle[] mipDownVs = new TextureHandle[_dualGaussianBlur.Iteration.value];
            TextureHandle[] mipDownHs = new TextureHandle[_dualGaussianBlur.Iteration.value];
            TextureHandle[] mipUpVs = new TextureHandle[_dualGaussianBlur.Iteration.value];
            TextureHandle[] mipUpHs = new TextureHandle[_dualGaussianBlur.Iteration.value];

            TextureHandle lastDown = source;

            // Downsample
            for (int i = 0; i < _dualGaussianBlur.Iteration.value; i++)
            {
                desc.width = tw;
                desc.height = th;
                
                mipDownVs[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BlurMipDownV" + i, false);
                mipDownHs[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BlurMipDownH" + i, false);
                mipUpVs[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BlurMipUpV" + i, false);
                mipUpHs[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BlurMipUpH" + i, false);
                
                // horizontal blur
                Vector4 blurOffsetH = new Vector4(_dualGaussianBlur.BlurRadius.value / cameraData.camera.pixelWidth, 0, 0, 0);
                AddBlitPass(renderGraph, lastDown, mipDownHs[i], dualGaussianBlurMat, 0, blurOffsetH);

                // vertical blur
                Vector4 blurOffsetV = new Vector4(0, _dualGaussianBlur.BlurRadius.value / cameraData.camera.pixelHeight, 0, 0);
                AddBlitPass(renderGraph, mipDownHs[i], mipDownVs[i], dualGaussianBlurMat, 0, blurOffsetV);

                lastDown = mipDownVs[i];
                tw = Mathf.Max(tw / 2, 1);
                th = Mathf.Max(th / 2, 1);
            }

            // Upsample
            TextureHandle lastUp = mipDownVs[_dualGaussianBlur.Iteration.value - 1];
            for (int i = _dualGaussianBlur.Iteration.value - 2; i >= 0; i--)
            {
                // horizontal blur
                Vector4 blurOffsetH = new Vector4(_dualGaussianBlur.BlurRadius.value / cameraData.camera.pixelWidth, 0, 0, 0);
                AddBlitPass(renderGraph, lastUp, mipUpHs[i], dualGaussianBlurMat, 0, blurOffsetH);

                // vertical blur
                Vector4 blurOffsetV = new Vector4(0, _dualGaussianBlur.BlurRadius.value / cameraData.camera.pixelHeight, 0, 0);
                AddBlitPass(renderGraph, mipUpHs[i], mipUpVs[i], dualGaussianBlurMat, 0, blurOffsetV);

                lastUp = mipUpVs[i];
            }

            // Render blurred texture in blend pass
            AddBlitPass(renderGraph, lastUp, source, dualGaussianBlurMat, 1, Vector4.zero);
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
                    if (data.blurRadius != Vector4.zero)
                    {
                        data.material.SetVector(BlurOffset, data.blurRadius);
                    }
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
                });
            }
        }
#endif

        struct Level
        {
            internal int down_vertical;
            internal int down_horizontal;
            internal int up_horizontal;
            internal int up_vertical;
        }
    }

    public override void Create()
    {
        _dualGaussianBlurPass = new DualGaussianBlurPass(renderPassEvent, shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_dualGaussianBlurPass);
    }
#if !UNITY_2023_3_OR_NEWER
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _dualGaussianBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        base.SetupRenderPasses(renderer, renderingData);
    }
#endif
}

