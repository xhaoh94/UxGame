using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    /// <summary>
    /// 战斗 Timeline 仅由外部逻辑帧驱动。每次 Tick 对应一个 Timeline 资源帧。
    /// </summary>
    public partial class TimelineComponent : Entity, IAwakeSystem
    {
        public Timeline Current { get; private set; }
        public Timeline Last { get; private set; }
        public PlayableGraph PlayableGraph { get; private set; }
        public bool IsPaused { get; private set; }

        private readonly List<Timeline> _additives = new();
        private readonly Dictionary<TimelineTrackAsset, UnityEngine.Object> _bindings = new();

        void IAwakeSystem.OnAwake()
        {
            IsPaused = false;
            PlayableGraph = PlayableGraph.Create(Parent.Name);
            PlayableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            PlayableGraph.Play();
        }

        protected override void OnDestroy()
        {
            Current = null;
            Last = null;
            _additives.Clear();
            _bindings.Clear();
            if (PlayableGraph.IsValid())
            {
                PlayableGraph.Destroy();
            }
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        /// <summary>
        /// 战斗逻辑帧入口。帧源可以来自单机时钟、网络帧或录像；跳帧时事件区间不会遗漏。
        /// </summary>
        public void Tick(int deltaFrames = 1)
        {
            if (IsPaused || deltaFrames == 0)
            {
                return;
            }

            Current?.EvaluateFrames(deltaFrames);
            Last?.EvaluateFrames(deltaFrames);
            foreach (var additive in _additives)
            {
                additive.EvaluateFrames(deltaFrames);
            }

            CleanupFinishedTimelines();
            EvaluateGraph();
        }

        /// <summary>执行当前主 Timeline 帧但不推进游标，用于新动作进入时的第 0 帧。</summary>
        public void TickCurrentFrame()
        {
            if (IsPaused || Current == null)
            {
                return;
            }
            Current.EvaluateCurrentFramePlayback();
            CleanupFinishedTimelines(false);
            EvaluateGraph();
        }

        public void Play(TimelineAsset timeline, bool isAdditive = false)
        {
            if (timeline == null || !PlayableGraph.IsValid())
            {
                return;
            }

            var frameClock = SimulationClock.Ins;
            if (frameClock.IsRunning && timeline.FrameRate != frameClock.FrameRate)
            {
                Log.Error($"Timeline 帧率必须与逻辑帧率一致: asset={timeline.name}, timeline={timeline.FrameRate}, logic={frameClock.FrameRate}");
                return;
            }

            if (isAdditive)
            {
                var additive = Add<Timeline, TimelineAsset, bool>(timeline, true);
                additive.StartWeightFade(1, 0.3f);
                _additives.Insert(0, additive);
                EvaluateGraph();
                return;
            }

            if (Last != null)
            {
                RemoveTimeline(Last);
                Last = null;
            }

            if (Current != null)
            {
                if (Application.isPlaying)
                {
                    Last = Current;
                }
                else
                {
                    RemoveTimeline(Current);
                }
            }

            Current = Add<Timeline, TimelineAsset, bool>(timeline, false);
            Current.StartWeightFade(1, 0.3f);
            Last?.StartWeightFade(0, 0.3f);
            EvaluateGraph();
        }

        public void Stop(bool clearAdditives = true)
        {
            if (Current != null)
            {
                RemoveTimeline(Current);
                Current = null;
            }
            if (Last != null)
            {
                RemoveTimeline(Last);
                Last = null;
            }
            if (clearAdditives)
            {
                for (var i = _additives.Count - 1; i >= 0; i--)
                {
                    RemoveTimeline(_additives[i]);
                }
                _additives.Clear();
            }
            EvaluateGraph();
        }

        public void SetBinding(TimelineTrackAsset track, UnityEngine.Object target)
        {
            if (track == null)
            {
                return;
            }

            if (target == null)
            {
                _bindings.Remove(track);
            }
            else
            {
                _bindings[track] = target;
            }

            RebindTimelines();
            EvaluateGraph();
        }

        public T GetBinding<T>(TimelineTrackAsset track) where T : UnityEngine.Object
        {
            if (track != null && _bindings.TryGetValue(track, out var target))
            {
                return target as T;
            }
            return null;
        }

        public void ClearBindings()
        {
            _bindings.Clear();
            RebindTimelines();
            EvaluateGraph();
        }

        /// <summary>
        /// 绝对帧定位属于 Seek，不触发 Gameplay/Event Track。
        /// </summary>
        public void Set(int frame, bool replayFrameZero = true)
        {
            if (!PlayableGraph.IsValid())
            {
                return;
            }

            Current?.Set(frame, replayFrameZero);
            Last?.Set(frame, replayFrameZero);
            foreach (var additive in _additives)
            {
                additive.Set(frame, replayFrameZero);
            }
            CleanupFinishedTimelines(false);
            EvaluateGraph();
        }

        private void RebindTimelines()
        {
            if (!PlayableGraph.IsValid())
            {
                return;
            }

            Current?.OnBinding();
            Last?.OnBinding();
            foreach (var additive in _additives)
            {
                additive.OnBinding();
            }
        }

        private void CleanupFinishedTimelines(bool removeCurrentFade = true)
        {
            if (Last != null && removeCurrentFade && Last.IsWeightFadeComplete)
            {
                RemoveTimeline(Last);
                Last = null;
            }

            for (var i = _additives.Count - 1; i >= 0; i--)
            {
                var additive = _additives[i];
                if (!additive.IsDone)
                {
                    continue;
                }
                RemoveTimeline(additive);
                _additives.RemoveAt(i);
            }
        }

        private void RemoveTimeline(Timeline timeline)
        {
            timeline.StopImmediate();
            timeline.StartWeightFade(0, 0);
            Remove(timeline);
        }

        private void EvaluateGraph()
        {
            if (PlayableGraph.IsValid())
            {
                // Clip 使用绝对帧采样；Evaluate(0) 只刷新最终姿势。
                PlayableGraph.Evaluate(0);
            }
        }
    }
}
