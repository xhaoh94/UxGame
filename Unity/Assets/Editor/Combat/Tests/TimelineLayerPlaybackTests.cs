using System;
using NUnit.Framework;
using UnityEngine;

namespace Ux.Editor.Combat.Tests
{
    public sealed class TimelineLayerPlaybackTests
    {
        [Test]
        public void BaseAndActionLayersKeepIndependentFrames()
        {
            var baseAsset = CreateAsset("Base");
            var actionAsset = CreateAsset("Action");
            var root = Entity.Create<TestRootEntity>(false);
            var component = root.Add<TimelineComponent>(false);
            try
            {
                var baseTimeline = component.PlayOnLayer(baseAsset, TimelinePlaybackLayer.Base, 0);
                var actionTimeline = component.PlayOnLayer(actionAsset, TimelinePlaybackLayer.Action, 0);
                component.SetLayerFrame(TimelinePlaybackLayer.Base, 12, false);
                component.SetLayerFrame(TimelinePlaybackLayer.Action, 4, false);

                Assert.AreSame(baseTimeline, component.GetTimeline(TimelinePlaybackLayer.Base));
                Assert.AreSame(actionTimeline, component.GetTimeline(TimelinePlaybackLayer.Action));
                Assert.AreEqual(12, baseTimeline.CurrentFrame);
                Assert.AreEqual(4, actionTimeline.CurrentFrame);

                component.StopLayer(TimelinePlaybackLayer.Action, 0);

                Assert.IsNull(component.GetTimeline(TimelinePlaybackLayer.Action));
                Assert.AreSame(baseTimeline, component.GetTimeline(TimelinePlaybackLayer.Base));
                Assert.AreEqual(12, component.GetTimeline(TimelinePlaybackLayer.Base).CurrentFrame);
            }
            finally
            {
                if (root != null && !root.IsDestroy)
                {
                    root.Destroy();
                }
                UnityEngine.Object.DestroyImmediate(baseAsset);
                UnityEngine.Object.DestroyImmediate(actionAsset);
            }
        }

        private static TimelineAsset CreateAsset(string name)
        {
            var asset = ScriptableObject.CreateInstance<TimelineAsset>();
            asset.name = name;
            asset.tracks.Add(new TestTrackAsset { trackName = name });
            asset.ValidateData();
            return asset;
        }

        private sealed class TestRootEntity : Entity
        {
        }

        private sealed class TestTrackAsset : TimelineTrackAsset
        {
            public override Type TrackType => typeof(TestTrack);
        }

        private sealed class TestTrack : TimelineTrack
        {
            protected override void OnStart(TimelineTrackAsset asset)
            {
            }

            protected override void OnEvaluate(in TimelineEvaluationContext context)
            {
            }
        }
    }
}
