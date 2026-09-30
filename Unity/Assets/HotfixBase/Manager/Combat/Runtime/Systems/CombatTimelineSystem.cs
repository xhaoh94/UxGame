using System.Collections.Generic;

namespace Ux
{
    /// <summary>
    /// 阶段 Timeline 插件：把时间轴配置翻译成本帧的逻辑事实，写进 BattleWorld.FrameEvents。
    ///
    /// 求值命中/取消窗口与离散帧事件，只报告本帧成立的逻辑事实——不判几何、不判去重、不碰表现层。
    /// 纯逻辑侧实现，不引用 Unity 对象，战报重放也能跑。
    /// </summary>
    public sealed class CombatTimelineSystem : ICombatSystem
    {
        /// <summary>输出本帧求值出的帧事件数量。</summary>
        public static bool Verbose;

        public BattlePhase Phase => BattlePhase.Timeline;

        public int Order => 0;

        public void Tick(BattleWorld world, long frame)
        {
            var table = world.FrameEvents;

            // 只遍历 Actions 阶段收集好的出招单位，不再扫全场。
            // 顺序 = 收集顺序（Id 升序），决定帧事件表与伤害结算的先后。
            var entities = world.ActionActiveEntities;
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.Id <= 0)
                {
                    // Id <= 0 会让命中解析器抛异常。IsCombatActive 已由 Actions 阶段筛过。
                    continue;
                }

                var actionRunner = entity.Controller.ActionRunner;
                if (!actionRunner.HasAction)
                {
                    // 防御：Actions 阶段刚筛过，但 Timeline 阶段的插件理论上可以改动作状态。
                    continue;
                }

                var set = table.Append(entity.Id, actionRunner.Current, actionRunner.Current.HasHitConfirmed);
                AppendHitboxWindows(actionRunner, set);
                AppendActionWindows(actionRunner, set);
                AppendFrameEvents(actionRunner, set);

                if (Verbose && set.HasHitboxWindows && !set.HasActionWindows)
                {
                    Log.Debug($"[Timeline] frame={frame} {entity.Id} action={set.ActionId} " +
                              $"actionFrame={set.ActionFrame} 攻击判定={set.HitboxWindows.Count} 动作窗口=0");
                }
            }
        }

        /// <summary>攻击判定求值：只管有没有，不管打没打到。</summary>
        private static void AppendHitboxWindows(CombatActionRunner actionRunner, CombatFrameEventSet set)
        {
            actionRunner.AppendActiveHitboxWindows(set.HitboxWindows);
        }

        /// <summary>连招衔接与泛化取消求值：两者都写入本帧开放的动作窗口事实。</summary>
        private static void AppendActionWindows(CombatActionRunner actionRunner, CombatFrameEventSet set)
        {
            var asset = actionRunner.CurrentAsset;
            if (asset == null)
            {
                return;
            }

            AppendTargetWindows(asset.CancelWindows, set);
            AppendTargetWindows(asset.LinkWindows, set);
        }

        private static void AppendTargetWindows<TWindow>(
            IReadOnlyList<TWindow> windows,
            CombatFrameEventSet set)
            where TWindow : ActionTargetWindow
        {
            if (windows == null)
            {
                return;
            }

            for (var i = 0; i < windows.Count; i++)
            {
                var window = windows[i];
                if (window == null || !window.IsOpen(set.ActionFrame, set.HasHitConfirmed))
                {
                    continue;
                }

                set.ActionWindows.Add(new CombatActiveActionWindow(
                    set.ActionInstanceId,
                    set.ActionId,
                    set.ActionFrame,
                    i,
                    window));
            }
        }

        /// <summary>
        /// 离散帧事件求值：事件只在自己的 Frame 上成立一次，不使用区间窗口语义。
        /// 这里按资产顺序写入帧事件缓冲，具体消费由对应系统负责。
        /// </summary>
        private static void AppendFrameEvents(CombatActionRunner actionRunner, CombatFrameEventSet set)
        {
            var asset = actionRunner.CurrentAsset;
            if (asset?.FrameEvents == null)
            {
                return;
            }

            for (var i = 0; i < asset.FrameEvents.Count; i++)
            {
                var frameEvent = asset.FrameEvents[i];
                if (!(frameEvent is ActionSpawnEvent spawnEvent) ||
                    spawnEvent.Frame != set.ActionFrame)
                {
                    continue;
                }

                set.SpawnEvents.Add(new CombatActiveSpawnEvent(
                    set.ActionInstanceId,
                    set.ActionId,
                    set.ActionFrame,
                    i,
                    spawnEvent));
            }
        }
    }
}
