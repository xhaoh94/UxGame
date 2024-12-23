using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        RenderTargetIdentifier currentTarget;

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
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _dualKawaseBlurPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);
        base.SetupRenderPasses(renderer, renderingData);
    }
}
