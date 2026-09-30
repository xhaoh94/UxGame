using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 占位特效宿主：模型上没有美术摆好的 ParticleSystem 时，临时建一个能被特效轨驱动的系统。
    ///
    /// ⚠ 只为验证"特效轨能不能播出来"。正式特效应由美术在模型上摆好 —— 那时会直接复用模型里的
    /// 系统，本类的生成逻辑自动让位。
    /// </summary>
    public static class CombatVfxHost
    {
        private const string HostName = "__CombatVfxHost";

        // 项目是 URP，内置管线的粒子 Shader 在这里渲染成洋红；后面几项只是兜底。
        private static readonly string[] ShaderCandidates =
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
            "Unlit/Color",
        };

        private static Material _sharedMaterial;
        private static bool _shaderMissing;

        /// <summary>优先复用父节点下已有的粒子系统，没有才建一个。返回 null 表示当前环境建不出来。</summary>
        public static ParticleSystem Ensure(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.GetComponentInChildren<ParticleSystem>(true);
            if (existing != null)
            {
                return existing;
            }

            var host = new GameObject(HostName);
            host.transform.SetParent(parent, false);
            // 摆在胸口偏前，免得整段特效被身体挡住。
            host.transform.localPosition = new Vector3(0f, 0.9f, 0.45f);

            var particle = host.AddComponent<ParticleSystem>();
            Configure(particle);
            return particle;
        }

        private static void Configure(ParticleSystem particle)
        {
            var main = particle.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.45f;
            main.startSpeed = 2.5f;
            main.startSize = 0.35f;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 64;

            var emission = particle.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });

            var shape = particle.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;

            var particleRenderer = particle.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                particleRenderer.sharedMaterial = SharedMaterial;
            }

            // TLParticleClip 用 Simulate 手动推进，系统必须保持"不自己播"，
            // 否则自动播放会和 Simulate 叠加，粒子位置比权威帧跑得快。
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static Material SharedMaterial
        {
            get
            {
                if (_sharedMaterial == null && !_shaderMissing)
                {
                    _sharedMaterial = CreateMaterial();
                }
                return _sharedMaterial;
            }
        }

        private static Material CreateMaterial()
        {
            foreach (var shaderName in ShaderCandidates)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    continue;
                }

                var material = new Material(shader) { name = HostName };
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", Color.white);
                }
                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", Color.white);
                }
                return material;
            }

            _shaderMissing = true;
            Log.Error($"占位特效找不到可用 Shader，特效轨不会显示：{string.Join(", ", ShaderCandidates)}");
            return null;
        }
    }
}
