using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    /// <summary>
    /// 同一角色共享一个表现图。命名层独立管理播放实例和本地帧，基础状态切换不会重启动作。
    /// </summary>
    public partial class TimelineComponent : Entity, IAwakeSystem
    {
        private readonly Dictionary<TimelinePlaybackLayer, Timeline> _layers = new();
        private readonly List<Timeline> _fading = new();
        private readonly List<Timeline> _additives = new();
        private readonly Dictionary<TimelineTrackAsset, UnityEngine.Object> _bindings = new();
        private int _playbackOrder;

        /// <summary>兼容旧调用：Current/Last 仅表示基础层。</summary>
        public Timeline Current => GetTimeline(TimelinePlaybackLayer.Base);
        public Timeline Last => _fading.FindLast(timeline => timeline != null && timeline.PlaybackLayer == TimelinePlaybackLayer.Base);
        public PlayableGraph PlayableGraph { get; private set; }
        public bool IsPaused { get; private set; }

        void IAwakeSystem.OnAwake()
        {
            IsPaused = false;
            _playbackOrder = 0;
            PlayableGraph = PlayableGraph.Create(Parent.Name);
            PlayableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            PlayableGraph.Play();
        }

        protected override void OnDestroy()
        {
            foreach (var timeline in _layers.Values)
            {
                RemoveTimeline(timeline);
            }
            foreach (var timeline in _fading)
            {
                RemoveTimeline(timeline);
            }
            foreach (var timeline in _additives)
            {
                RemoveTimeline(timeline);
            }
            _layers.Clear();
            _fading.Clear();
            _additives.Clear();
            _bindings.Clear();
            _playbackOrder = 0;
            if (PlayableGraph.IsValid())
            {
                PlayableGraph.Destroy();
            }
        }

        internal int GetNextPlaybackOrder() => ++_playbackOrder;

        public Timeline GetTimeline(TimelinePlaybackLayer layer)
        {
            return _layers.TryGetValue(layer, out var timeline) && timeline != null && !timeline.IsDestroy
                ? timeline
                : null;
        }

        public Timeline PlayOnLayer(TimelineAsset asset, TimelinePlaybackLayer layer, float fadeDuration = 0.15f)
        {
            if (!CanPlay(asset))
            {
                return null;
            }

            // 同一层最多保留一个退出实例，防止快速连续切换积累无界的淡出对象。
            ClearFadingLayer(layer);
            if (_layers.TryGetValue(layer, out var previous))
            {
                _layers.Remove(layer);
                FadeOut(previous, fadeDuration);
            }

            var timeline = Add<Timeline, TimelineAsset, TimelinePlaybackLayer>(asset, layer, IsFromPool);
            if (timeline == null)
            {
                return null;
            }
            _layers[layer] = timeline;
            timeline.StartWeightFade(1, fadeDuration);
            EvaluateGraph();
            return timeline;
        }

        public void StopLayer(TimelinePlaybackLayer layer, float fadeDuration = 0.15f)
        {
            if (fadeDuration <= 0)
            {
                ClearFadingLayer(layer);
            }
            if (_layers.TryGetValue(layer, out var timeline))
            {
                _layers.Remove(layer);
                FadeOut(timeline, fadeDuration);
            }
            EvaluateGraph();
        }

        /// <summary>只定位指定层；回滚、编辑器拖标尺等定位不会触发表现事件。</summary>
        public void SetLayerFrame(TimelinePlaybackLayer layer, int frame, bool replayFrameZero = false)
        {
            GetTimeline(layer)?.Set(frame, replayFrameZero);
            EvaluateGraph();
        }

        /// <summary>按权威本地帧播放指定层；相同帧重复求值不会重复触发事件。</summary>
        public void EvaluateLayerFrame(TimelinePlaybackLayer layer, int frame)
        {
            if (IsPaused)
            {
                return;
            }
            GetTimeline(layer)?.EvaluatePlaybackFrame(frame);
            EvaluateGraph();
        }

        /// <summary>
        /// 分层协调器提交帧后的收尾。退出实例冻结姿势只淡出；不能再次推进已求值的当前层。
        /// 旧版并行播放的临时 Timeline 仍按自身本地帧推进。
        /// </summary>
        public void CompleteLayerFrame(int deltaFrames = 1)
        {
            if (IsPaused)
            {
                return;
            }
            foreach (var timeline in _layers.Values)
            {
                timeline.AdvanceWeightFade(Mathf.Abs(deltaFrames) / (float)timeline.FrameRate);
            }
            foreach (var timeline in _fading)
            {
                timeline.AdvanceWeightFade(Mathf.Abs(deltaFrames) / (float)timeline.FrameRate);
            }
            for (var i = _fading.Count - 1; i >= 0; i--)
            {
                var timeline = _fading[i];
                if (timeline.IsWeightFadeComplete)
                {
                    _fading.RemoveAt(i);
                    RemoveTimeline(timeline);
                }
            }
            for (var i = _additives.Count - 1; i >= 0; i--)
            {
                var timeline = _additives[i];
                if (deltaFrames != 0)
                {
                    timeline.EvaluateFrames(deltaFrames);
                }
                if (timeline.IsDone)
                {
                    _additives.RemoveAt(i);
                    RemoveTimeline(timeline);
                }
            }
            EvaluateGraph();
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;

        public void Tick(int deltaFrames = 1)
        {
            if (IsPaused || deltaFrames == 0)
            {
                return;
            }
            foreach (var timeline in _layers.Values)
            {
                timeline.EvaluateFrames(deltaFrames);
            }
            CompleteLayerFrame(deltaFrames);
        }

        public void TickCurrentFrame()
        {
            if (IsPaused)
            {
                return;
            }
            Current?.EvaluateCurrentFramePlayback();
            CompleteLayerFrame(0);
        }

        /// <summary>兼容旧接口。并行 Timeline 不等于增量姿势，增量开关仍由动画轨道控制。</summary>
        public void Play(TimelineAsset timeline, bool isAdditive = false)
        {
            if (!isAdditive)
            {
                PlayOnLayer(timeline, TimelinePlaybackLayer.Base, 0.3f);
                return;
            }
            if (!CanPlay(timeline))
            {
                return;
            }
            var additive = Add<Timeline, TimelineAsset, bool>(timeline, true, IsFromPool);
            if (additive == null)
            {
                return;
            }
            additive.StartWeightFade(1, 0.3f);
            _additives.Add(additive);
            EvaluateGraph();
        }

        public void Stop(bool clearAdditives = true)
        {
            foreach (var timeline in _layers.Values)
            {
                RemoveTimeline(timeline);
            }
            _layers.Clear();
            foreach (var timeline in _fading)
            {
                RemoveTimeline(timeline);
            }
            _fading.Clear();
            if (clearAdditives)
            {
                foreach (var timeline in _additives)
                {
                    RemoveTimeline(timeline);
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
            return track != null && _bindings.TryGetValue(track, out var target) ? target as T : null;
        }

        public void ClearBindings()
        {
            _bindings.Clear();
            RebindTimelines();
            EvaluateGraph();
        }

        /// <summary>兼容旧的整图定位；分层播放应使用 SetLayerFrame 保持各层独立帧。</summary>
        public void Set(int frame, bool replayFrameZero = true)
        {
            foreach (var timeline in _layers.Values)
            {
                timeline.Set(frame, replayFrameZero);
            }
            foreach (var additive in _additives)
            {
                additive.Set(frame, replayFrameZero);
            }
            EvaluateGraph();
        }

        private void RebindTimelines()
        {
            if (!PlayableGraph.IsValid())
            {
                return;
            }
            foreach (var timeline in _layers.Values)
            {
                timeline.OnBinding();
            }
            foreach (var timeline in _fading)
            {
                timeline.OnBinding();
            }
            foreach (var timeline in _additives)
            {
                timeline.OnBinding();
            }
        }

        private bool CanPlay(TimelineAsset asset)
        {
            if (asset == null || !PlayableGraph.IsValid())
            {
                return false;
            }
            var clock = SimulationClock.Ins;
            if (clock.IsRunning && asset.FrameRate != clock.FrameRate)
            {
                Log.Error($"Timeline 帧率必须与逻辑帧率一致：asset={asset.name}, timeline={asset.FrameRate}, logic={clock.FrameRate}");
                return false;
            }
            return true;
        }

        private void FadeOut(Timeline timeline, float fadeDuration)
        {
            if (timeline == null || timeline.IsDestroy)
            {
                return;
            }
            timeline.StopImmediate();
            timeline.StartWeightFade(0, fadeDuration);
            if (timeline.IsWeightFadeComplete)
            {
                RemoveTimeline(timeline);
            }
            else
            {
                _fading.Add(timeline);
            }
        }

        private void ClearFadingLayer(TimelinePlaybackLayer layer)
        {
            for (var i = _fading.Count - 1; i >= 0; i--)
            {
                if (_fading[i].PlaybackLayer == layer)
                {
                    var timeline = _fading[i];
                    _fading.RemoveAt(i);
                    RemoveTimeline(timeline);
                }
            }
        }

        private void RemoveTimeline(Timeline timeline)
        {
            if (timeline == null || timeline.IsDestroy)
            {
                return;
            }
            timeline.StopImmediate();
            timeline.StartWeightFade(0, 0);
            Remove(timeline);
        }

        private void EvaluateGraph()
        {
            if (PlayableGraph.IsValid())
            {
                PlayableGraph.Evaluate(0);
            }
        }
    }
}
