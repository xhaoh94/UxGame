using System;
using UnityEngine;

namespace Ux
{
    public class TimelineMgr : Singleton<TimelineMgr>
    {
        public TimelineAsset LoadAsset(string assetName)
        {
            var assetPath = string.Format(PathHelper.Res.Timeline, assetName);
            return ResMgr.Ins.LoadAsset<TimelineAsset>(assetPath);
        }
    }

    public enum SimulationClockMode
    {
        Stopped,
        LocalRealtime,
        External,
        Replay
    }

    /// <summary>
    /// 与网络方案解耦的固定逻辑帧时钟。单机由本地时间驱动，帧同步和录像由外部帧源驱动。
    /// 状态同步仍使用 LocalRealtime，服务器快照通过独立的状态同步层纠正实体状态。
    /// </summary>
    public sealed class SimulationClock : Singleton<SimulationClock>
    {
        public event Action<long> FrameAdvanced;

        public SimulationClockMode Mode { get; private set; } = SimulationClockMode.Stopped;
        public int FrameRate { get; private set; } = TimelineAsset.DefaultFrameRate;
        public long CurrentFrame { get; private set; }
        public bool IsRunning => Mode != SimulationClockMode.Stopped;
        public bool IsPaused { get; private set; }
        public int MaxCatchUpFramesPerUpdate { get; set; } = 8;
        public float InterpolationAlpha => Mode == SimulationClockMode.LocalRealtime
            ? (float)Math.Clamp(_accumulator / FrameDuration, 0, 1)
            : 0;

        private double _accumulator;
        private double FrameDuration => 1d / FrameRate;

        protected override void OnCreated()
        {
            GameMethod.Update += Update;
        }

        /// <summary>单机和状态同步客户端使用本地固定步长推进。</summary>
        public void StartLocalRealtime(int frameRate = TimelineAsset.DefaultFrameRate, long startFrame = 0)
        {
            Start(SimulationClockMode.LocalRealtime, frameRate, startFrame);
        }

        /// <summary>帧同步客户端使用服务器确认帧或输入帧推进。</summary>
        public void StartExternal(int frameRate = TimelineAsset.DefaultFrameRate, long startFrame = 0)
        {
            Start(SimulationClockMode.External, frameRate, startFrame);
        }

        /// <summary>录像系统使用录制数据逐帧推进。</summary>
        public void StartReplay(int frameRate = TimelineAsset.DefaultFrameRate, long startFrame = 0)
        {
            Start(SimulationClockMode.Replay, frameRate, startFrame);
        }

        public void Stop()
        {
            Mode = SimulationClockMode.Stopped;
            IsPaused = false;
            _accumulator = 0;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        /// <summary>暂停调试、外部帧源和录像均可使用的确定性推进入口。</summary>
        public void Step(int frameCount = 1)
        {
            if (!IsRunning || frameCount <= 0)
            {
                return;
            }
            if (Mode == SimulationClockMode.LocalRealtime && !IsPaused)
            {
                Log.Error("LocalRealtime 模式必须暂停后才能手动 Step");
                return;
            }

            for (var i = 0; i < frameCount; i++)
            {
                AdvanceOneFrame();
            }
        }

        /// <summary>
        /// 外部帧源追赶到目标帧，只允许正向推进。状态同步快照纠正不能使用此接口，
        /// 应恢复实体状态并使用 Timeline.Set(actionFrame) 对齐表现。
        /// </summary>
        public void AdvanceExternalTo(long targetFrame)
        {
            if (Mode != SimulationClockMode.External && Mode != SimulationClockMode.Replay)
            {
                Log.Error($"当前逻辑帧模式不允许外部追帧: {Mode}");
                return;
            }

            while (CurrentFrame < targetFrame)
            {
                AdvanceOneFrame();
            }
        }

        private void Start(SimulationClockMode mode, int frameRate, long startFrame)
        {
            Mode = mode;
            FrameRate = Math.Max(1, frameRate);
            CurrentFrame = Math.Max(0, startFrame);
            _accumulator = 0;
            IsPaused = false;
        }

        private void Update()
        {
            if (Mode != SimulationClockMode.LocalRealtime || IsPaused)
            {
                return;
            }

            _accumulator += Time.unscaledDeltaTime;
            var catchUpCount = 0;
            var catchUpLimit = Math.Max(1, MaxCatchUpFramesPerUpdate);
            while (_accumulator >= FrameDuration && catchUpCount < catchUpLimit)
            {
                _accumulator -= FrameDuration;
                catchUpCount++;
                AdvanceOneFrame();
            }
        }

        private void AdvanceOneFrame()
        {
            CurrentFrame++;
            FrameAdvanced?.Invoke(CurrentFrame);
        }
    }
}
