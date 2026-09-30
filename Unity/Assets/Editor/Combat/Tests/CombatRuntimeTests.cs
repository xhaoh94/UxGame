using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Combat.Tests
{
    public sealed class CombatRuntimeTests
    {
        [Test]
        public void CodeRulesDriveLocomotionWithoutTransitionAsset()
        {
            var profile = CreateProfile();
            try
            {
                var controller = new CombatController();
                controller.Initialize(profile, 1, 0);
                var empty = CombatFrameCommands.Empty;

                controller.Tick(1, Vector2.up, empty);
                Assert.AreEqual(LocomotionState.Move, controller.StateMachine.Locomotion);
                Assert.AreEqual(0, controller.StateMachine.GetStateFrame(StateLayer.Locomotion));

                controller.Tick(2, Vector2.zero, empty);
                Assert.AreEqual(LocomotionState.Idle, controller.StateMachine.Locomotion);
                Assert.AreEqual(0, controller.StateMachine.GetStateFrame(StateLayer.Locomotion));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void StatePresentationResolvesExplicitVariantAndDefaultFallback()
        {
            var profile = CreateProfile();
            var defaultTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var swordTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            defaultTimeline.SetFrameRate(profile.FrameRate);
            swordTimeline.SetFrameRate(profile.FrameRate);
            CombatTestProfiles.AddStatePresentation(
                profile,
                StateLayer.Locomotion,
                (int)LocomotionState.Idle,
                CombatStatePresentation.DefaultVariantId,
                defaultTimeline);
            CombatTestProfiles.AddStatePresentation(
                profile,
                StateLayer.Locomotion,
                (int)LocomotionState.Idle,
                "weapon_sword",
                swordTimeline);

            try
            {
                profile.ValidateRuntime();
                Assert.AreSame(
                    defaultTimeline,
                    profile.GetStateTimeline(
                        StateLayer.Locomotion,
                        (int)LocomotionState.Idle));
                Assert.AreSame(
                    swordTimeline,
                    profile.GetStateTimeline(
                        StateLayer.Locomotion,
                        (int)LocomotionState.Idle,
                        "weapon_sword"));
                Assert.AreSame(
                    defaultTimeline,
                    profile.GetStateTimeline(
                        StateLayer.Locomotion,
                        (int)LocomotionState.Idle,
                        "missing_variant"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(defaultTimeline);
                UnityEngine.Object.DestroyImmediate(swordTimeline);
            }
        }

        [Test]
        public void StatePresentationFallbackUsesHighestPriorityDeterministically()
        {
            var profile = CreateProfile();
            var lowTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var highTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            lowTimeline.SetFrameRate(profile.FrameRate);
            highTimeline.SetFrameRate(profile.FrameRate);
            CombatTestProfiles.AddStatePresentation(
                profile,
                StateLayer.Locomotion,
                (int)LocomotionState.Move,
                "walk",
                lowTimeline,
                1,
                "move.walk");
            CombatTestProfiles.AddStatePresentation(
                profile,
                StateLayer.Locomotion,
                (int)LocomotionState.Move,
                "run",
                highTimeline,
                10,
                "move.run");

            try
            {
                Assert.AreSame(
                    highTimeline,
                    profile.GetStateTimeline(
                        StateLayer.Locomotion,
                        (int)LocomotionState.Move,
                        "missing_variant"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(lowTimeline);
                UnityEngine.Object.DestroyImmediate(highTimeline);
            }
        }

        [Test]
        public void DuplicateStatePresentationVariantFailsRuntimeValidation()
        {
            var profile = CreateProfile();
            CombatTestProfiles.AddStatePresentation(
                profile,
                StateLayer.Life,
                (int)LifeState.Dead,
                "armored",
                null,
                stableId: "dead.armored.first");
            CombatTestProfiles.AddStatePresentation(
                profile,
                StateLayer.Life,
                (int)LifeState.Dead,
                "armored",
                null,
                stableId: "dead.armored.second");

            try
            {
                Assert.Throws<System.InvalidOperationException>(() => profile.ValidateRuntime());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void CommandStartsIndependentActionAtFrameZero()
        {
            var attack = CreateAction(1001, "attack", 30);
            var profile = CreateProfile(attack);
            try
            {
                var controller = new CombatController();
                controller.Initialize(profile, 1, 0);
                var commands = new CombatFrameCommands(new[]
                {
                    new CombatCommand(77, 5, attack.ActionId),
                });

                controller.Tick(5, Vector2.zero, commands);
                Assert.IsTrue(controller.ActionRunner.HasAction);
                Assert.AreEqual(1001, controller.ActionRunner.Current.ActionId);
                Assert.AreEqual(0, controller.ActionRunner.Current.ActionFrame);
                Assert.AreEqual(77, controller.ActionRunner.Current.RequestId);

                controller.Tick(6, Vector2.zero, CombatFrameCommands.Empty);
                Assert.AreEqual(1, controller.ActionRunner.Current.ActionFrame);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ExplicitActionRequestSelectsMatchingAction()
        {
            var firstAttack = CreateAction(1001, "first-attack", 30);
            var secondAttack = CreateAction(1002, "second-attack", 30);
            var profile = CreateProfile(firstAttack, secondAttack);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(9, 1, firstAttack.ActionId),
                    }),
                    true);

                Assert.IsTrue(runner.HasAction);
                Assert.AreEqual(1001, runner.Current.ActionId);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(firstAttack);
                UnityEngine.Object.DestroyImmediate(secondAttack);
            }
        }

        [Test]
        public void UnknownExplicitActionRequestDoesNotStartAnotherAction()
        {
            var attack = CreateAction(1001, "attack", 30);
            var profile = CreateProfile(attack);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(9, 1, 9999),
                    }),
                    true);

                Assert.IsFalse(runner.HasAction);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void CancelWindowUsesHalfOpenConfiguredFrameRange()
        {
            var window = new ActionCancelWindow
            {
                StartFrame = 2,
                EndFrame = 4,
                TargetActionId = 1002,
            };

            Assert.IsFalse(window.IsOpen(1, false));
            Assert.IsTrue(window.IsOpen(2, false));
            Assert.IsTrue(window.IsOpen(3, false));
            Assert.IsFalse(window.IsOpen(4, false));

            window.RequiresHitConfirm = true;
            Assert.IsFalse(window.IsOpen(3, false));
            Assert.IsTrue(window.IsOpen(3, true));
        }

        [Test]
        public void CancelWindowEndCannotExceedActionDuration()
        {
            var attack = CreateAction(1001, "attack", 3);
            var followUp = CreateAction(1002, "follow-up", 3);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("cancelWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            window.FindPropertyRelative("StartFrame").intValue = 2;
            window.FindPropertyRelative("EndFrame").intValue = 4;
            window.FindPropertyRelative("TargetActionId").intValue = followUp.ActionId;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var profile = CreateProfile(attack, followUp);
            try
            {
                Assert.Throws<System.InvalidOperationException>(() => profile.ValidateRuntime());
                var runner = new CombatActionRunner();
                Assert.Throws<System.InvalidOperationException>(() => runner.Initialize(profile, 0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
                UnityEngine.Object.DestroyImmediate(followUp);
            }
        }

        [Test]
        public void CancelCommandSelectsTargetDuringOpenWindow()
        {
            var attack = CreateAction(1001, "attack", 10);
            var followUp = CreateAction(1002, "follow-up", 10);
            var profile = CreateProfile(attack, followUp);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("cancelWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            window.FindPropertyRelative("StartFrame").intValue = 2;
            window.FindPropertyRelative("EndFrame").intValue = 3;
            window.FindPropertyRelative("TargetActionId").intValue = followUp.ActionId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();

            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(1, 1, attack.ActionId),
                    }),
                    true);
                runner.Tick(2, CombatFrameCommands.Empty, true);
                Assert.AreEqual(1, runner.Current.ActionFrame);

                runner.Tick(
                    3,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(2, 3, followUp.ActionId),
                    }),
                    true);
                Assert.AreEqual(followUp.ActionId, runner.Current.ActionId);
                Assert.AreEqual(0, runner.Current.ActionFrame);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
                UnityEngine.Object.DestroyImmediate(followUp);
            }
        }

        [Test]
        public void CancelCommandAtEndFrameDoesNotCancel()
        {
            var attack = CreateAction(1001, "attack", 10);
            var followUp = CreateAction(1002, "follow-up", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("cancelWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            window.FindPropertyRelative("StartFrame").intValue = 1;
            window.FindPropertyRelative("EndFrame").intValue = 2;
            window.FindPropertyRelative("TargetActionId").intValue = followUp.ActionId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();

            var profile = CreateProfile(attack, followUp);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(1, 1, attack.ActionId),
                    }),
                    true);
                runner.Tick(2, CombatFrameCommands.Empty, true);
                runner.Tick(
                    3,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(2, 3, followUp.ActionId),
                    }),
                    true);

                Assert.AreEqual(attack.ActionId, runner.Current.ActionId);
                Assert.AreEqual(2, runner.Current.ActionFrame);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
                UnityEngine.Object.DestroyImmediate(followUp);
            }
        }

        [Test]
        public void UnknownCancelActionIdDoesNotInterruptCurrentAction()
        {
            var attack = CreateAction(1001, "attack", 10);
            var followUp = CreateAction(1002, "follow-up", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("cancelWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            window.FindPropertyRelative("StartFrame").intValue = 1;
            window.FindPropertyRelative("EndFrame").intValue = 4;
            window.FindPropertyRelative("TargetActionId").intValue = followUp.ActionId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();

            var profile = CreateProfile(attack, followUp);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(1, 1, attack.ActionId),
                    }),
                    true);
                runner.Tick(
                    2,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(2, 2, 9999),
                    }),
                    true);

                Assert.AreEqual(attack.ActionId, runner.Current.ActionId);
                Assert.AreEqual(1, runner.Current.ActionFrame);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
                UnityEngine.Object.DestroyImmediate(followUp);
            }
        }

        [Test]
        public void HitConfirmedCancelUsesCurrentActionInstanceOnly()
        {
            var attack = CreateAction(1001, "attack", 10);
            var followUp = CreateAction(1002, "follow-up", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("cancelWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            window.FindPropertyRelative("StartFrame").intValue = 1;
            window.FindPropertyRelative("EndFrame").intValue = 4;
            window.FindPropertyRelative("TargetActionId").intValue = followUp.ActionId;
            window.FindPropertyRelative("RequiresHitConfirm").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();

            var profile = CreateProfile(attack, followUp);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(1, 1, attack.ActionId),
                    }),
                    true);
                var attackInstanceId = runner.Current.InstanceId;

                runner.Tick(
                    2,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(2, 2, followUp.ActionId),
                    }),
                    true);
                Assert.AreEqual(attack.ActionId, runner.Current.ActionId);
                Assert.IsFalse(runner.MarkHitConfirmed(attackInstanceId + 1));
                Assert.IsTrue(runner.MarkHitConfirmed(attackInstanceId));

                runner.Tick(
                    3,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(3, 3, followUp.ActionId),
                    }),
                    true);
                Assert.AreEqual(followUp.ActionId, runner.Current.ActionId);
                Assert.IsFalse(runner.MarkHitConfirmed(attackInstanceId));
                Assert.IsFalse(runner.Current.HasHitConfirmed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
                UnityEngine.Object.DestroyImmediate(followUp);
            }
        }

        [Test]
        public void ExplicitCancelRequestOnlySelectsMatchingTargetAction()
        {
            var attack = CreateAction(1001, "attack", 10);
            var firstFollowUp = CreateAction(1002, "follow-up-1", 10);
            var secondFollowUp = CreateAction(1003, "follow-up-2", 10);
            var profile = CreateProfile(attack, firstFollowUp, secondFollowUp);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("cancelWindows");
            windows.arraySize = 2;
            for (var i = 0; i < windows.arraySize; i++)
            {
                var window = windows.GetArrayElementAtIndex(i);
                window.FindPropertyRelative("StartFrame").intValue = 1;
                window.FindPropertyRelative("EndFrame").intValue = 3;
                window.FindPropertyRelative("TargetActionId").intValue = i == 0
                    ? firstFollowUp.ActionId
                    : secondFollowUp.ActionId;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();

            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(1, 1, attack.ActionId),
                    }),
                    true);
                runner.Tick(2, CombatFrameCommands.Empty, true);
                runner.Tick(
                    3,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(2, 3, secondFollowUp.ActionId),
                    }),
                    true);

                Assert.AreEqual(secondFollowUp.ActionId, runner.Current.ActionId);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
                UnityEngine.Object.DestroyImmediate(firstFollowUp);
                UnityEngine.Object.DestroyImmediate(secondFollowUp);
            }
        }

        [Test]
        public void ResolveComboTargetFallsBackToChainRoot()
        {
            var chain = CreateComboChain();
            var profile = CreateProfile(chain);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);

                Assert.AreEqual(1001, runner.ResolveComboTarget(1001),
                    "没有动作在跑时必须回落到链头，输入层不用自己判断有没有动作。");

                runner.Tick(1, Frame(1, 1001), true);
                Assert.AreEqual(1001, runner.ResolveComboTarget(1001),
                    "动作在跑但窗口没打开时同样回落链头。");
            }
            finally
            {
                DestroyChain(profile, chain);
            }
        }

        [Test]
        public void ResolveComboTargetReturnsWindowTargetOnlyWhileOpen()
        {
            var chain = CreateComboChain();
            var profile = CreateProfile(chain);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(1, Frame(1, 1001), true);

                // 取消窗口 [4,12)：ActionFrame 3 还没开，4 才开
                runner.Tick(4, CombatFrameCommands.Empty, true);
                Assert.AreEqual(3, runner.Current.ActionFrame);
                Assert.AreEqual(1001, runner.ResolveComboTarget(1001));

                runner.Tick(5, CombatFrameCommands.Empty, true);
                Assert.AreEqual(4, runner.Current.ActionFrame);
                Assert.AreEqual(1002, runner.ResolveComboTarget(1001));

                // 半开区间：ActionFrame 12 已关闭
                runner.Tick(13, CombatFrameCommands.Empty, true);
                Assert.AreEqual(12, runner.Current.ActionFrame);
                Assert.AreEqual(1001, runner.ResolveComboTarget(1001));
            }
            finally
            {
                DestroyChain(profile, chain);
            }
        }

        [Test]
        public void ResolveComboTargetHonoursHitConfirmRequirement()
        {
            var attack = CreateAction(1001, "attack", 20);
            var followUp = CreateAction(1002, "follow-up", 20);
            ConfigureLinkWindow(attack, 2, 8, followUp.ActionId, requiresHitConfirm: true);
            var profile = CreateProfile(attack, followUp);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(1, Frame(1, attack.ActionId), true);

                runner.Tick(3, CombatFrameCommands.Empty, true);
                Assert.AreEqual(2, runner.Current.ActionFrame);
                Assert.AreEqual(attack.ActionId, runner.ResolveComboTarget(attack.ActionId),
                    "需要命中确认的窗口在确认之前不能成为连招目标。");

                Assert.IsTrue(runner.MarkHitConfirmed(runner.Current.InstanceId));
                Assert.AreEqual(followUp.ActionId, runner.ResolveComboTarget(attack.ActionId));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
                UnityEngine.Object.DestroyImmediate(followUp);
            }
        }

        [Test]
        public void ResolveComboTargetIgnoresCancelWindows()
        {
            var attack = CreateAction(1001, "attack", 20);
            var dodge = CreateAction(2001, "dodge", 20);
            ConfigureCancelWindow(attack, 2, 8, dodge.ActionId);
            var profile = CreateProfile(attack, dodge);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(1, Frame(1, attack.ActionId), true);
                runner.Tick(3, CombatFrameCommands.Empty, true);

                Assert.AreEqual(attack.ActionId, runner.ResolveComboTarget(attack.ActionId),
                    "泛化取消窗口不能泄漏成连招目标：连招只读 LinkWindows。");

                runner.Tick(4, Frame(4, dodge.ActionId), true);
                Assert.AreEqual(dodge.ActionId, runner.Current.ActionId,
                    "显式输入闪避技能时，仍然要能被取消窗口接受。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
                UnityEngine.Object.DestroyImmediate(dodge);
            }
        }

        [Test]
        public void ResolveComboTargetWalksFullThreeSegmentChain()
        {
            var chain = CreateComboChain();
            var profile = CreateProfile(chain);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);

                var starts = new List<int>();
                runner.ActionChanged += e =>
                {
                    if (e.EndReason == CombatActionEndReason.Started)
                    {
                        starts.Add(e.Current.ActionId);
                    }
                };

                runner.Tick(1, Frame(1, 1001), true);

                Assert.AreEqual(1002, QueryThenCancel(runner, 5, 1001));
                Assert.AreEqual(1003, QueryThenCancel(runner, 10, 1001));
                Assert.AreEqual(1001, QueryThenCancel(runner, 15, 1001));

                CollectionAssert.AreEqual(new[] { 1001, 1002, 1003, 1001 }, starts,
                    "三段打完必须回到链头，才能无限连；最后一跳说明链是环不是线。");
            }
            finally
            {
                DestroyChain(profile, chain);
            }
        }

        [Test]
        public void ComboQueryResultIsCancellableOnNextFrame()
        {
            var chain = CreateComboChain();
            var profile = CreateProfile(chain);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(1, Frame(1, 1001), true);
                runner.Tick(5, CombatFrameCommands.Empty, true);

                // 查表发生在窗口内、且不是窗口最后一帧 —— 下一帧消费时仍在窗口内
                var target = runner.ResolveComboTarget(1001);
                Assert.AreEqual(1002, target);
                runner.Tick(6, Frame(6, target), true);
                Assert.AreEqual(target, runner.Current.ActionId);
                Assert.AreEqual(0, runner.Current.ActionFrame);
            }
            finally
            {
                DestroyChain(profile, chain);
            }
        }

        [Test]
        public void ComboQueryOnLastWindowFrameFallsOutsideAfterOneFrameDelay()
        {
            var chain = CreateComboChain();
            var profile = CreateProfile(chain);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(1, Frame(1, 1001), true);

                // ActionFrame 11 是 [4,12) 的最后一帧：这一帧查表有目标，但命令下一帧才生效
                runner.Tick(12, CombatFrameCommands.Empty, true);
                Assert.AreEqual(11, runner.Current.ActionFrame);
                var target = runner.ResolveComboTarget(1001);
                Assert.AreEqual(1002, target);

                runner.Tick(13, Frame(13, target), true);
                Assert.AreEqual(1001, runner.Current.ActionId,
                    "窗口最后一帧按下会落到窗口外 —— 命令延迟一帧的固有代价，不是缺陷。");
            }
            finally
            {
                DestroyChain(profile, chain);
            }
        }

        [Test]
        public void HitWindowUsesHalfOpenConfiguredFrameRange()
        {
            var window = new ActionHitboxWindow
            {
                StartFrame = 2,
                EndFrame = 4,
            };

            Assert.IsFalse(window.IsActive(1));
            Assert.IsTrue(window.IsActive(2));
            Assert.IsTrue(window.IsActive(3));
            Assert.IsFalse(window.IsActive(4));
        }

        [Test]
        public void RunnerAppendsActiveHitboxWindowsInSerializedOrderWithoutSideEffects()
        {
            var attack = CreateAction(1001, "attack", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("hitboxWindows");
            windows.arraySize = 3;
            SetWindowRange(windows.GetArrayElementAtIndex(0), 1, 4);
            SetWindowRange(windows.GetArrayElementAtIndex(1), 2, 3);
            SetWindowRange(windows.GetArrayElementAtIndex(2), 4, 5);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();
            var profile = CreateProfile(attack);

            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(1, 1, attack.ActionId),
                    }),
                    true);
                runner.Tick(2, CombatFrameCommands.Empty, true); // ActionFrame 1

                var output = new List<CombatActiveHitboxWindow>();
                Assert.AreEqual(1, runner.AppendActiveHitboxWindows(output));
                Assert.AreEqual(0, output[0].WindowIndex);
                Assert.AreEqual(runner.Current.InstanceId, output[0].ActionInstanceId);
                Assert.AreEqual(1, output[0].ActionFrame);

                runner.Tick(3, CombatFrameCommands.Empty, true); // ActionFrame 2
                output.Clear();
                Assert.AreEqual(2, runner.AppendActiveHitboxWindows(output));
                CollectionAssert.AreEqual(new[] { 0, 1 },
                    output.ConvertAll(item => item.WindowIndex));
                var firstResult = output[0];
                var firstWindow = attack.HitboxWindows[0];
                firstWindow.EndFrame = 9;
                Assert.AreEqual(4, firstResult.EndFrame,
                    "查询结果必须复制帧值，不能持有会随权威资产变化的可变窗口引用。");
                firstWindow.EndFrame = 4;
                Assert.AreEqual(2, runner.AppendActiveHitboxWindows(output),
                    "重复查询必须无状态，并只向调用方集合追加结果。");
                CollectionAssert.AreEqual(new[] { 0, 1, 0, 1 },
                    output.ConvertAll(item => item.WindowIndex));

                runner.Tick(5, CombatFrameCommands.Empty, true); // ActionFrame 4
                output.Clear();
                Assert.AreEqual(1, runner.AppendActiveHitboxWindows(output));
                Assert.AreEqual(2, output[0].WindowIndex,
                    "EndFrame 本身不属于前两条攻击判定。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void SnapshotRestoreReconstructsActiveHitboxWindowsWithoutExtraState()
        {
            var attack = CreateAction(1001, "attack", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("hitboxWindows");
            windows.arraySize = 1;
            SetWindowRange(windows.GetArrayElementAtIndex(0), 1, 3);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();
            var profile = CreateProfile(attack);

            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(1, 1, attack.ActionId),
                    }),
                    true);
                runner.Tick(2, CombatFrameCommands.Empty, true);
                var snapshot = runner.Current;
                var before = new List<CombatActiveHitboxWindow>();
                Assert.AreEqual(1, runner.AppendActiveHitboxWindows(before));

                runner.Tick(5, CombatFrameCommands.Empty, true);
                Assert.AreEqual(0, runner.AppendActiveHitboxWindows(new List<CombatActiveHitboxWindow>()));
                runner.Restore(snapshot, true, 2);

                var restored = new List<CombatActiveHitboxWindow>();
                Assert.AreEqual(1, runner.AppendActiveHitboxWindows(restored));
                Assert.AreEqual(before[0].WindowId, restored[0].WindowId);
                Assert.AreEqual(before[0].ActionFrame, restored[0].ActionFrame);
                Assert.AreEqual(before[0].ActionInstanceId, restored[0].ActionInstanceId);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void HitResolverUsesFixedCircleAndTargetIdOrder()
        {
            var attack = CreateAction(1001, "attack", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("hitboxWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            SetWindowRange(window, 0, 4);
            window.FindPropertyRelative("radiusMillimeters").intValue = 1000;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();
            var profile = CreateProfile(attack);

            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                Assert.IsTrue(runner.StartAction(attack.ActionId, 1, 0, false));

                var targets = new List<CombatHitTarget>
                {
                    new CombatHitTarget(30, new CombatFixedPoint(1001, 0)),
                    new CombatHitTarget(20, new CombatFixedPoint(500, 0)),
                    new CombatHitTarget(10, new CombatFixedPoint(0, 1000)),
                    new CombatHitTarget(1, new CombatFixedPoint(0, 0)),
                };
                var hits = new List<CombatHitCandidate>();
                var added = CombatHitResolver.AppendResolvedHits(
                    runner,
                    new CombatHitQuerySource(1, new CombatFixedPoint(0, 0)),
                    targets,
                    hits);

                Assert.AreEqual(2, added);
                CollectionAssert.AreEqual(new long[] { 10, 20 },
                    hits.ConvertAll(hit => hit.TargetEntityId));
                Assert.AreEqual(0, CombatHitResolver.AppendResolvedHits(
                    runner,
                    new CombatHitQuerySource(1, new CombatFixedPoint(0, 0)),
                    targets,
                    hits));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void HitResolverExcludesSpawnerForProjectiles()
        {
            // 刀波在发射者脚下生成（距离 0），不豁免发射者会当场打中自己把自己耗死。
            var attack = CreateAction(1001, "attack", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("hitboxWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            SetWindowRange(window, 0, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();
            var profile = CreateProfile(attack);

            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                Assert.IsTrue(runner.StartAction(attack.ActionId, 1, 0, false));

                var targets = new List<CombatHitTarget>
                {
                    new CombatHitTarget(77, new CombatFixedPoint(0, 0)),
                    new CombatHitTarget(20, new CombatFixedPoint(500, 0)),
                };
                var hits = new List<CombatHitCandidate>();
                Assert.AreEqual(1, CombatHitResolver.AppendResolvedHits(
                    runner,
                    new CombatHitQuerySource(1, new CombatFixedPoint(0, 0), 77),
                    targets,
                    hits),
                    "发射者 77 必须被豁免，只剩 20 中招。");
                CollectionAssert.AreEqual(new long[] { 20 },
                    hits.ConvertAll(hit => hit.TargetEntityId));

                // 不带豁免时行为不变：发射者恢复为合法目标。
                hits.Clear();
                Assert.AreEqual(2, CombatHitResolver.AppendResolvedHits(
                    runner,
                    new CombatHitQuerySource(1, new CombatFixedPoint(0, 0)),
                    targets,
                    hits));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void PredictedHitDeduplicationMigratesWhenActionIsConfirmed()
        {
            var attack = CreateAction(1001, "attack", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("hitboxWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            SetWindowRange(window, 0, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();
            var profile = CreateProfile(attack);

            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                Assert.IsTrue(runner.StartAction(attack.ActionId, 7, 0, true));
                var source = new CombatHitQuerySource(1, new CombatFixedPoint(0, 0));
                var targets = new List<CombatHitTarget>
                {
                    new CombatHitTarget(2, new CombatFixedPoint(1, 0)),
                };
                var hits = new List<CombatHitCandidate>();
                Assert.AreEqual(1, CombatHitResolver.AppendResolvedHits(runner, source, targets, hits));
                var predictedInstanceId = hits[0].ActionInstanceId;
                var authoritativeInstanceId = predictedInstanceId + 1000;

                Assert.IsTrue(runner.Confirm(7, authoritativeInstanceId, 0));
                hits.Clear();
                Assert.AreEqual(0, CombatHitResolver.AppendResolvedHits(runner, source, targets, hits));
                var accepted = runner.CaptureAcceptedHits();
                Assert.AreEqual(1, accepted.Length);
                Assert.AreEqual(authoritativeInstanceId, accepted[0].ActionInstanceId);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void HitResolverRejectsInvalidEntityIds()
        {
            var attack = CreateAction(1001, "attack", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("hitboxWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            SetWindowRange(window, 0, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();
            var profile = CreateProfile(attack);

            try
            {
                var runner = new CombatActionRunner();
                var source = new CombatHitQuerySource(1, new CombatFixedPoint(0, 0));
                Assert.Throws<System.InvalidOperationException>(() =>
                    CombatHitResolver.AppendResolvedHits(
                        runner,
                        source,
                        new List<CombatHitTarget> { new CombatHitTarget(0, new CombatFixedPoint(0, 0)) },
                        new List<CombatHitCandidate>()),
                    "即使当前无动作，非法目标快照也必须立即失败。");
                runner.Initialize(profile, 0);
                runner.StartAction(attack.ActionId, 1, 0, false);
                Assert.Throws<System.InvalidOperationException>(() =>
                    CombatHitResolver.AppendResolvedHits(
                        runner,
                        source,
                        new List<CombatHitTarget> { new CombatHitTarget(0, new CombatFixedPoint(0, 0)) },
                        new List<CombatHitCandidate>()));
                Assert.Throws<System.InvalidOperationException>(() =>
                    CombatHitResolver.AppendResolvedHits(
                        runner,
                        new CombatHitQuerySource(0, new CombatFixedPoint(0, 0)),
                        new List<CombatHitTarget>(),
                        new List<CombatHitCandidate>()));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void HitResolverRejectsDuplicateTargetIds()
        {
            var attack = CreateAction(1001, "attack", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("hitboxWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            SetWindowRange(window, 0, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();
            var profile = CreateProfile(attack);

            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.StartAction(attack.ActionId, 1, 0, false);
                var targets = new List<CombatHitTarget>
                {
                    new CombatHitTarget(2, new CombatFixedPoint(0, 0)),
                    new CombatHitTarget(2, new CombatFixedPoint(100, 0)),
                };

                Assert.Throws<System.InvalidOperationException>(() =>
                    CombatHitResolver.AppendResolvedHits(
                        runner,
                        new CombatHitQuerySource(1, new CombatFixedPoint(0, 0)),
                        targets,
                        new List<CombatHitCandidate>()));
                runner.Tick(2, CombatFrameCommands.Empty, true);
                Assert.Throws<System.InvalidOperationException>(() =>
                    CombatHitResolver.AppendResolvedHits(
                        runner,
                        new CombatHitQuerySource(1, new CombatFixedPoint(0, 0)),
                        targets,
                        new List<CombatHitCandidate>()),
                    "即使当前帧没有激活窗口，重复目标 ID 也必须立即失败。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void HitDeduplicationSurvivesSnapshotRestoreAndResetsForNewActionInstance()
        {
            var attack = CreateAction(1001, "attack", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("hitboxWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            SetWindowRange(window, 0, 4);
            window.FindPropertyRelative("radiusMillimeters").intValue = 1000;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();
            var profile = CreateProfile(attack);

            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                Assert.IsTrue(runner.StartAction(attack.ActionId, 1, 0, false));
                var target = new List<CombatHitTarget>
                {
                    new CombatHitTarget(2, new CombatFixedPoint(100, 0)),
                };
                var source = new CombatHitQuerySource(1, new CombatFixedPoint(0, 0));
                var hits = new List<CombatHitCandidate>();
                Assert.AreEqual(1, CombatHitResolver.AppendResolvedHits(runner, source, target, hits));

                var accepted = runner.CaptureAcceptedHits();
                runner.Restore(runner.Current, true, 0, accepted);
                hits.Clear();
                Assert.AreEqual(0, CombatHitResolver.AppendResolvedHits(runner, source, target, hits),
                    "恢复快照后，同一动作实例的已命中目标必须继续去重。");

                Assert.IsTrue(runner.StartAction(attack.ActionId, 2, 0, false));
                hits.Clear();
                Assert.AreEqual(1, CombatHitResolver.AppendResolvedHits(runner, source, target, hits),
                    "新动作实例必须拥有独立的命中去重域。");
                Assert.AreNotEqual(accepted[0].ActionInstanceId, hits[0].ActionInstanceId);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void InvalidHitShapeParametersFailRuntimeValidation()
        {
            var attack = CreateAction(1001, "attack", 10);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("hitboxWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            SetWindowRange(window, 0, 2);
            window.FindPropertyRelative("shape").enumValueIndex = 0;
            window.FindPropertyRelative("radiusMillimeters").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var profile = CreateProfile(attack);

            try
            {
                Assert.Throws<System.InvalidOperationException>(() => profile.ValidateRuntime());
                Assert.Throws<System.InvalidOperationException>(() =>
                    new CombatActionRunner().Initialize(profile, 0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void SnapshotRestoreRestoresLocalActionSequenceExactly()
        {
            var attack = CreateAction(1001, "attack", 10);
            var profile = CreateProfile(attack);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                Assert.IsTrue(runner.StartAction(attack.ActionId, 1, 0, false));
                var snapshot = runner.Current;
                var snapshotSequence = runner.LocalSequence;

                Assert.IsTrue(runner.StartAction(attack.ActionId, 2, 0, false));
                var firstReplayInstanceId = runner.Current.InstanceId;
                Assert.Greater(runner.LocalSequence, snapshotSequence);

                Assert.Throws<System.InvalidOperationException>(() =>
                    runner.Restore(snapshot, true, 0, null, snapshot.InstanceId - 1));
                runner.Restore(snapshot, true, 0, null, snapshotSequence);
                Assert.AreEqual(snapshotSequence, runner.LocalSequence);
                Assert.IsTrue(runner.StartAction(attack.ActionId, 2, 0, false));
                Assert.AreEqual(firstReplayInstanceId, runner.Current.InstanceId,
                    "回滚后重放同一动作必须生成相同实例 ID。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void WindowValidationHandlesMaximumIntegerStartFrame()
        {
            var hitWindow = new ActionHitboxWindow
            {
                StartFrame = int.MaxValue,
                EndFrame = int.MaxValue,
            };
            var cancelWindow = new ActionCancelWindow
            {
                StartFrame = int.MaxValue,
                EndFrame = int.MaxValue,
                TargetActionId = 1,
            };

            hitWindow.ValidateData();
            cancelWindow.ValidateData();

            Assert.AreEqual(int.MaxValue - 1, hitWindow.StartFrame);
            Assert.AreEqual(int.MaxValue, hitWindow.EndFrame);
            Assert.AreEqual(int.MaxValue - 1, cancelWindow.StartFrame);
            Assert.AreEqual(int.MaxValue, cancelWindow.EndFrame);
        }

        [Test]
        public void InvalidHitWindowEndFailsRuntimeValidation()
        {
            var attack = CreateAction(1001, "attack", 3);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("hitboxWindows");
            windows.arraySize = 1;
            SetWindowRange(windows.GetArrayElementAtIndex(0), 2, 4);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack.ValidateData();
            var profile = CreateProfile(attack);

            try
            {
                Assert.Throws<System.InvalidOperationException>(() => profile.ValidateRuntime());
                var runner = new CombatActionRunner();
                Assert.Throws<System.InvalidOperationException>(() => runner.Initialize(profile, 0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ActionCompletesAfterConfiguredFrameCount()
        {
            var attack = CreateAction(1001, "attack", 3);
            var profile = CreateProfile(attack);
            try
            {
                var controller = new CombatController();
                controller.Initialize(profile, 1, 0);
                var commands = new CombatFrameCommands(new[]
                {
                    new CombatCommand(1, 1, attack.ActionId),
                });

                controller.Tick(1, Vector2.zero, commands); // frame 0
                controller.Tick(2, Vector2.zero, CombatFrameCommands.Empty); // frame 1
                controller.Tick(3, Vector2.zero, CombatFrameCommands.Empty); // frame 2
                Assert.IsTrue(controller.ActionRunner.HasAction);

                controller.Tick(4, Vector2.zero, CombatFrameCommands.Empty);
                Assert.IsFalse(controller.ActionRunner.HasAction);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void SnapshotRestoresMacroStateAndActionFrame()
        {
            var attack = CreateAction(1001, "attack", 30);
            var profile = CreateProfile(attack);
            try
            {
                var controller = new CombatController();
                controller.Initialize(profile, 1, 0);
                var commands = new CombatFrameCommands(new[]
                {
                    new CombatCommand(1, 1, attack.ActionId),
                });
                controller.Tick(1, Vector2.zero, commands);
                controller.Tick(2, Vector2.zero, CombatFrameCommands.Empty);
                var snapshot = controller.CaptureSnapshot();
                Assert.AreEqual(UnitCombatSnapshot.CurrentVersion, snapshot.Version);
                Assert.IsTrue(snapshot.IsGrounded);

                controller.SetGrounded(false);
                controller.SetControl(ControlState.Stunned);
                Assert.IsFalse(controller.ActionRunner.HasAction);

                controller.RestoreSnapshot(snapshot);
                Assert.AreEqual(ControlState.Normal, controller.StateMachine.Control);
                Assert.AreEqual(1, controller.ActionRunner.Current.ActionFrame);
                Assert.IsTrue(controller.IsGrounded);
                controller.Tick(3, Vector2.zero, CombatFrameCommands.Empty);
                Assert.AreEqual(LocomotionState.Idle, controller.StateMachine.Locomotion,
                    "恢复后的 grounded 隐藏状态必须保证下一帧继续走相同路径。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void SameFrameActionRequestsUseRequestIdOrder()
        {
            var first = CreateAction(1001, "first", 30);
            var second = CreateAction(1002, "second", 30);
            var profile = CreateProfile(first, second);
            try
            {
                var buffer = new CombatCommandBuffer();
                buffer.Enqueue(new CombatCommand(2, 1, second.ActionId));
                buffer.Enqueue(new CombatCommand(1, 1, first.ActionId));

                var controller = new CombatController();
                controller.Initialize(profile, 1, 0);
                controller.Tick(1, Vector2.zero, buffer.Consume(1));

                Assert.AreEqual(first.ActionId, controller.ActionRunner.Current.ActionId);
                Assert.AreEqual(1, controller.ActionRunner.Current.RequestId);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void CommandBufferUsesCompleteStableTieBreakers()
        {
            var buffer = new CombatCommandBuffer();
            buffer.Enqueue(new CombatCommand(2, 1, 1001));
            buffer.Enqueue(new CombatCommand(1, 1, 1002));
            buffer.Enqueue(new CombatCommand(1, 1, 1001, 2));
            buffer.Enqueue(new CombatCommand(1, 1, 1001, 1, new Vector3(2, 0, 0)));
            buffer.Enqueue(new CombatCommand(1, 1, 1001, 1, new Vector3(1, 2, 0)));
            buffer.Enqueue(new CombatCommand(1, 1, 1001, 1, new Vector3(1, 1, 2)));
            buffer.Enqueue(new CombatCommand(1, 1, 1001, 1, new Vector3(1, 1, 1)));

            var commands = buffer.Consume(1);
            Assert.AreEqual(7, commands.Count);
            Assert.AreEqual(new Vector3(1, 1, 1), commands[0].AimDirection);
            Assert.AreEqual(new Vector3(1, 1, 2), commands[1].AimDirection);
            Assert.AreEqual(new Vector3(1, 2, 0), commands[2].AimDirection);
            Assert.AreEqual(new Vector3(2, 0, 0), commands[3].AimDirection);
            Assert.AreEqual(2u, commands[4].TargetId);
            Assert.AreEqual(1002, commands[5].ActionId);
            Assert.AreEqual(2, commands[6].RequestId);
        }

        private static CombatFrameCommands Frame(int simulationFrame, int actionId)
        {
            return new CombatFrameCommands(new[]
            {
                new CombatCommand(simulationFrame, simulationFrame, actionId),
            });
        }

        /// <summary>
        /// 复刻输入层语义：在 frame 这一帧查表，把结果包成"下一帧生效"的命令，返回下一帧实际起手的动作号。
        /// 查表与消费跨帧，正是连招手感的关键。
        /// </summary>
        private static int QueryThenCancel(CombatActionRunner runner, int frame, int chainRoot)
        {
            runner.Tick(frame, CombatFrameCommands.Empty, true);
            var target = runner.ResolveComboTarget(chainRoot);
            runner.Tick(frame + 1, Frame(frame + 1, target), true);
            return runner.Current.ActionId;
        }

        /// <summary>三段环链：每段取消窗口 [4,12) 指向下一段，第三段指回第一段。</summary>
        private static CombatActionAsset[] CreateComboChain()
        {
            var first = CreateAction(1001, "attack01", 20);
            var second = CreateAction(1002, "attack02", 20);
            var third = CreateAction(1003, "attack03", 20);
            ConfigureLinkWindow(first, 4, 12, second.ActionId);
            ConfigureLinkWindow(second, 4, 12, third.ActionId);
            ConfigureLinkWindow(third, 4, 12, first.ActionId);
            return new[] { first, second, third };
        }

        private static void ConfigureLinkWindow(
            CombatActionAsset action, int startFrame, int endFrame, int targetActionId,
            bool requiresHitConfirm = false)
        {
            var serialized = new SerializedObject(action);
            var windows = serialized.FindProperty("linkWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            SetWindowRange(window, startFrame, endFrame);
            window.FindPropertyRelative("TargetActionId").intValue = targetActionId;
            window.FindPropertyRelative("RequiresHitConfirm").boolValue = requiresHitConfirm;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            action.ValidateData();
        }

        private static void ConfigureCancelWindow(
            CombatActionAsset action, int startFrame, int endFrame, int targetActionId,
            bool requiresHitConfirm = false)
        {
            var serialized = new SerializedObject(action);
            var windows = serialized.FindProperty("cancelWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            SetWindowRange(window, startFrame, endFrame);
            window.FindPropertyRelative("TargetActionId").intValue = targetActionId;
            window.FindPropertyRelative("RequiresHitConfirm").boolValue = requiresHitConfirm;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            action.ValidateData();
        }

        private static void DestroyChain(CharacterCombatProfile profile, CombatActionAsset[] chain)
        {
            UnityEngine.Object.DestroyImmediate(profile);
            foreach (var action in chain)
            {
                UnityEngine.Object.DestroyImmediate(action);
            }
        }

        private static CharacterCombatProfile CreateProfile(params CombatActionAsset[] actions)
        {
            return CombatTestProfiles.CreateProfile(actions);
        }

        private static void SetWindowRange(SerializedProperty window, int startFrame, int endFrame)
        {
            window.FindPropertyRelative("StartFrame").intValue = startFrame;
            window.FindPropertyRelative("EndFrame").intValue = endFrame;
        }

        private static CombatActionAsset CreateAction(int actionId, string stableId, int durationFrames)
        {
            return CombatTestProfiles.CreateAction(actionId, stableId, durationFrames);
        }
    }
}
