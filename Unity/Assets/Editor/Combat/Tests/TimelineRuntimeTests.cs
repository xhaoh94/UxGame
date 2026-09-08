using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Ux.Editor.Combat.Tests
{
    /// <summary>
    /// Timeline 最小可复现用例：固定帧边界、Seek/Playback 事件语义和重复清理。
    /// 测试 Track/Clip 只记录生命周期，不依赖具体动画或粒子资源。
    /// </summary>
    public sealed class TimelineRuntimeTests
    {
        [Test]
        public void ClipIntervalIsStartInclusiveAndEndExclusive()
        {
            using (var fixture = RecordingFixture.Create(2, 4))
            {
                var clip = fixture.Clip;
                Assert.AreEqual(TimelineClip.TLClipStatus.Pre, clip.Status);
                Assert.AreEqual(0, clip.EnableCount);

                fixture.Timeline.Set(1);
                Assert.AreEqual(TimelineClip.TLClipStatus.Pre, clip.Status);
                Assert.AreEqual(0, clip.EnableCount);

                fixture.Timeline.Set(2);
                Assert.AreEqual(TimelineClip.TLClipStatus.Ing, clip.Status);
                Assert.AreEqual(1, clip.EnableCount);

                fixture.Timeline.Set(3);
                Assert.AreEqual(TimelineClip.TLClipStatus.Ing, clip.Status);
                Assert.AreEqual(0, clip.DisableCount);

                fixture.Timeline.Set(4);
                Assert.AreEqual(TimelineClip.TLClipStatus.Post, clip.Status);
                Assert.AreEqual(1, clip.DisableCount);
            }
        }

        [Test]
        public void FirstPlaybackTickEvaluatesFrameZeroOnlyOnce()
        {
            using (var fixture = RecordingFixture.Create(0, 4, 0))
            {
                Assert.AreEqual(0, fixture.Clip.TriggerCount,
                    "初始化求值不能触发 Gameplay/Event 事件。");

                fixture.Timeline.EvaluateFrames(1);
                Assert.AreEqual(0, fixture.Timeline.CurrentFrame);
                Assert.AreEqual(1, fixture.Clip.TriggerCount);

                fixture.Timeline.EvaluateFrames(1);
                Assert.AreEqual(1, fixture.Timeline.CurrentFrame);
                Assert.AreEqual(1, fixture.Clip.TriggerCount,
                    "第 0 帧事件不能在下一逻辑帧重复触发。");
            }
        }

        [Test]
        public void SeekDoesNotTriggerEventAndForwardSkipDoesNotMissIt()
        {
            using (var fixture = RecordingFixture.Create(0, 6, 2))
            {
                fixture.Timeline.Set(3);
                Assert.AreEqual(0, fixture.Clip.TriggerCount,
                    "绝对帧定位属于 Seek，不能触发 Gameplay/Event 事件。");

                fixture.Timeline.Set(0);
                fixture.Timeline.EvaluateFrames(3);
                Assert.AreEqual(2, fixture.Timeline.CurrentFrame);
                Assert.AreEqual(1, fixture.Clip.TriggerCount,
                    "播放跳帧应按 (PreviousFrame, CurrentFrame] 覆盖中间事件帧。");
            }
        }

        [Test]
        public void SetFrameRatePreservesClipTimePositions()
        {
            var asset = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = new RecordingTrackAsset { trackName = "测试" };
            track.clips.Add(new RecordingClipAsset
            {
                StartFrame = 20,
                EndFrame = 60,
                InFrame = 30,
                OutFrame = 50,
            });
            asset.tracks.Add(track);
            asset.ValidateData();

            try
            {
                Assert.IsTrue(asset.SetFrameRate(30));
                var clip = (RecordingClipAsset)track.clips[0];
                Assert.AreEqual(10, clip.StartFrame);
                Assert.AreEqual(30, clip.EndFrame);
                Assert.AreEqual(15, clip.InFrame);
                Assert.AreEqual(25, clip.OutFrame);
                Assert.AreEqual(1f, asset.FrameToTime(30), 0.0001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void StopImmediateThenDestroyDoesNotDisableTwice()
        {
            using (var fixture = RecordingFixture.Create(0, 10))
            {
                var clip = fixture.Clip;
                Assert.AreEqual(1, clip.EnableCount);

                fixture.Timeline.StopImmediate();
                fixture.Timeline.StopImmediate();
                Assert.AreEqual(1, clip.DisableCount);
                Assert.AreEqual(0, clip.StopCount);
                Assert.AreEqual(TimelineClip.TLClipStatus.Stop, clip.Status);

                fixture.Timeline.Destroy();
                Assert.AreEqual(1, clip.DisableCount);
                Assert.AreEqual(1, clip.StopCount);
            }
        }

        [Test]
        public void UnboundAnimationTrackDoesNotBlockFadeCleanup()
        {
            var asset = ScriptableObject.CreateInstance<TimelineAsset>();
            asset.tracks.Add(new AnimationTrackAsset { trackName = "动画" });
            asset.ValidateData();

            using (var fixture = TimelineFixture.Create(asset))
            {
                var track = fixture.Timeline.Get<TLAnimationTrack>();
                Assert.IsNotNull(track);
                Assert.IsNull(track.Output);

                SetPrivateField(track, "_fadeWeight", 0f);
                SetPrivateField(track, "_isFading", true);

                Assert.IsTrue(track.IsWeightFadeComplete,
                    "未绑定 Animator 或 Output 已失效时，淡出到 0 不应让 Last Timeline 永久滞留。");
            }
        }

        private static void SetPrivateField<T>(TLAnimationTrack track, string name, T value)
        {
            var field = typeof(TLAnimationTrack).GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"找不到字段 {name}");
            field.SetValue(track, value);
        }

        public sealed class RecordingTrackAsset : TimelineTrackAsset
        {
            public override Type TrackType => typeof(RecordingTrack);
        }

        public sealed class RecordingClipAsset : TimelineClipAsset
        {
            public int TriggerFrame;
            public override Type ClipType => typeof(RecordingClip);
        }

        public sealed class RecordingTrack : TimelineTrack
        {
            protected override void OnStart(TimelineTrackAsset asset)
            {
            }

            protected override void OnEvaluate(in TimelineEvaluationContext context)
            {
            }
        }

        public sealed class RecordingClip : TimelineClip
        {
            private RecordingClipAsset _asset;

            public int EnableCount { get; private set; }
            public int DisableCount { get; private set; }
            public int StopCount { get; private set; }
            public int TriggerCount { get; private set; }
            public List<int> EvaluatedFrames { get; } = new List<int>();

            protected override void OnStart(TimelineClipAsset asset)
            {
                _asset = asset as RecordingClipAsset;
            }

            protected override void OnEnable()
            {
                EnableCount++;
            }

            protected override void OnDisable()
            {
                DisableCount++;
            }

            protected override void OnStop()
            {
                StopCount++;
                _asset = null;
            }

            protected override void OnEvaluate(in TimelineEvaluationContext context)
            {
                EvaluatedFrames.Add(context.CurrentFrame);
                if (_asset != null && context.ShouldTriggerFrame(_asset.TriggerFrame))
                {
                    TriggerCount++;
                }
            }
        }

        public sealed class TestRootEntity : Entity
        {
        }

        private class TimelineFixture : IDisposable
        {
            protected TimelineFixture(
                TestRootEntity root,
                TimelineComponent component,
                TimelineAsset asset,
                Ux.Timeline timeline)
            {
                Root = root;
                Component = component;
                Asset = asset;
                Timeline = timeline;
            }

            protected TestRootEntity Root { get; }
            protected TimelineComponent Component { get; }
            protected TimelineAsset Asset { get; }
            public Ux.Timeline Timeline { get; }

            public static TimelineFixture Create(TimelineAsset asset)
            {
                var root = Entity.Create<TestRootEntity>(false);
                var component = root.Add<TimelineComponent>(false);
                var timeline = component.Add<Ux.Timeline, TimelineAsset, bool>(asset, false, false);
                return new TimelineFixture(root, component, asset, timeline);
            }

            public void Dispose()
            {
                if (Root != null && !Root.IsDestroy)
                {
                    Root.Destroy();
                }
                if (Asset != null)
                {
                    UnityEngine.Object.DestroyImmediate(Asset);
                }
            }
        }

        private sealed class RecordingFixture : TimelineFixture
        {
            private RecordingFixture(
                TestRootEntity root,
                TimelineComponent component,
                TimelineAsset asset,
                Ux.Timeline timeline,
                RecordingClip clip)
                : base(root, component, asset, timeline)
            {
                Clip = clip;
            }

            public RecordingClip Clip { get; }

            public static RecordingFixture Create(
                int startFrame,
                int endFrame,
                int triggerFrame = 0)
            {
                var asset = ScriptableObject.CreateInstance<TimelineAsset>();
                var trackAsset = new RecordingTrackAsset { trackName = "测试" };
                trackAsset.clips.Add(new RecordingClipAsset
                {
                    StartFrame = startFrame,
                    EndFrame = endFrame,
                    TriggerFrame = triggerFrame,
                    clipName = "记录 Clip",
                });
                asset.tracks.Add(trackAsset);
                asset.ValidateData();

                var root = Entity.Create<TestRootEntity>(false);
                var component = root.Add<TimelineComponent>(false);
                var timeline = component.Add<Ux.Timeline, TimelineAsset, bool>(asset, false, false);
                var track = timeline.Get<RecordingTrack>();
                var clip = track?.Get<RecordingClip>();
                Assert.IsNotNull(clip);
                return new RecordingFixture(root, component, asset, timeline, clip);
            }
        }
    }
}
