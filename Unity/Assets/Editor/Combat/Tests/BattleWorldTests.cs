using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Combat.Tests
{
    public sealed class BattleWorldTests
    {
        [Test]
        public void EntitiesTickInAscendingIdOrder()
        {
            var profile = CombatTestProfiles.CreateProfile();
            var order = new List<long>();
            try
            {
                var world = new BattleWorld("test", 30);
                // 故意乱序注册，遍历顺序必须仍然由 Id 决定。
                world.Register(new StubEntity(30L, profile, order));
                world.Register(new StubEntity(10L, profile, order));
                world.Register(new StubEntity(20L, profile, order));

                world.Tick(1);

                Assert.AreEqual(new List<long> { 10L, 20L, 30L }, order);
            }
            finally
            {
                // 测试资源由 UnityEngine.Object 显式销毁，避免与 System.Object 产生歧义。
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SameInputProducesSameStateHash()
        {
            var profile = CombatTestProfiles.CreateProfile(
                CombatTestProfiles.CreateAction(
                    1001, "attack", 30));
            try
            {
                var first = BuildWorld(profile, 2);
                var second = BuildWorld(profile, 2);

                for (var frame = 1; frame <= 5; frame++)
                {
                    first.Tick(frame);
                    second.Tick(frame);
                }

                Assert.AreNotEqual(0UL, first.ComputeStateHash());
                Assert.AreEqual(first.ComputeStateHash(), second.ComputeStateHash());
                Assert.AreEqual(first.Frame, second.Frame);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void WorldFrameAndGroundedStateParticipateInStateHash()
        {
            var profile = CombatTestProfiles.CreateProfile();
            try
            {
                var world = new BattleWorld("state-hash", 30);
                var entity = new StubEntity(1L, profile);
                world.Register(entity);
                var initial = world.ComputeStateHash();

                entity.Controller.SetGrounded(false);
                var airborne = world.ComputeStateHash();
                Assert.AreNotEqual(initial, airborne);

                world.Tick(1);
                Assert.AreNotEqual(airborne, world.ComputeStateHash());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void HitDeduplicationParticipatesInWorldStateHash()
        {
            var action = CombatTestProfiles.CreateAction(1001, "attack", 10);
            var serialized = new SerializedObject(action);
            var windows = serialized.FindProperty("hitWindows");
            windows.arraySize = 1;
            var window = windows.GetArrayElementAtIndex(0);
            window.FindPropertyRelative("StartFrame").intValue = 0;
            window.FindPropertyRelative("EndFrame").intValue = 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            action.ValidateData();
            var profile = CombatTestProfiles.CreateProfile(action);
            try
            {
                var world = new BattleWorld("hit-hash", 30);
                var entity = new StubEntity(1L, profile);
                world.Register(entity);
                Assert.IsTrue(entity.Controller.ActionRunner.StartAction(action.ActionId, 1, 0, false));
                var before = world.ComputeStateHash();
                var requestId = entity.Controller.ActionRunner.Current.RequestId;
                var authoritativeId = entity.Controller.ActionRunner.Current.InstanceId + 1000;
                Assert.IsTrue(entity.Controller.ActionRunner.Confirm(requestId, authoritativeId, 0));
                var afterConfirm = world.ComputeStateHash();
                Assert.AreNotEqual(before, afterConfirm,
                    "动作实例身份属于逻辑状态，必须参与世界哈希。");
                Assert.IsTrue(entity.Controller.ActionRunner.MarkHitConfirmed(authoritativeId));
                var afterHitConfirm = world.ComputeStateHash();
                Assert.AreNotEqual(afterConfirm, afterHitConfirm,
                    "命中确认状态属于逻辑状态，必须参与世界哈希。");

                Assert.IsTrue(entity.Controller.ActionRunner.TryAcceptHit(
                    entity.Controller.ActionRunner.Current.InstanceId,
                    action.HitWindows[0].StableId,
                    2));
                Assert.AreNotEqual(before, world.ComputeStateHash(),
                    "已命中去重集合属于逻辑状态，必须参与世界哈希。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(action);
            }
        }

        [Test]
        public void SnapshotRestoreRejectsChangedEntitySetBeforeMutation()
        {
            var profile = CombatTestProfiles.CreateProfile();
            try
            {
                var world = new BattleWorld("snapshot-set", 30);
                world.Register(new StubEntity(1L, profile));
                var snapshot = world.CaptureSnapshot();

                world.Register(new StubEntity(2L, profile));
                Assert.Throws<InvalidOperationException>(() => world.RestoreSnapshot(snapshot));

                world.Unregister(2L);
                world.Unregister(1L);
                Assert.Throws<InvalidOperationException>(() => world.RestoreSnapshot(snapshot));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SnapshotRestoresEveryEntity()
        {
            var profile = CombatTestProfiles.CreateProfile(
                CombatTestProfiles.CreateAction(
                    1001, "attack", 30));
            try
            {
                var world = BuildWorld(profile, 2);
                world.Tick(1);
                world.Tick(2);
                world.Tick(3);

                var hashBefore = world.ComputeStateHash();
                var snapshot = world.CaptureSnapshot();
                Assert.AreEqual(2, snapshot.Entities.Length);
                Assert.AreEqual(3, snapshot.Frame);

                foreach (var entity in world.Entities)
                {
                    entity.Controller.SetControl(ControlState.Stunned);
                }
                Assert.AreNotEqual(hashBefore, world.ComputeStateHash());

                world.RestoreSnapshot(snapshot);
                Assert.AreEqual(hashBefore, world.ComputeStateHash());
                Assert.AreEqual(3, world.Frame);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SnapshotRestoresPositionAndRotation()
        {
            var profile = CombatTestProfiles.CreateProfile();
            try
            {
                var world = new BattleWorld("test", 30);
                var entity = new StubEntity(7L, profile, null);
                entity.Position = new Vector3(1f, 0f, 2f);
                entity.Rotation = Quaternion.Euler(0f, 90f, 0f);
                world.Register(entity);

                var snapshot = world.CaptureSnapshot();
                entity.Position = new Vector3(99f, 0f, 99f);
                entity.Rotation = Quaternion.identity;

                world.RestoreSnapshot(snapshot);
                Assert.AreEqual(new Vector3(1f, 0f, 2f), entity.Position);
                Assert.AreEqual(Quaternion.Euler(0f, 90f, 0f), entity.Rotation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SystemsRunBeforeBuiltInLogicInPhaseOrder()
        {
            var profile = CombatTestProfiles.CreateProfile();
            var log = new List<string>();
            try
            {
                var world = new BattleWorld("test", 30);
                world.Register(new StubEntity(1L, profile, null, log));
                // 乱序注册，验证阶段顺序只由 BattlePhase 决定。
                world.AddSystem(new RecordingSystem(BattlePhase.Presentation, 0, log));
                world.AddSystem(new RecordingSystem(BattlePhase.Damage, 0, log));
                world.AddSystem(new RecordingSystem(BattlePhase.Commands, 0, log));
                world.AddSystem(new RecordingSystem(BattlePhase.Actions, 0, log));

                world.Tick(1);

                Assert.AreEqual(
                    new[]
                    {
                        "system:Commands",
                        "entity:Consume",
                        "system:Actions",
                        "entity:Logic",
                        "system:Damage",
                        "system:Presentation",
                        "entity:Presentation",
                    },
                    log.ToArray());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SystemsWithSameOrderFollowRegistrationSequence()
        {
            var profile = CombatTestProfiles.CreateProfile();
            var log = new List<string>();
            try
            {
                var world = new BattleWorld("test", 30);
                world.AddSystem(new RecordingSystem(BattlePhase.Hitbox, 5, log, "second"));
                world.AddSystem(new RecordingSystem(BattlePhase.Hitbox, 5, log, "first"));

                world.Tick(1);

                Assert.AreEqual(new[] { "second:Hitbox", "first:Hitbox" }, log.ToArray());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void InactiveEntitiesAreSkipped()
        {
            var world = new BattleWorld("test", 30);
            // 不传 profile，Controller 未初始化，IsCombatActive 为 false。
            var inactive = new StubEntity(1L, null);
            world.Register(inactive);

            world.Tick(1);

            Assert.AreEqual(0, inactive.ConsumeCount);
            Assert.AreEqual(0, inactive.LogicCount);
            Assert.AreEqual(0, inactive.PresentationCount);
            Assert.AreEqual(1, world.Frame);
        }

        [Test]
        public void BackwardFrameIsIgnored()
        {
            var profile = CombatTestProfiles.CreateProfile();
            try
            {
                var world = new BattleWorld("test", 30);
                world.Register(new StubEntity(1L, profile, null));

                world.Tick(5);
                Assert.AreEqual(5, world.Frame);

                world.Tick(3);
                Assert.AreEqual(5, world.Frame);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void UnregisterStopsTickingAndSnapshot()
        {
            var profile = CombatTestProfiles.CreateProfile();
            var order = new List<long>();
            try
            {
                var world = new BattleWorld("test", 30);
                var removed = new StubEntity(2L, profile, order);
                world.Register(new StubEntity(1L, profile, order));
                world.Register(removed);

                world.Tick(1);
                Assert.AreEqual(2, world.EntityCount);
                Assert.AreEqual(2, world.CaptureSnapshot().Entities.Length);
                Assert.AreEqual(new List<long> { 1L, 2L }, order);

                Assert.IsTrue(world.Unregister(2L));
                Assert.AreEqual(1, world.EntityCount);
                Assert.AreEqual(1, world.CaptureSnapshot().Entities.Length);

                order.Clear();
                world.Tick(2);

                // 被注销的单位只在第 1 帧跑过一次，第 2 帧不再参与。
                Assert.AreEqual(1, removed.LogicCount);
                Assert.AreEqual(new List<long> { 1L }, order);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void CombatMgrCreatesAndDestroysWorlds()
        {
            var mgr = CombatMgr.Ins;
            mgr.AutoDriveByClock = false;
            try
            {
                var replay = mgr.CreateWorld("battleworldtests_replay", 30, 0);
                Assert.AreSame(replay, mgr.GetWorld("battleworldtests_replay"));
                Assert.IsNotNull(mgr.Main);
                Assert.AreNotSame(replay, mgr.Main);

                Assert.IsTrue(mgr.DestroyWorld("battleworldtests_replay"));
                Assert.IsNull(mgr.GetWorld("battleworldtests_replay"));
                Assert.IsFalse(mgr.DestroyWorld("battleworldtests_replay"));
            }
            finally
            {
                mgr.DestroyAllWorlds();
                mgr.AutoDriveByClock = true;
            }
        }

        private static BattleWorld BuildWorld(CharacterCombatProfile profile, long attackFrame)
        {
            var world = new BattleWorld("test", 30);
            world.Register(new StubEntity(10L, profile, null, null, attackFrame));
            world.Register(new StubEntity(20L, profile, null, null, attackFrame));
            return world;
        }

        private sealed class StubEntity : ICombatEntity
        {
            private readonly List<string> _log;
            private readonly List<long> _order;
            private readonly long _attackFrame;

            public StubEntity(long id, CharacterCombatProfile profile, List<long> order = null, List<string> log = null, long attackFrame = 0)
            {
                Id = id;
                _order = order;
                _log = log;
                _attackFrame = attackFrame;
                Controller = new CombatController();
                if (profile != null)
                {
                    Controller.Initialize(profile, id, 0);
                }
            }

            public long Id { get; }
            public bool IsCombatActive => Controller.IsInitialized;
            public CombatController Controller { get; }
            public Vector2 MoveInput => Vector2.zero;
            public Vector3 Position { get; set; }
            public Quaternion Rotation { get; set; }

            public int ConsumeCount { get; private set; }
            public int LogicCount { get; private set; }
            public int PresentationCount { get; private set; }

            public CombatFrameCommands ConsumeCommands(long frame)
            {
                ConsumeCount++;
                _log?.Add("entity:Consume");
                if (_attackFrame > 0 && frame == _attackFrame)
                {
                    return new CombatFrameCommands(new[]
                    {
                        new CombatCommand(1, frame, 1001),
                    });
                }
                return CombatFrameCommands.Empty;
            }

            public void TickLogic(long frame, in CombatFrameCommands commands)
            {
                LogicCount++;
                _order?.Add(Id);
                _log?.Add("entity:Logic");
                Controller.Tick(frame, MoveInput, commands);
            }

            public void TickPresentation(long frame)
            {
                PresentationCount++;
                _log?.Add("entity:Presentation");
            }
        }

        private sealed class RecordingSystem : ICombatSystem
        {
            private readonly List<string> _log;
            private readonly string _name;

            public RecordingSystem(BattlePhase phase, int order, List<string> log, string name = "system")
            {
                Phase = phase;
                Order = order;
                _log = log;
                _name = name;
            }

            public BattlePhase Phase { get; }

            public int Order { get; }

            public void Tick(BattleWorld world, long frame)
            {
                _log.Add($"{_name}:{Phase}");
            }
        }
    }
}
