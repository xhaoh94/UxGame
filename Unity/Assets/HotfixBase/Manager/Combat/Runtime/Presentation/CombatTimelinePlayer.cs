using System;
using UnityEngine;

namespace Ux
{
    public readonly struct CombatTimelineSelection
    {
        public readonly string OwnerKey;
        public readonly TimelineAsset Asset;
        public readonly int Frame;

        public CombatTimelineSelection(string ownerKey, TimelineAsset asset, int frame)
        {
            OwnerKey = ownerKey ?? string.Empty;
            Asset = asset;
            Frame = Math.Max(0, frame);
        }
    }

    public readonly struct CombatTimelinePlan
    {
        public readonly CombatTimelineSelection Base;
        public readonly CombatTimelineSelection Action;
        public readonly bool ExclusiveBase;

        public CombatTimelinePlan(CombatTimelineSelection baseSelection, CombatTimelineSelection actionSelection, bool exclusiveBase = false)
        {
            Base = baseSelection;
            Action = actionSelection;
            ExclusiveBase = exclusiveBase;
        }
    }

    public static class CombatTimelineResolver
    {
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
                return new CombatTimelinePlan(new CombatTimelineSelection($"state:{(int)StateLayer.Life}:{lifeId}:{life.StableId}:{life.VariantId}", life.Timeline, states.GetStateFrame(StateLayer.Life)), default, true);
            }

            var controlId = states.GetCurrentStateId(StateLayer.Control);
            var control = profile.GetStatePresentation(StateLayer.Control, controlId, variant);
            if (control?.Timeline != null)
            {
                return new CombatTimelinePlan(new CombatTimelineSelection($"state:{(int)StateLayer.Control}:{controlId}:{control.StableId}:{control.VariantId}", control.Timeline, states.GetStateFrame(StateLayer.Control)), default, true);
            }

            var locomotionId = states.GetCurrentStateId(StateLayer.Locomotion);
            var locomotion = profile.GetStatePresentation(StateLayer.Locomotion, locomotionId, variant);
            var actionSelection = default(CombatTimelineSelection);
            if (actions?.HasAction == true)
            {
                var asset = profile.GetActionTimeline(actions.Current.ActionId);
                if (asset != null)
                {
                    actionSelection = new CombatTimelineSelection($"action:{actions.Current.InstanceId}", asset, actions.Current.ActionFrame);
                }
            }
            var baseSelection = new CombatTimelineSelection($"state:{(int)StateLayer.Locomotion}:{locomotionId}:{locomotion?.StableId ?? "none"}:{locomotion?.VariantId ?? ""}", locomotion?.Timeline, states.GetStateFrame(StateLayer.Locomotion));
            return new CombatTimelinePlan(baseSelection, actionSelection);
        }
    }

    public sealed class CombatTimelinePlayer
    {
        private readonly TimelineComponent _component;
        private string _baseOwner = string.Empty;
        private string _actionOwner = string.Empty;

        public CombatTimelinePlayer(TimelineComponent component) { _component = component; }

        public void Synchronize(in CombatTimelinePlan plan, Animator animator, bool force = false, float fadeDuration = 0.15f)
        {
            if (_component == null) return;
            var baseChanged = force || !string.Equals(_baseOwner, plan.Base.OwnerKey, StringComparison.Ordinal) ||
                plan.Base.Asset != null && _component.GetTimeline(TimelinePlaybackLayer.Base) == null;
            var actionChanged = force || !string.Equals(_actionOwner, plan.Action.OwnerKey, StringComparison.Ordinal) ||
                plan.Action.Asset != null && _component.GetTimeline(TimelinePlaybackLayer.Action) == null;
            if (baseChanged) SyncLayer(plan.Base, TimelinePlaybackLayer.Base, animator, fadeDuration, false);
            if (plan.ExclusiveBase)
            {
                _component.StopLayer(TimelinePlaybackLayer.Action, 0);
            }
            else if (actionChanged)
            {
                var replayFrameZero = !force && plan.Action.Asset != null && plan.Action.Frame == 0;
                SyncLayer(plan.Action, TimelinePlaybackLayer.Action, animator, fadeDuration, replayFrameZero);
            }
            _baseOwner = plan.Base.OwnerKey;
            _actionOwner = plan.ExclusiveBase ? string.Empty : plan.Action.OwnerKey;
        }

        public void Evaluate(in CombatTimelinePlan plan, bool playback, int deltaFrames = 1)
        {
            if (_component == null) return;
            if (playback)
            {
                _component.EvaluateLayerFrame(TimelinePlaybackLayer.Base, plan.Base.Frame);
                _component.EvaluateLayerFrame(TimelinePlaybackLayer.Action, plan.Action.Frame);
                _component.CompleteLayerFrame(deltaFrames);
            }
            else
            {
                _component.SetLayerFrame(TimelinePlaybackLayer.Base, plan.Base.Frame, false);
                _component.SetLayerFrame(TimelinePlaybackLayer.Action, plan.Action.Frame, false);
            }
        }

        public void Release()
        {
            _baseOwner = string.Empty;
            _actionOwner = string.Empty;
        }

        private void SyncLayer(CombatTimelineSelection selection, TimelinePlaybackLayer layer, Animator animator, float fadeDuration, bool replayFrameZero)
        {
            if (selection.Asset == null)
            {
                _component.StopLayer(layer, 0);
                return;
            }
            _component.PlayOnLayer(selection.Asset, layer, fadeDuration);
            if (selection.Asset.tracks != null && animator != null)
            {
                foreach (var track in selection.Asset.tracks)
                {
                    if (track is AnimationTrackAsset)
                    {
                        _component.SetBinding(track, animator);
                    }
                }
            }
            _component.SetLayerFrame(layer, selection.Frame, replayFrameZero);
        }
    }
}
