using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 逻辑投射物到 Unity 视觉对象的桥接。
    ///
    /// BattleWorld 只负责确定性实体和位置；本类订阅实体注册／注销事件创建和回收视觉，
    /// 再在 Presentation 阶段把视觉对象同步到逻辑位置。没有配置 prefab 时使用占位方块，
    /// 这样先能验证"生成 → 飞行 → 回收"链路，正式美术资源稍后只需填入表现映射。
    /// </summary>
    public sealed class CombatProjectilePresentation : ICombatSystem
    {
        private static readonly List<WeakReference> ActiveBridges = new();

        /// <summary>占位方块共用的 URP 材质，避免每个投射物造一份材质实例。</summary>
        private static Material _placeholderMaterial;

        private readonly BattleWorld world;
        private readonly Dictionary<long, ProjectileView> views = new();
        private readonly List<long> staleIds = new();

        private CombatProjectilePresentation(BattleWorld world)
        {
            this.world = world;
        }

        public BattlePhase Phase => BattlePhase.Presentation;

        /// <summary>先于 BattleWorld 的内置 Presentation 核心同步，保证逻辑帧结束即能看到最新位置。</summary>
        public int Order => 0;

        /// <summary>
        /// 为一个战斗世界安装唯一的表现桥。弱引用表不会把已销毁的世界留在静态根上；
        /// 世界自身通过系统列表和事件订阅持有桥，生命周期仍与世界一致。
        /// </summary>
        public static void Ensure(BattleWorld world)
        {
            if (world == null)
            {
                return;
            }

            for (var i = ActiveBridges.Count - 1; i >= 0; i--)
            {
                var bridge = ActiveBridges[i].Target as CombatProjectilePresentation;
                if (bridge == null)
                {
                    ActiveBridges.RemoveAt(i);
                    continue;
                }

                if (ReferenceEquals(bridge.world, world))
                {
                    return;
                }
            }

            var presentation = new CombatProjectilePresentation(world);
            ActiveBridges.Add(new WeakReference(presentation));
            world.EntityRegistered += presentation.OnEntityRegistered;
            world.EntityUnregistered += presentation.OnEntityUnregistered;
            world.AddSystem(presentation);

            // 正常路径是在角色 Register 之前安装，因此后续实体都会走事件；
            // 这里补一次已有实体，兼容世界创建后才加载模型／表现组件的场景。
            foreach (var entity in world.Entities)
            {
                presentation.OnEntityRegistered(entity);
            }
        }

        public void Tick(BattleWorld world, long frame)
        {
            staleIds.Clear();
            foreach (var pair in views)
            {
                var view = pair.Value;
                if (!world.TryGetEntity(pair.Key, out var entity) ||
                    !ReferenceEquals(entity, view.Entity) ||
                    !(entity is ICombatProjectile))
                {
                    staleIds.Add(pair.Key);
                    continue;
                }

                Sync(view, entity);
            }

            for (var i = 0; i < staleIds.Count; i++)
            {
                RemoveView(staleIds[i]);
            }
        }

        private void OnEntityRegistered(ICombatEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            // Register 同 ID 替换实体时没有单独的旧实体注销事件，这里先回收旧视觉。
            RemoveView(entity.Id);

            if (!(entity is ICombatProjectile projectile))
            {
                return;
            }

            var view = CreateView(entity, projectile);
            if (view != null)
            {
                views[entity.Id] = view;
            }
        }

        private void OnEntityUnregistered(ICombatEntity entity)
        {
            if (entity != null)
            {
                RemoveView(entity.Id);
            }
        }

        private static ProjectileView CreateView(ICombatEntity entity, ICombatProjectile projectile)
        {
            try
            {
                var spawn = projectile.Spawn;
                var presentation = spawn?.FlightProfile?.GetSpawnPresentation(spawn);
                var gameObject = presentation?.Prefab != null
                    ? UnityEngine.Object.Instantiate(presentation.Prefab)
                    : CreatePlaceholder(spawn);

                if (gameObject == null)
                {
                    return null;
                }

                gameObject.name = $"__CombatProjectile_{entity.Id}";
                // 只禁构建，不要用 HideFlags.DontSave：DontSave 的对象不属于任何场景，
                // 退播放时不会被带走，会永久赖在编辑器场景里。
                gameObject.hideFlags |= HideFlags.DontSaveInBuild;
                gameObject.transform.position = entity.Position;
                gameObject.transform.rotation = entity.Rotation;
                return new ProjectileView(entity, gameObject);
            }
            catch (Exception exception)
            {
                Log.Error($"创建投射物视觉失败: entity={entity.Id}, {exception.Message}");
                return null;
            }
        }

        private static GameObject CreatePlaceholder(CombatSpawnProfile spawn)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.transform.localScale = new Vector3(0.18f, 0.18f, 0.8f);

            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                // 同 DestroyViewObject：编辑模式下 Destroy 不生效，占位方块的碰撞体不能留在场景里。
                DestroyViewObject(collider);
            }

            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                var color = spawn != null && (spawn.SpawnId & 1) == 0
                    ? new Color(0.2f, 0.8f, 1f, 1f)
                    : new Color(1f, 0.55f, 0.15f, 1f);

                // CreatePrimitive 给的是 Built-in 默认材质，URP 下渲染成洋红且没有颜色属性可写；
                // 换成 URP 材质 + 属性块，颜色才真的生效，也不给每个实例造一份材质。
                var material = PlaceholderMaterial();
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                    var block = new MaterialPropertyBlock();
                    block.SetColor("_BaseColor", color);
                    block.SetColor("_Color", color);
                    renderer.SetPropertyBlock(block);
                }
            }

            return gameObject;
        }

        private static Material PlaceholderMaterial()
        {
            if (_placeholderMaterial != null)
            {
                return _placeholderMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                return null;
            }

            _placeholderMaterial = new Material(shader) { name = "__CombatProjectilePlaceholder" };
            _placeholderMaterial.hideFlags = HideFlags.DontSaveInBuild;
            return _placeholderMaterial;
        }

        private static void Sync(ProjectileView view, ICombatEntity entity)
        {
            if (view.GameObject == null)
            {
                return;
            }

            view.GameObject.transform.position = entity.Position;
            view.GameObject.transform.rotation = entity.Rotation;
        }

        private void RemoveView(long entityId)
        {
            if (!views.TryGetValue(entityId, out var view))
            {
                return;
            }

            views.Remove(entityId);
            DestroyViewObject(view.GameObject);
        }

        /// <summary>
        /// 编辑模式下 Object.Destroy 是空操作（不会报错，只是什么都不做），残留物会永久留在场景里；
        /// 所以非播放态必须走 DestroyImmediate。这条路径在 Timeline 预览、回放校验里都会走到。
        /// </summary>
        private static void DestroyViewObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private sealed class ProjectileView
        {
            public ProjectileView(ICombatEntity entity, GameObject gameObject)
            {
                Entity = entity;
                GameObject = gameObject;
            }

            public ICombatEntity Entity { get; }
            public GameObject GameObject { get; }
        }
    }
}
