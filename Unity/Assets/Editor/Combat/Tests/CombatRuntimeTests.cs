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
                Assert.AreEqual(LocomotionState.Move, controller.States.Locomotion);
                Assert.AreEqual(0, controller.States.GetStateFrame(StateLayer.Locomotion));

                controller.Tick(2, Vector2.zero, empty);
                Assert.AreEqual(LocomotionState.Idle, controller.States.Locomotion);
                Assert.AreEqual(0, controller.States.GetStateFrame(StateLayer.Locomotion));
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
            var attack = CreateAction(1001, "attack", CombatCommandType.Attack, 30, 10);
            var profile = CreateProfile(attack);
            try
            {
                var controller = new CombatController();
                controller.Initialize(profile, 1, 0);
                var commands = new CombatFrameCommands(new[]
                {
                    new CombatCommand(77, 5, CombatCommandType.Attack),
                });

                controller.Tick(5, Vector2.zero, commands);
                Assert.AreEqual(ActionState.Executing, controller.States.Action);
                Assert.AreEqual(0, controller.States.GetStateFrame(StateLayer.Action));
                Assert.IsTrue(controller.Actions.HasAction);
                Assert.AreEqual(1001, controller.Actions.Current.ActionId);
                Assert.AreEqual(0, controller.Actions.Current.ActionFrame);
                Assert.AreEqual(77, controller.Actions.Current.RequestId);

                controller.Tick(6, Vector2.zero, CombatFrameCommands.Empty);
                Assert.AreEqual(1, controller.Actions.Current.ActionFrame);
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
            var firstAttack = CreateAction(1001, "first-attack", CombatCommandType.Attack, 30, 0);
            var secondAttack = CreateAction(1002, "second-attack", CombatCommandType.Attack, 30, 10);
            var profile = CreateProfile(firstAttack, secondAttack);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(9, 1, CombatCommandType.Attack, 1001),
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
            var attack = CreateAction(1001, "attack", CombatCommandType.Attack, 30, 10);
            var profile = CreateProfile(attack);
            try
            {
                var runner = new CombatActionRunner();
                runner.Initialize(profile, 0);
                runner.Tick(
                    1,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(9, 1, CombatCommandType.Attack, 9999),
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
        public void CancelWindowUsesInclusiveConfiguredFrameRange()
        {
            var window = new ActionCancelWindow
            {
                StartFrame = 2,
                EndFrame = 4,
                AcceptedCommand = CombatCommandType.Attack,
                TargetActionId = 1002,
            };

            Assert.IsFalse(window.IsOpen(1, CombatCommandType.Attack, false));
            Assert.IsTrue(window.IsOpen(2, CombatCommandType.Attack, false));
            Assert.IsTrue(window.IsOpen(4, CombatCommandType.Attack, false));
            Assert.IsFalse(window.IsOpen(5, CombatCommandType.Attack, false));
            Assert.IsFalse(window.IsOpen(3, CombatCommandType.Skill01, false));

            window.RequiresHitConfirm = true;
            Assert.IsFalse(window.IsOpen(3, CombatCommandType.Attack, false));
            Assert.IsTrue(window.IsOpen(3, CombatCommandType.Attack, true));
        }

        [Test]
        public void CancelCommandSelectsTargetDuringOpenWindow()
        {
            var attack = CreateAction(1001, "attack", CombatCommandType.Attack, 10, 10);
            var followUp = CreateAction(1002, "follow-up", CombatCommandType.Attack, 10, 10);
            var profile = CreateProfile(attack, followUp);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("cancelWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            window.FindPropertyRelative("StartFrame").intValue = 2;
            window.FindPropertyRelative("EndFrame").intValue = 3;
            window.FindPropertyRelative("AcceptedCommand").enumValueIndex = (int)CombatCommandType.Attack;
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
                        new CombatCommand(1, 1, CombatCommandType.Attack),
                    }),
                    true);
                runner.Tick(2, CombatFrameCommands.Empty, true);
                Assert.AreEqual(1, runner.Current.ActionFrame);

                runner.Tick(
                    3,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(2, 3, CombatCommandType.Attack),
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
        public void ExplicitCancelRequestOnlySelectsMatchingTargetAction()
        {
            var attack = CreateAction(1001, "attack", CombatCommandType.Attack, 10, 10);
            var firstFollowUp = CreateAction(1002, "follow-up-1", CombatCommandType.Attack, 10, 10);
            var secondFollowUp = CreateAction(1003, "follow-up-2", CombatCommandType.Attack, 10, 10);
            var profile = CreateProfile(attack, firstFollowUp, secondFollowUp);
            var serialized = new SerializedObject(attack);
            var windows = serialized.FindProperty("cancelWindows");
            windows.arraySize = 2;
            for (var i = 0; i < windows.arraySize; i++)
            {
                var window = windows.GetArrayElementAtIndex(i);
                window.FindPropertyRelative("StartFrame").intValue = 1;
                window.FindPropertyRelative("EndFrame").intValue = 3;
                window.FindPropertyRelative("AcceptedCommand").enumValueIndex = (int)CombatCommandType.Attack;
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
                        new CombatCommand(1, 1, CombatCommandType.Attack, attack.ActionId),
                    }),
                    true);
                runner.Tick(2, CombatFrameCommands.Empty, true);
                runner.Tick(
                    3,
                    new CombatFrameCommands(new[]
                    {
                        new CombatCommand(2, 3, CombatCommandType.Attack, secondFollowUp.ActionId),
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
        public void ActionCompletesAfterConfiguredFrameCount()
        {
            var attack = CreateAction(1001, "attack", CombatCommandType.Attack, 3, 10);
            var profile = CreateProfile(attack);
            try
            {
                var controller = new CombatController();
                controller.Initialize(profile, 1, 0);
                var commands = new CombatFrameCommands(new[]
                {
                    new CombatCommand(1, 1, CombatCommandType.Attack),
                });

                controller.Tick(1, Vector2.zero, commands); // frame 0
                controller.Tick(2, Vector2.zero, CombatFrameCommands.Empty); // frame 1
                controller.Tick(3, Vector2.zero, CombatFrameCommands.Empty); // frame 2
                Assert.IsTrue(controller.Actions.HasAction);

                controller.Tick(4, Vector2.zero, CombatFrameCommands.Empty);
                Assert.IsFalse(controller.Actions.HasAction);
                Assert.AreEqual(ActionState.Free, controller.States.Action);
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
            var attack = CreateAction(1001, "attack", CombatCommandType.Attack, 30, 10);
            var profile = CreateProfile(attack);
            try
            {
                var controller = new CombatController();
                controller.Initialize(profile, 1, 0);
                var commands = new CombatFrameCommands(new[]
                {
                    new CombatCommand(1, 1, CombatCommandType.Attack),
                });
                controller.Tick(1, Vector2.zero, commands);
                controller.Tick(2, Vector2.zero, CombatFrameCommands.Empty);
                var snapshot = controller.CaptureSnapshot();

                controller.SetControl(ControlState.Stunned);
                Assert.IsFalse(controller.Actions.HasAction);

                controller.RestoreSnapshot(snapshot);
                Assert.AreEqual(ControlState.Normal, controller.States.Control);
                Assert.AreEqual(ActionState.Executing, controller.States.Action);
                Assert.AreEqual(1, controller.Actions.Current.ActionFrame);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void HigherPriorityActionWinsDeterministically()
        {
            var low = CreateAction(1001, "low", CombatCommandType.Attack, 30, 1);
            var high = CreateAction(1002, "high", CombatCommandType.Attack, 30, 100);
            var profile = CreateProfile(low, high);
            try
            {
                var controller = new CombatController();
                controller.Initialize(profile, 1, 0);
                var commands = new CombatFrameCommands(new[]
                {
                    new CombatCommand(1, 1, CombatCommandType.Attack),
                });

                controller.Tick(1, Vector2.zero, commands);
                Assert.AreEqual(1002, controller.Actions.Current.ActionId);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(low);
                UnityEngine.Object.DestroyImmediate(high);
            }
        }

        private static CharacterCombatProfile CreateProfile(params CombatActionAsset[] actions)
        {
            return CombatTestProfiles.CreateProfile(actions);
        }

        private static CombatActionAsset CreateAction(
            int actionId,
            string stableId,
            CombatCommandType command,
            int durationFrames,
            int priority)
        {
            return CombatTestProfiles.CreateAction(actionId, stableId, command, durationFrames, priority);
        }
    }
}
