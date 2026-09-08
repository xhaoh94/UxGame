using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Combat.Tests
{
    public sealed class CombatEditorWorkflowTests
    {
        [Test]
        public void PrimaryAnimationShortcutCreatesAndUpdatesTimelineClip()
        {
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var animation = new AnimationClip
            {
                frameRate = 60f,
            };
            animation.SetCurve(
                string.Empty,
                typeof(Transform),
                "m_LocalPosition.x",
                AnimationCurve.Linear(0f, 0f, 1f, 1f));

            try
            {
                int durationFrames;
                Assert.IsTrue(CombatEditorUtility.SetPrimaryAnimationClip(
                    timeline,
                    animation,
                    out durationFrames));
                Assert.AreEqual(
                    Mathf.Max(1, Mathf.RoundToInt(animation.length * timeline.FrameRate)),
                    durationFrames);

                var track = timeline.FindTrack<AnimationTrackAsset>();
                Assert.IsNotNull(track);
                Assert.AreEqual(1, track.clips.Count);
                var clip = track.clips[0] as AnimationClipAsset;
                Assert.IsNotNull(clip);
                Assert.AreSame(animation, clip.clip);
                Assert.AreEqual(durationFrames, clip.DurationFrames);

                var replacement = new AnimationClip
                {
                    frameRate = 60f,
                };
                replacement.SetCurve(
                    string.Empty,
                    typeof(Transform),
                    "m_LocalPosition.y",
                    AnimationCurve.Linear(0f, 0f, 0.5f, 1f));
                try
                {
                    Assert.IsTrue(CombatEditorUtility.SetPrimaryAnimationClip(
                        timeline,
                        replacement,
                        out var replacementDuration));
                    Assert.AreSame(replacement, clip.clip);
                    Assert.AreEqual(replacementDuration, clip.DurationFrames);
                }
                finally
                {
                    Object.DestroyImmediate(replacement);
                }

                Assert.IsTrue(CombatEditorUtility.SetPrimaryAnimationClip(
                    timeline,
                    null,
                    out _));
                Assert.AreEqual(0, track.clips.Count);
            }
            finally
            {
                Object.DestroyImmediate(animation);
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void BasicAttackExampleLinksProfileActionTimelineAndAnimation()
        {
            const string profilePath =
                "Assets/Data/Res/Combat/HeroZS/HeroZSCombatProfile222.asset";
            var profile = AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(profilePath);

            Assert.IsNotNull(profile, $"缺少基础普攻示例 Profile：{profilePath}");
            var action = profile.FindAction(1001);
            Assert.IsNotNull(action, "基础普攻示例缺少 Action 1001。");
            Assert.AreEqual(CombatCommandType.Attack, action.TriggerCommand);
            Assert.AreEqual("普通攻击", action.DisplayName);
            Assert.IsNotNull(action.Timeline, "基础普攻示例缺少 Timeline。");
            Assert.AreEqual(profile.FrameRate, action.Timeline.FrameRate);
            Assert.Greater(action.DurationFrames, 0);
            Assert.IsNotNull(
                CombatEditorUtility.GetPrimaryAnimationClip(action.Timeline),
                "基础普攻 Timeline 缺少动画 Clip。");
        }

        [Test]
        public void LegacyFixedStateTimelineMigratesIntoDynamicPresentationList()
        {
            var profile = ScriptableObject.CreateInstance<CharacterCombatProfile>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.SetFrameRate(profile.FrameRate);

            try
            {
                var serialized = new SerializedObject(profile);
                var legacy = serialized.FindProperty("idleTimeline");
                Assert.IsNotNull(legacy);
                legacy.objectReferenceValue = timeline;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                profile.ValidateData();

                Assert.AreEqual(1, profile.StatePresentations.Count);
                var presentation = profile.StatePresentations[0];
                Assert.AreEqual(StateLayer.Locomotion, presentation.Layer);
                Assert.AreEqual((int)LocomotionState.Idle, presentation.StateId);
                Assert.AreEqual(CombatStatePresentation.DefaultVariantId, presentation.VariantId);
                Assert.AreSame(timeline, presentation.Timeline);
                Assert.AreSame(
                    timeline,
                    profile.GetStateTimeline(
                        StateLayer.Locomotion,
                        (int)LocomotionState.Idle));

                serialized.Update();
                Assert.IsNull(serialized.FindProperty("idleTimeline").objectReferenceValue);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(timeline);
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void StatePresentationValidationUsesTimelineOrProfileContext(
            bool hasTimeline,
            bool destroyTimeline)
        {
            var profile = CombatTestProfiles.CreateProfile();
            var timeline = hasTimeline ? ScriptableObject.CreateInstance<TimelineAsset>() : null;
            try
            {
                if (timeline != null)
                {
                    timeline.SetFrameRate(profile.FrameRate);
                }
                CombatTestProfiles.AddStatePresentation(
                    profile,
                    StateLayer.Action,
                    (int)ActionState.Executing,
                    CombatStatePresentation.DefaultVariantId,
                    timeline);
                if (destroyTimeline)
                {
                    Object.DestroyImmediate(timeline);
                }

                var issues = CombatEditorUtility.ValidateProfile(profile);
                var issue = issues.Find(value =>
                    value.Severity == CombatValidationSeverity.Error &&
                    value.Message.Contains("Action 层"));

                Assert.IsNotNull(issue);
                if (hasTimeline && !destroyTimeline)
                {
                    Assert.AreSame(timeline, issue.Context);
                }
                else
                {
                    Assert.AreSame(profile, issue.Context);
                }
            }
            finally
            {
                Object.DestroyImmediate(profile);
                if (timeline != null)
                {
                    Object.DestroyImmediate(timeline);
                }
            }
        }

        [Test]
        public void SameStateCanKeepMultiplePresentationVariants()
        {
            var profile = CombatTestProfiles.CreateProfile();
            try
            {
                CombatTestProfiles.AddStatePresentation(
                    profile,
                    StateLayer.Life,
                    (int)LifeState.Dead,
                    CombatStatePresentation.DefaultVariantId,
                    null);
                CombatTestProfiles.AddStatePresentation(
                    profile,
                    StateLayer.Life,
                    (int)LifeState.Dead,
                    "frozen",
                    null);

                profile.ValidateData();

                Assert.AreEqual(2, profile.StatePresentations.Count);
                Assert.AreEqual(
                    CombatStatePresentation.DefaultVariantId,
                    profile.GetStatePresentation(
                        StateLayer.Life,
                        (int)LifeState.Dead).VariantId);
                Assert.AreEqual(
                    "frozen",
                    profile.GetStatePresentation(
                        StateLayer.Life,
                        (int)LifeState.Dead,
                        "frozen").VariantId);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
