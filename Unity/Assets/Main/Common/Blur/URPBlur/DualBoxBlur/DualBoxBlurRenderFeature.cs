using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class DualBoxBlurRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;      // 设置Pass渲染的位置-初始值
    public Shader shader;
    public class DualBoxBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");

        private const string PROFILER_TAG = "DualBoxBlur";

        private DualBoxBlur _dualBoxBlur;
        private Material _dualBoxBlurMat;
#if !UNITY_2023_3_OR_NEWER
        RenderTargetIdentifier currentTarget;
#endif

        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 16;

        public DualBoxBlurPass(RenderPassEvent evt, Shader shader)
        {
            renderPassEvent = evt;
            //Shader dualBoxBlurShader = Shader.Find("Ux/URP_PostProcessing/DualBoxBlur");
            if (shader)
            {
                _dualBoxBlurMat = CoreUtils.CreateEngineMaterial(shader);
            }

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
            if (_dualBoxBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _dualBoxBlur = stack.GetComponent<DualBoxBlur>();
            if (_dualBoxBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_dualBoxBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);

                int tw = (int)(camera.pixelWidth / _dualBoxBlur.RTDownScaling.value);
                int th = (int)(camera.pixelHeight / _dualBoxBlur.RTDownScaling.value);

                Vector4 BlurOffsetValue = new Vector4(_dualBoxBlur.BlurRadius.value / camera.pixelWidth,
                    _dualBoxBlur.BlurRadius.value / camera.pixelHeight, 0, 0);
                _dualBoxBlurMat.SetVector(BlurOffset, BlurOffsetValue);
                // Downsample
                RenderTargetIdentifier lastDown = source;
                for (int i = 0; i < _dualBoxBlur.Iteration.value; i++)
                {
                    int mipDown = m_Pyramid[i].down;
                    int mipUp = m_Pyramid[i].up;
                    cmd.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear);
                    cmd.Blit(lastDown, mipDown, _dualBoxBlurMat, 0);

                    lastDown = mipDown;
                    tw = Mathf.Max(tw / 2, 1);
                    th = Mathf.Max(th / 2, 1);
                }

                // Upsample
                int lastUp = m_Pyramid[_dualBoxBlur.Iteration.value - 1].down;
                for (int i = _dualBoxBlur.Iteration.value - 2; i >= 0; i--)
                {
                    int mipUp = m_Pyramid[i].up;
                    cmd.Blit(lastUp, mipUp, _dualBoxBlurMat, 0);
                    lastUp = mipUp;
                }


                // Render blurred texture in blend pass
                cmd.Blit(lastUp, source, _dualBoxBlurMat, 1);

                // Cleanup
                for (int i = 0; i < _dualBoxBlur.Iteration.value; i++)
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
            if (_dualBoxBlurMat == null) return;
            
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.postProcessEnabled) return;
            
            var stack = VolumeManager.instance.stack;
            _dualBoxBlur = stack.GetComponent<DualBoxBlur>();
            if (_dualBoxBlur == null || !_dualBoxBlur.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            int tw = (int)(cameraData.camera.pixelWidth / _dualBoxBlur.RTDownScaling.value);
            int th = (int)(cameraData.camera.pixelHeight / _dualBoxBlur.RTDownScaling.value);

            Vector4 blurOffsetValue = new Vector4(_dualBoxBlur.BlurRadius.value / cameraData.camera.pixelWidth,
                _dualBoxBlur.BlurRadius.value / cameraData.camera.pixelHeight, 0, 0);

            TextureHandle[] mipDowns = new TextureHandle[_dualBoxBlur.Iteration.value];
            TextureHandle[] mipUps = new TextureHandle[_dualBoxBlur.Iteration.value];

            TextureHandle lastDown = source;

            // Downsample
            for (int i = 0; i < _dualBoxBlur.Iteration.value; i++)
            {
                desc.width = tw;
                desc.height = th;
                
                mipDowns[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BlurMipDown" + i, false);
                mipUps[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BlurMipUp" + i, false);
                
                AddBlitPass(renderGraph, lastDown, mipDowns[i], _dualBoxBlurMat, 0, blurOffsetValue);

                lastDown = mipDowns[i];
                tw = Mathf.Max(tw / 2, 1);
                th = Mathf.Max(th / 2, 1);
            }

            // Upsample
            TextureHandle lastUp = mipDowns[_dualBoxBlur.Iteration.value - 1];
            for (int i = _dualBoxBlur.Iteration.value - 2; i >= 0; i--)
            {
                AddBlitPass(renderGraph, lastUp, mipUps[i], _dualBoxBlurMat, 0, blurOffsetValue);
                lastUp = mipUps[i];
            }

            // Render blurred texture in blend pass
            AddBlitPass(renderGraph, lastUp, source, _dualBoxBlurMat, 1, blurOffsetValue);
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
            internal int down;
            internal int up;
        }
    }

    private DualBoxBlurPass _dualBoxBlurPass;

    public override void Create()
    {
        _dualBoxBlurPass = new DualBoxBlurPass(renderPassEvent, shader);
    }

public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
{
renderer.EnqueuePass(_dualBoxBlurPass);
    }
#if !UNITY_2023_3_OR_NEWER
public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
{
_dualBoxBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
base.SetupRenderPasses(renderer, renderingData);
    }
#endif
}

