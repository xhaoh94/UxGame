namespace Ux
{
    public static class CombatTimelineResolver
    {
        /// <summary>
        /// 把状态机与动作运行时的当前事实翻译成"这一帧两个播放层各播哪条时间线、播到第几帧"。
        /// 纯读、无副作用；切轨道与推播放头由 CombatTimelinePlayer 做。
        /// 基础层按 Life > Control > Locomotion 选，Life/Control 命中即独占（ExclusiveBase）。
        /// </summary>
        public static CombatTimelinePlan Resolve(CharacterCombatProfile profile, CombatStateMachine states, CombatActionRunner actions, string variantId = null)
        {
            if (profile == null || states == null)
            {
                return default;
            }

            var variant = CombatStatePresentation.NormalizeVariantId(variantId);
            var lifeId = states.GetCurrentStateId(StateLayer.Life);
            var life = profile.GetStatePresentation(StateLayer.Life, lifeId, variant);
            if (life?.Timeline != null)
            {
                return new CombatTimelinePlan(new CombatTimelineSelection(life.Timeline, states.GetStateFrame(StateLayer.Life)), default, true);
            }

            var controlId = states.GetCurrentStateId(StateLayer.Control);
            var control = profile.GetStatePresentation(StateLayer.Control, controlId, variant);
            if (control?.Timeline != null)
            {
                return new CombatTimelinePlan(new CombatTimelineSelection(control.Timeline, states.GetStateFrame(StateLayer.Control)), default, true);
            }

            var locomotionId = states.GetCurrentStateId(StateLayer.Locomotion);
            var locomotion = profile.GetStatePresentation(StateLayer.Locomotion, locomotionId, variant);
            var actionSelection = default(CombatTimelineSelection);
            if (actions?.HasAction == true)
            {
                var asset = profile.GetActionTimeline(actions.Current.ActionId);
                if (asset != null)
                {
                    actionSelection = new CombatTimelineSelection(asset, actions.Current.ActionFrame, actions.Current.InstanceId);
                }
            }
            var baseSelection = new CombatTimelineSelection(locomotion?.Timeline, states.GetStateFrame(StateLayer.Locomotion));
            return new CombatTimelinePlan(baseSelection, actionSelection);
        }
    }

}