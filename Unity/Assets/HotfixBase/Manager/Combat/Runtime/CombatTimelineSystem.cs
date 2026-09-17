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

            // 顺序 = OrderedEntities 的 Id 升序，决定帧事件表与伤害结算的先后。
            // 按索引遍历，不用 foreach world.Entities（接口枚举器会装箱，每帧一个堆对象）。
            var entities = world.OrderedEntities;
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!entity.IsCombatActive || entity.Id <= 0)
                {
                    // Id <= 0 会让命中解析器抛异常，未初始化的单位也没有可求值的动作。
                    continue;
                }

                var actions = entity.Controller.Actions;
                if (!actions.HasAction)
                {
                    // 没在出招就没有帧事件，这是常态。
                    continue;
                }

                var set = table.Append(entity.Id, actions.Current, actions.Current.HasHitConfirmed);
                AppendHitWindows(actions, set);
                AppendCancelWindows(actions, set);

                if (Verbose && set.HasHitWindows && !set.HasCancelWindows)
                {
                    Log.Debug($"[Timeline] frame={frame} {entity.Id} action={set.ActionId} " +
                              $"actionFrame={set.ActionFrame} 命中窗口={set.HitWindows.Count} 取消窗口=0");
                }
            }
        }

        /// <summary>命中窗口求值：只管有没有，不管打没打到。</summary>
        private static void AppendHitWindows(CombatActionRunner actions, CombatFrameEventSet set)
        {
            actions.AppendActiveHitWindows(set.HitWindows);
        }

        /// <summary>取消窗口求值：判定条件与 CombatActionRunner.TryCancel 一致（ActionCancelWindow.IsOpen）。</summary>
        private static void AppendCancelWindows(CombatActionRunner actions, CombatFrameEventSet set)
        {
            var asset = actions.CurrentAsset;
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
