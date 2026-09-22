namespace Ux
{
    /// <summary>
    /// 阶段 Timeline 插件：把时间轴配置翻译成本帧的逻辑事实，写进 BattleWorld.FrameEvents。
    ///
    /// 求值命中窗口与取消窗口，只报告"哪些窗口本帧成立"——不判几何、不判去重、不碰表现层。
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
                AppendHitWindows(actionRunner, set);
                AppendCancelWindows(actionRunner, set);

                if (Verbose && set.HasHitWindows && !set.HasCancelWindows)
                {
                    Log.Debug($"[Timeline] frame={frame} {entity.Id} action={set.ActionId} " +
                              $"actionFrame={set.ActionFrame} 命中窗口={set.HitWindows.Count} 取消窗口=0");
                }
            }
        }

        /// <summary>命中窗口求值：只管有没有，不管打没打到。</summary>
        private static void AppendHitWindows(CombatActionRunner actionRunner, CombatFrameEventSet set)
        {
            actionRunner.AppendActiveHitWindows(set.HitWindows);
        }

        /// <summary>取消窗口求值：判定条件与 CombatActionRunner.TryCancel 一致（ActionCancelWindow.IsOpen）。</summary>
        private static void AppendCancelWindows(CombatActionRunner actionRunner, CombatFrameEventSet set)
        {
            var asset = actionRunner.CurrentAsset;
            if (asset?.CancelWindows == null)
            {
                return;
            }

            for (var i = 0; i < asset.CancelWindows.Count; i++)
            {
                var window = asset.CancelWindows[i];
                if (window == null || !window.IsOpen(set.ActionFrame, set.HasHitConfirmed))
                {
                    continue;
                }

                set.CancelWindows.Add(new CombatActiveCancelWindow(
                    set.ActionInstanceId,
                    set.ActionId,
                    set.ActionFrame,
                    i,
                    window));
            }
        }
    }
}
