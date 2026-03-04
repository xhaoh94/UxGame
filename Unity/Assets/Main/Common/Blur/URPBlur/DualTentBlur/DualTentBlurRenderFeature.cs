using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class DualTentBlurRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;      // 设置Pass渲染的位置-初始值
    public Shader shader;
    public class DualTentBlurPass : ScriptableRenderPass
    {
        private const string PROFILER_TAG = "DualTentBlur";
        private DualTentBlur _dualTentBlur;

        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 16;

        internal static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");
        private Material dualKawaseBlurMat;

#if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
#endif

        public DualTentBlurPass(RenderPassEvent evt, Shader shader)
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
            _dualTentBlur = stack.GetComponent<DualTentBlur>();
            if (_dualTentBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_dualTentBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);

                int tw = (int)(camera.pixelWidth / _dualTentBlur.RTDownScaling.value);
                int th = (int)(camera.pixelHeight / _dualTentBlur.RTDownScaling.value);

                Vector4 BlurOffsetValue = new Vector4(_dualTentBlur.BlurRadius.value / (float)camera.pixelWidth,
                    _dualTentBlur.BlurRadius.value / (float)camera.pixelHeight, 0, 0);
                dualKawaseBlurMat.SetVector(BlurOffset, BlurOffsetValue);
                // Downsample
                RenderTargetIdentifier lastDown = source;
                for (int i = 0; i < _dualTentBlur.Iteration.value; i++)
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
                int lastUp = m_Pyramid[_dualTentBlur.Iteration.value - 1].down;
                for (int i = _dualTentBlur.Iteration.value - 2; i >= 0; i--)
                {
                    int mipUp = m_Pyramid[i].up;
                    cmd.Blit(lastUp, mipUp, dualKawaseBlurMat, 0);
                    lastUp = mipUp;
                }


                // Render blurred texture in blend pass
                cmd.Blit(lastUp, source, dualKawaseBlurMat, 1);

                // Cleanup
                for (int i = 0; i < _dualTentBlur.Iteration.value; i++)
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
            public Vector4 blurOffset;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (dualKawaseBlurMat == null) return;
            
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;
            
            var stack = VolumeManager.instance.stack;
            _dualTentBlur = stack.GetComponent<DualTentBlur>();
            if (_dualTentBlur == null || !_dualTentBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            int tw = (int)(cameraData.camera.pixelWidth / _dualTentBlur.RTDownScaling.value);
            int th = (int)(cameraData.camera.pixelHeight / _dualTentBlur.RTDownScaling.value);

            TextureHandle[] mipDowns = new TextureHandle[_dualTentBlur.Iteration.value];
            TextureHandle[] mipUps = new TextureHandle[_dualTentBlur.Iteration.value];

            for (int i = 0; i < _dualTentBlur.Iteration.value; i++)
            {
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.width = tw;
                desc.height = th;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;

                mipDowns[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, $"DualTentBlur_MipDown_{i}", false);
                mipUps[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, $"DualTentBlur_MipUp_{i}", false);

                tw = Mathf.Max(tw / 2, 1);
                th = Mathf.Max(th / 2, 1);
            }

            Vector4 blurOffsetValue = new Vector4(_dualTentBlur.BlurRadius.value / (float)cameraData.camera.pixelWidth,
                _dualTentBlur.BlurRadius.value / (float)cameraData.camera.pixelHeight, 0, 0);

            // Downsample
            TextureHandle lastDown = source;
            for (int i = 0; i < _dualTentBlur.Iteration.value; i++)
            {
                TextureHandle mipDown = mipDowns[i];
                AddBlitPass(renderGraph, lastDown, mipDown, dualKawaseBlurMat, 0, blurOffsetValue);
                lastDown = mipDown;
            }

            // Upsample
            TextureHandle lastUp = mipDowns[_dualTentBlur.Iteration.value - 1];
            for (int i = _dualTentBlur.Iteration.value - 2; i >= 0; i--)
            {
                TextureHandle mipUp = mipUps[i];
                AddBlitPass(renderGraph, lastUp, mipUp, dualKawaseBlurMat, 0, blurOffsetValue);
                lastUp = mipUp;
            }

            // Render blurred texture in blend pass
            AddBlitPass(renderGraph, lastUp, source, dualKawaseBlurMat, 1, blurOffsetValue);
        }

        private void AddBlitPass(RenderGraph renderGraph, TextureHandle src, TextureHandle dst, Material mat, int passIndex, Vector4 blurOffset)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG, out var passData))
            {
                passData.src = src;
                passData.material = mat;
                passData.passIndex = passIndex;
                passData.blurOffset = blurOffset;

                builder.UseTexture(src, AccessFlags.Read);
                builder.SetRenderAttachment(dst, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetVector(BlurOffset, data.blurOffset);
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

    private DualTentBlurPass _dualTentBlurPass;

    public override void Create()
    {
        _dualTentBlurPass = new DualTentBlurPass(renderPassEvent, shader);
    }

public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
{
renderer.EnqueuePass(_dualTentBlurPass);
    }
#if !UNITY_2023_3_OR_NEWER
public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
{
_dualTentBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
base.SetupRenderPasses(renderer, renderingData);
    }
#endif
}

