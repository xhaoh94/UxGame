using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public enum TimelineEvaluationMode
    {
        Initialize,
        Playback,
        Seek
    }

    public readonly struct TimelineEvaluationContext
    {
        public readonly int PreviousFrame;
        public readonly int CurrentFrame;
        public readonly int FrameRate;
        public readonly TimelineEvaluationMode Mode;

        public TimelineEvaluationContext(int previousFrame, int currentFrame, int frameRate, TimelineEvaluationMode mode)
        {
            PreviousFrame = previousFrame;
            CurrentFrame = currentFrame;
            FrameRate = Mathf.Max(1, frameRate);
            Mode = mode;
        }

        public int DeltaFrames => CurrentFrame - PreviousFrame;
        public float DeltaTime => DeltaFrames / (float)FrameRate;
        public float CurrentTime => CurrentFrame / (float)FrameRate;
        public bool IsForward => CurrentFrame >= PreviousFrame;
        public bool IsSeek => Mode == TimelineEvaluationMode.Seek;

        // 正向求值时，判断某帧是否在本次区间中被跨越。
        public bool CrossedForward(int frame)
        {
            return IsForward && PreviousFrame < frame && CurrentFrame >= frame;
        }

        /// <summary>
        /// Gameplay/Event Track 应使用此方法。Seek/初始化不会触发事件；播放区间严格使用
        /// (PreviousFrame, CurrentFrame]，首次播放由 Timeline 以 -1 → 0 保证只触发第 0 帧。
        /// </summary>
        public bool ShouldTriggerFrame(int frame)
        {
            return Mode == TimelineEvaluationMode.Playback &&
                   IsForward &&
                   CurrentFrame != PreviousFrame &&
                   CrossedForward(frame);
        }

        public bool CrossedBackward(int frame)
        {
            return !IsForward && CurrentFrame <= frame && PreviousFrame > frame;
        }
    }

    public class Timeline : Entity, IAwakeSystem<TimelineAsset, bool>, IAwakeSystem<TimelineAsset, TimelinePlaybackLayer>
    {
        public int CurrentFrame { get; private set; }
        public int FrameRate => Asset.FrameRate;
        public TimelineAsset Asset { get; private set; }
        public TimelineComponent Component => ParentAs<TimelineComponent>();
        public bool IsDone { get; private set; }
        public bool IsAdditive { get; private set; }
        public TimelinePlaybackLayer PlaybackLayer { get; private set; }
        public int PlaybackOrder { get; private set; }
        public bool IsWeightFadeComplete
        {
            get
            {
                foreach (var track in _tracks)
                {
                    if (!track.IsWeightFadeComplete)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private readonly List<TimelineTrack> _tracks = new();
        private bool _hasPlaybackEvaluation;

        void IAwakeSystem<TimelineAsset, bool>.OnAwake(TimelineAsset asset, bool isAdditive)
        {
            Awake(asset, isAdditive ? TimelinePlaybackLayer.Additive : TimelinePlaybackLayer.Base);
        }

        void IAwakeSystem<TimelineAsset, TimelinePlaybackLayer>.OnAwake(TimelineAsset asset, TimelinePlaybackLayer layer)
        {
            Awake(asset, layer);
        }

        private void Awake(TimelineAsset asset, TimelinePlaybackLayer layer)
        {
            Asset = asset;
            PlaybackLayer = layer;
            PlaybackOrder = Component?.GetNextPlaybackOrder() ?? 0;
            IsAdditive = layer == TimelinePlaybackLayer.Additive;
            CurrentFrame = 0;
            _hasPlaybackEvaluation = false;

            if (asset?.tracks != null)
            {
                for (var index = 0; index < asset.tracks.Count; index++)
                {
                    var trackAsset = asset.tracks[index];
                    if (trackAsset?.TrackType == null)
                    {
                        continue;
                    }

                    if (Add(trackAsset.TrackType, trackAsset, IsFromPool) is TimelineTrack track)
                    {
                        track.TrackOrder = index;
                        _tracks.Add(track);
                    }
                }
            }

            OnBinding();
            EvaluateInternal(new TimelineEvaluationContext(-1, 0, FrameRate, TimelineEvaluationMode.Initialize));
        }

        protected override void OnDestroy()
        {
            _tracks.Clear();
            Asset = null;
            CurrentFrame = 0;
            IsDone = false;
            IsAdditive = false;
            PlaybackLayer = TimelinePlaybackLayer.Base;
            PlaybackOrder = 0;
            _hasPlaybackEvaluation = false;
        }

        public void OnBinding()
        {
            foreach (var track in _tracks)
            {
                track.OnBinding();
            }
        }

        public void EvaluateFrames(int deltaFrames)
        {
            var previousFrame = CurrentFrame;
            if (!_hasPlaybackEvaluation && deltaFrames > 0)
            {
                // 第一次逻辑帧只求值资源第 0 帧，避免同一 Tick 同时触发第 0、1 帧事件。
                previousFrame = -1;
                CurrentFrame = Mathf.Max(0, CurrentFrame + deltaFrames - 1);
            }
            else
            {
                // 主 Timeline 到达资源末尾后仍允许时间继续前进，以支持 Hold/Loop 后外推。
                CurrentFrame = Mathf.Max(0, CurrentFrame + deltaFrames);
            }
            _hasPlaybackEvaluation = true;
            EvaluateInternal(new TimelineEvaluationContext(previousFrame, CurrentFrame, FrameRate, TimelineEvaluationMode.Playback));
        }

        public void Set(int frame, bool replayFrameZero = true)
        {
            var previousFrame = CurrentFrame;
            CurrentFrame = Mathf.Max(0, frame);
            // 通用回滚到第 0 帧后会重放第 0 帧；表现协调器已采样当前帧时可显式关闭重放。
            _hasPlaybackEvaluation = CurrentFrame != 0 || !replayFrameZero;
            EvaluateInternal(new TimelineEvaluationContext(previousFrame, CurrentFrame, FrameRate, TimelineEvaluationMode.Seek));
        }

        public void EvaluatePlaybackFrame(int frame)
        {
            var nextFrame = Mathf.Max(0, frame);
            if (_hasPlaybackEvaluation && nextFrame == CurrentFrame)
            {
                return;
            }

            var previousFrame = _hasPlaybackEvaluation ? CurrentFrame : -1;
            CurrentFrame = nextFrame;
            _hasPlaybackEvaluation = true;
            EvaluateInternal(new TimelineEvaluationContext(
                previousFrame,
                CurrentFrame,
                FrameRate,
                TimelineEvaluationMode.Playback));
        }

        /// <summary>
        /// 动作进入逻辑帧执行当前帧而不推进游标。主要用于新动作第 0 帧，确保不会在一个逻辑帧内同时执行第 0、1 帧事件。
        /// </summary>
        public void EvaluateCurrentFramePlayback()
        {
            var previousFrame = _hasPlaybackEvaluation ? CurrentFrame : CurrentFrame - 1;
            EvaluateInternal(new TimelineEvaluationContext(
                previousFrame,
                CurrentFrame,
                FrameRate,
                TimelineEvaluationMode.Playback));
            _hasPlaybackEvaluation = true;
        }

        private void EvaluateInternal(in TimelineEvaluationContext context)
        {
            foreach (var track in _tracks)
            {
                track.Evaluate(in context);
            }
            IsDone = CurrentFrame >= (Asset?.DurationFrames ?? 0);
        }

        public void StopImmediate()
        {
            foreach (var track in _tracks)
            {
                track.StopImmediate();
            }
        }

        public void StartWeightFade(float destWeight, float fadeDuration)
        {
            foreach (var track in _tracks)
            {
                track.StartWeightFade(destWeight, fadeDuration);
            }
        }

        public void AdvanceWeightFade(float deltaTime)
        {
            foreach (var track in _tracks)
            {
                track.AdvanceWeightFade(deltaTime);
            }
        }
    }
}
