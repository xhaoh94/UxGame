using System;
using UnityEngine;

namespace Ux
{
    public readonly struct CombatTimelineSelection
    {
        public readonly TimelineAsset Asset;
        public readonly int Frame;

        /// <summary>同一资产的第几次播放。只有 Action 槽用：连招时同一资产再次起手必须强制重播动画。</summary>
        public readonly long InstanceId;

        public CombatTimelineSelection(TimelineAsset asset, int frame, long instanceId = 0)
        {
            Asset = asset;
            Frame = Math.Max(0, frame);
            InstanceId = instanceId;
        }

        /// <summary>
        /// 是否同一个来源 —— 唯一用途是判断"要不要重新切轨道"。
        /// 所以 Frame 必须不参与：帧每帧都在变，它变了不该重播。
        /// </summary>
        public bool SameOwner(in CombatTimelineSelection other) =>
            ReferenceEquals(Asset, other.Asset) && InstanceId == other.InstanceId;
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


    public sealed class CombatTimelinePlayer
    {
        private readonly TimelineComponent _component;
        private CombatTimelineSelection _baseOwner;
        private CombatTimelineSelection _actionOwner;

        public CombatTimelinePlayer(TimelineComponent component) { _component = component; }

        public void Synchronize(in CombatTimelinePlan plan, Animator animator, bool force = false, float fadeDuration = 0.15f, ParticleSystem vfx = null)
        {
            if (_component == null) return;
            var baseChanged = force || !_baseOwner.SameOwner(plan.Base) ||
                plan.Base.Asset != null && _component.GetTimeline(TimelinePlaybackLayer.Base) == null;
            var actionChanged = force || !_actionOwner.SameOwner(plan.Action) ||
                plan.Action.Asset != null && _component.GetTimeline(TimelinePlaybackLayer.Action) == null;
            if (baseChanged) SyncLayer(plan.Base, TimelinePlaybackLayer.Base, animator, vfx, fadeDuration, false);
            if (plan.ExclusiveBase)
            {
                _component.StopLayer(TimelinePlaybackLayer.Action, 0);
            }
            else if (actionChanged)
            {
                var replayFrameZero = !force && plan.Action.Asset != null && plan.Action.Frame == 0;
                SyncLayer(plan.Action, TimelinePlaybackLayer.Action, animator, vfx, fadeDuration, replayFrameZero);
            }
            _baseOwner = plan.Base;
            _actionOwner = plan.ExclusiveBase ? default : plan.Action;
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
            _baseOwner = default;
            _actionOwner = default;
        }

        private void SyncLayer(CombatTimelineSelection selection, TimelinePlaybackLayer layer, Animator animator, ParticleSystem vfx, float fadeDuration, bool replayFrameZero)
        {
            if (selection.Asset == null)
            {
                _component.StopLayer(layer, 0);
                return;
            }
            _component.PlayOnLayer(selection.Asset, layer, fadeDuration);
            BindTracks(selection.Asset, animator, vfx);
            _component.SetLayerFrame(layer, selection.Frame, replayFrameZero);
        }

        /// <summary>
        /// 必须在 PlayOnLayer 之后绑定：PlayOnLayer 会新建播放实例并让旧实例淡出，先绑会被新实例丢掉。
        /// 粒子轨没有资产级引用，只能绑外部 ParticleSystem —— 传 null 时该轨静默不播。
        /// </summary>
        private void BindTracks(TimelineAsset asset, Animator animator, ParticleSystem vfx)
        {
            if (asset.tracks == null) return;
            foreach (var track in asset.tracks)
            {
                switch (track)
                {
                    case AnimationTrackAsset when animator != null:
                        _component.SetBinding(track, animator);
                        break;
                    case ParticleAssetTrack when vfx != null:
                        _component.SetBinding(track, vfx);
                        break;
                }
            }
        }
    }
}
