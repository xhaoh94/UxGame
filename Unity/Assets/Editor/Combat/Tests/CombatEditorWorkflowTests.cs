using System.Linq;
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
                "Assets/Data/Res/Combat/HeroZS/HeroZSCombatProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(profilePath);

            Assert.IsNotNull(profile, $"缺少基础普攻示例 Profile：{profilePath}");
            var action = profile.FindAction(1001);
            Assert.IsNotNull(action, "基础普攻示例缺少 Action 1001。");
            Assert.AreEqual("普通攻击", action.DisplayName);
            var timeline = profile.GetActionTimeline(action.ActionId);
            Assert.IsNotNull(timeline, "基础普攻示例缺少表现 Timeline 映射。");
            Assert.AreEqual(profile.FrameRate, timeline.FrameRate);
            Assert.Greater(action.DurationFrames, 0);
            Assert.IsNotNull(
                CombatEditorUtility.GetPrimaryAnimationClip(timeline),
                "基础普攻 Timeline 缺少动画 Clip。");
        }

        [Test]
        public void BasicAttackExampleUsesFireAndQOnly()
        {
            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/Settings/Input/InputActions.inputactions");
            var operate = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Hotfix/Common/Operate/OperateComponent.cs");

            Assert.IsNotNull(inputAsset);
            Assert.IsNotNull(operate);
            var fireAction = inputAsset.FindAction("Player/Fire", true);
            var keyAction = inputAsset.FindAction("Player/Key", true);
            Assert.IsTrue(fireAction.bindings.Any(binding => binding.path == "<Mouse>/leftButton"));
            Assert.IsTrue(keyAction.bindings.Any(binding => binding.path == "<Keyboard>/q"));
            StringAssert.Contains("OnFire", operate.text);
            StringAssert.Contains("RequestAction(AttackActionId)", operate.text);
            StringAssert.DoesNotContain("Skill01ActionId", operate.text);
            StringAssert.DoesNotContain("DodgeActionId", operate.text);
        }

        [Test]
        public void ActionSchemaUsesOnlyExplicitActionIds()
        {
            var action = CombatTestProfiles.CreateAction(1001, "attack", 30);
            try
            {
                var serialized = new SerializedObject(action);
                Assert.IsNull(serialized.FindProperty("triggerCommand"));
                Assert.IsNull(serialized.FindProperty("priority"));

                var windows = serialized.FindProperty("cancelWindows");
                windows.arraySize = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();
                var window = windows.GetArrayElementAtIndex(0);
                Assert.IsNull(window.FindPropertyRelative("AcceptedCommand"));
                Assert.IsNull(window.FindPropertyRelative("Priority"));
                Assert.IsNotNull(window.FindPropertyRelative("TargetActionId"));
            }
            finally
            {
                Object.DestroyImmediate(action);
            }
        }

        [Test]
        public void DefaultHeroProfileKeepsIdleRunAndBasicAttackPresentations()
        {
            const string profilePath =
                "Assets/Data/Res/Combat/HeroZS/HeroZSCombatProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(profilePath);

            Assert.IsNotNull(profile, $"缺少默认 HeroZS Profile：{profilePath}");
            var idleTimeline = profile.GetStateTimeline(
                StateLayer.Locomotion,
                (int)LocomotionState.Idle);
            var runTimeline = profile.GetStateTimeline(
                StateLayer.Locomotion,
                (int)LocomotionState.Move);
            Assert.IsNotNull(idleTimeline);
            Assert.IsNotNull(runTimeline);
            Assert.IsNotNull(CombatEditorUtility.GetPrimaryAnimationClip(idleTimeline));
            Assert.IsNotNull(CombatEditorUtility.GetPrimaryAnimationClip(runTimeline));
            var attack = profile.FindAction(1001);
            Assert.IsNotNull(attack);
            Assert.AreEqual("普通攻击", attack.DisplayName);
            Assert.IsNotNull(profile.GetActionTimeline(attack));
            Assert.DoesNotThrow(() => profile.ValidateRuntime());
        }

        [Test]
        public void CreateActionAssetsPersistsBothOwnersAndSupportsUndoRedo()
        {
            var key = $"CombatEditorWorkflow_{System.Guid.NewGuid():N}";
            var testFolder = $"Assets/{key}";
            var profilePath = $"{testFolder}/TestProfile.asset";
            var animationPath = $"{testFolder}/TestAnimation.anim";
            CombatEditorUtility.EnsureFolder(testFolder);
            var profile = CombatTestProfiles.CreateProfile();
            var profileSerialized = new SerializedObject(profile);
            profileSerialized.FindProperty("group").stringValue = key;
            profileSerialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, profilePath);
            var animation = new AnimationClip
            {
                frameRate = 60f,
            };
            animation.SetCurve(
                string.Empty,
                typeof(Transform),
                "m_LocalPosition.x",
                AnimationCurve.Linear(0f, 0f, 0.5f, 1f));
            AssetDatabase.CreateAsset(animation, animationPath);
            AssetDatabase.SaveAssets();

            string actionPath = null;
            string timelinePath = null;
            try
            {
                Assert.IsTrue(CombatEditorUtility.TryCreateActionAssets(
                    profile,
                    1001,
                    "test.action.1001",
                    "测试技能",
                    30,
                    ActionMovementPolicy.Block,
                    "Action1001",
                    animation,
                    out var action,
                    out var timeline,
                    out var error), error);

                actionPath = AssetDatabase.GetAssetPath(action);
                timelinePath = AssetDatabase.GetAssetPath(timeline);
                Assert.IsFalse(string.IsNullOrEmpty(actionPath));
                Assert.IsFalse(string.IsNullOrEmpty(timelinePath));
                Assert.AreSame(action, profile.FindAction(1001));
                Assert.AreSame(timeline, profile.GetActionTimeline(action));
                Assert.AreSame(animation, CombatEditorUtility.GetPrimaryAnimationClip(timeline));
                Assert.IsNull(new SerializedObject(action).FindProperty("timeline"));

                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(profilePath);
                var persistedProfile = AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(profilePath);
                var persistedAction = persistedProfile.FindAction(1001);
                Assert.IsNotNull(persistedAction);
                Assert.AreSame(timeline, persistedProfile.GetActionTimeline(persistedAction));

                Undo.PerformUndo();
                AssetDatabase.Refresh();
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<CombatActionAsset>(actionPath));
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath));
                var reverted = new SerializedObject(profile);
                reverted.Update();
                Assert.AreEqual(0, reverted.FindProperty("actions").arraySize);
                Assert.AreEqual(0, reverted.FindProperty("actionPresentations").arraySize);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(profilePath);
                var revertedProfile = AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(profilePath);
                Assert.AreEqual(0, revertedProfile.Actions.Count);
                Assert.AreEqual(0, revertedProfile.ActionPresentations.Count);

                Undo.PerformRedo();
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(profilePath);
                var restoredAction = AssetDatabase.LoadAssetAtPath<CombatActionAsset>(actionPath);
                var restoredTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
                var restoredProfile = AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(profilePath);
                Assert.IsNotNull(restoredAction);
                Assert.IsNotNull(restoredTimeline);
                Assert.AreSame(restoredAction, restoredProfile.FindAction(1001));
                Assert.AreSame(restoredTimeline, restoredProfile.GetActionTimeline(restoredAction));
                Assert.AreSame(
                    animation,
                    CombatEditorUtility.GetPrimaryAnimationClip(restoredTimeline),
                    "Redo 必须恢复创建时写入的表现动画。");
            }
            finally
            {
                Undo.ClearAll();
                if (!string.IsNullOrEmpty(actionPath))
                {
                    AssetDatabase.DeleteAsset(actionPath);
                }
                if (!string.IsNullOrEmpty(timelinePath))
                {
                    AssetDatabase.DeleteAsset(timelinePath);
                }
                AssetDatabase.DeleteAsset(testFolder);
                AssetDatabase.DeleteAsset($"Assets/Data/Res/Timeline/{key}");
                AssetDatabase.SaveAssets();
            }
        }

        [Test]
        public void ActionPresentationResolvesSeparatelyFromLogicAsset()
        {
            var action = CombatTestProfiles.CreateAction(
                1001,
                "attack",
                30);
            var profile = CombatTestProfiles.CreateProfile(action);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.SetFrameRate(profile.FrameRate);

            try
            {
                var presentation = CombatTestProfiles.AddActionPresentation(
                    profile,
                    action,
                    timeline);

                Assert.AreSame(action, presentation.Action);
                Assert.AreSame(timeline, presentation.Timeline);
                Assert.AreSame(timeline, profile.GetActionTimeline(action.ActionId));
                Assert.IsNull(profile.GetActionTimeline(9999));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(action);
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void ActionPresentationResolvesByAssetIdentityForEditor()
        {
            var first = CombatTestProfiles.CreateAction(1001, "first", 20);
            var second = CombatTestProfiles.CreateAction(1002, "second", 40);
            var profile = CombatTestProfiles.CreateProfile(first, second);
            var firstTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var secondTimeline = ScriptableObject.CreateInstance<TimelineAsset>();

            try
            {
                CombatTestProfiles.AddActionPresentation(profile, first, firstTimeline);
                CombatTestProfiles.AddActionPresentation(profile, second, secondTimeline);

                var secondSerialized = new SerializedObject(second);
                secondSerialized.FindProperty("actionId").intValue = first.ActionId;
                secondSerialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.AreSame(firstTimeline, profile.GetActionTimeline(first.ActionId));
                Assert.AreSame(firstTimeline, profile.GetActionTimeline(first));
                Assert.AreSame(
                    secondTimeline,
                    profile.GetActionTimeline(second),
                    "编辑器按资产引用定位表现映射时，不应被临时重复的 ActionId 串线。");
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(firstTimeline);
                Object.DestroyImmediate(secondTimeline);
            }
        }

        [Test]
        public void SettingActionTimelineOnlyChangesProfilePresentationMapping()
        {
            var action = CombatTestProfiles.CreateAction(1001, "attack", 30);
            var profile = CombatTestProfiles.CreateProfile(action);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var logicBefore = EditorJsonUtility.ToJson(action);

            try
            {
                Assert.IsTrue(CombatEditorUtility.TrySetActionTimeline(
                    profile,
                    action,
                    timeline,
                    out var error), error);

                Assert.AreEqual(logicBefore, EditorJsonUtility.ToJson(action));
                Assert.AreSame(action, profile.GetActionPresentation(action).Action);
                Assert.AreSame(timeline, profile.GetActionTimeline(action));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(action);
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void LogicAndPresentationDurationMismatchOnlyWarns()
        {
            var action = CombatTestProfiles.CreateAction(1001, "attack", 30);
            var profile = CombatTestProfiles.CreateProfile(action);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.SetFrameRate(profile.FrameRate);
            timeline.tracks.Add(new AnimationTrackAsset
            {
                trackName = "动画",
                clips =
                {
                    new AnimationClipAsset
                    {
                        StartFrame = 0,
                        EndFrame = 20,
                    },
                },
            });
            timeline.ValidateData();
            CombatTestProfiles.AddActionPresentation(profile, action, timeline);

            try
            {
                var issues = CombatEditorUtility.ValidateProfile(profile);
                Assert.AreEqual(30, action.DurationFrames, "校验不得用表现时长覆盖逻辑时长。");
                Assert.IsNotNull(issues.Find(value =>
                    value.Severity == CombatValidationSeverity.Warning &&
                    value.Message.Contains("逻辑时长与表现时长不一致")));
                Assert.IsNull(issues.Find(value =>
                    value.Severity == CombatValidationSeverity.Error &&
                    value.Message.Contains("逻辑时长与表现时长不一致")));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(action);
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void ProfileValidationReportsInvalidHitWindowRange()
        {
            var action = CombatTestProfiles.CreateAction(1001, "attack", 3);
            var serialized = new SerializedObject(action);
            var windows = serialized.FindProperty("hitWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            window.FindPropertyRelative("StartFrame").intValue = 2;
            window.FindPropertyRelative("EndFrame").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            action.ValidateData();
            var profile = CombatTestProfiles.CreateProfile(action);

            try
            {
                var issues = CombatEditorUtility.ValidateProfile(profile);
                Assert.IsNotNull(issues.Find(value =>
                    value.Severity == CombatValidationSeverity.Error &&
                    value.Message.Contains("命中窗口终点超出逻辑时长")));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(action);
            }
        }

        [Test]
        public void SettingActionTimelineRejectsForeignLogicAsset()
        {
            var owned = CombatTestProfiles.CreateAction(1001, "owned", 30);
            var foreign = CombatTestProfiles.CreateAction(1002, "foreign", 30);
            var profile = CombatTestProfiles.CreateProfile(owned);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            try
            {
                Assert.IsFalse(CombatEditorUtility.TrySetActionTimeline(
                    profile,
                    foreign,
                    timeline,
                    out var error));
                StringAssert.Contains("不属于 Profile", error);
                Assert.IsNull(profile.GetActionPresentation(foreign));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(owned);
                Object.DestroyImmediate(foreign);
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void ZeroLogicDurationDoesNotFallbackToPresentationTimeline()
        {
            var action = CombatTestProfiles.CreateAction(
                1001,
                "attack",
                0);
            var profile = CombatTestProfiles.CreateProfile(action);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.SetFrameRate(profile.FrameRate);
            CombatTestProfiles.AddActionPresentation(profile, action, timeline);

            try
            {
                Assert.AreEqual(0, action.DurationFrames);
                Assert.Throws<System.InvalidOperationException>(() => profile.ValidateRuntime());
                var runner = new CombatActionRunner();
                Assert.Throws<System.InvalidOperationException>(() => runner.Initialize(profile, 0));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(action);
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void MappableStateIdsExcludeOnlyStructuralNonStates()
        {
            // 状态映射候选必须来自枚举反射并排除结构性错误项；
            // 新增枚举状态会自动出现在 GetMappableStateIds 结果里，无需再同步手写列表。
            var locomotion = CombatStateId.GetMappableStateIds(StateLayer.Locomotion);
            var control = CombatStateId.GetMappableStateIds(StateLayer.Control);
            var life = CombatStateId.GetMappableStateIds(StateLayer.Life);

            Assert.Contains((int)LocomotionState.Idle, locomotion);
            Assert.Contains((int)LocomotionState.Move, locomotion);
            Assert.Contains((int)LocomotionState.Airborne, locomotion);

            Assert.Contains((int)ControlState.Stunned, control);
            Assert.Contains((int)ControlState.Knockback, control);
            Assert.Contains((int)ControlState.Frozen, control);
            CollectionAssert.DoesNotContain(control, (int)ControlState.Normal);

            Assert.Contains((int)LifeState.Dead, life);
            CollectionAssert.DoesNotContain(life, (int)LifeState.Alive);

            // Action 层整层禁止作为状态表现映射目标。
            Assert.IsEmpty(CombatStateId.GetMappableStateIds(StateLayer.Action));

            // IsStatePresentationMappable 与反射集合保持一致。
            Assert.IsTrue(CombatStateId.IsStatePresentationMappable(
                StateLayer.Locomotion, (int)LocomotionState.Move));
            Assert.IsFalse(CombatStateId.IsStatePresentationMappable(
                StateLayer.Action, (int)ActionState.Free));
            Assert.IsFalse(CombatStateId.IsStatePresentationMappable(
                StateLayer.Action, (int)ActionState.Executing));
            Assert.IsFalse(CombatStateId.IsStatePresentationMappable(
                StateLayer.Control, (int)ControlState.Normal));
            Assert.IsFalse(CombatStateId.IsStatePresentationMappable(
                StateLayer.Life, (int)LifeState.Alive));
        }

        [TestCase(StateLayer.Control, (int)ControlState.Normal)]
        [TestCase(StateLayer.Life, (int)LifeState.Alive)]
        public void StructuralStatesFailPresentationValidation(StateLayer layer, int stateId)
        {
            var profile = CombatTestProfiles.CreateProfile();
            try
            {
                CombatTestProfiles.AddStatePresentation(
                    profile,
                    layer,
                    stateId,
                    CombatStatePresentation.DefaultVariantId,
                    null);

                Assert.Throws<System.InvalidOperationException>(() => profile.ValidateRuntime());
                var issues = CombatEditorUtility.ValidateProfile(profile);
                Assert.IsNotNull(issues.Find(value =>
                    value.Severity == CombatValidationSeverity.Error &&
                    value.Message.Contains("不是允许配置表现映射")));
            }
            finally
            {
                Object.DestroyImmediate(profile);
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
