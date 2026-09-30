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
                gameObject.hideFlags |= HideFlags.DontSave;
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
                UnityEngine.Object.Destroy(collider);
            }

            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = renderer.material;
                var color = spawn != null && (spawn.SpawnId & 1) == 0
                    ? new Color(0.2f, 0.8f, 1f, 1f)
                    : new Color(1f, 0.55f, 0.15f, 1f);
                if (material != null)
                {
                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", color);
                    }
                    if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", color);
                    }
                }
            }

            return gameObject;
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
            if (view.GameObject != null)
            {
                UnityEngine.Object.Destroy(view.GameObject);
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
