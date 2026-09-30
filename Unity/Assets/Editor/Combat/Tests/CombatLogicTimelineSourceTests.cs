using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat.Tests
{
    public sealed class CombatLogicTimelineSourceTests
    {
        TimelineAsset timeline;
        CombatActionAsset action;
        CharacterCombatProfile profile;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            action = ScriptableObject.CreateInstance<CombatActionAsset>();
            profile = ScriptableObject.CreateInstance<CharacterCombatProfile>();
            SetActionData(action, 1001, "attack", 30);
            SetProfileActions(profile, action);
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            if (profile != null) UnityEngine.Object.DestroyImmediate(profile);
            if (action != null) UnityEngine.Object.DestroyImmediate(action);
            if (timeline != null) UnityEngine.Object.DestroyImmediate(timeline);
        }

        [Test]
        public void CancelWindowsReceivePersistentUniqueIds()
        {
            var serialized = new SerializedObject(action);
            var windows = serialized.FindProperty("cancelWindows");
            windows.arraySize = 2;
            for (var i = 0; i < windows.arraySize; i++)
            {
                var window = windows.GetArrayElementAtIndex(i);
                window.FindPropertyRelative("stableId").stringValue = "duplicate";
                window.FindPropertyRelative("StartFrame").intValue = i;
                window.FindPropertyRelative("EndFrame").intValue = i + 1;
                window.FindPropertyRelative("TargetActionId").intValue = 1001;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            action.ValidateData();

            Assert.IsFalse(string.IsNullOrEmpty(action.CancelWindows[0].StableId));
            Assert.IsFalse(string.IsNullOrEmpty(action.CancelWindows[1].StableId));
            Assert.AreNotEqual(
                action.CancelWindows[0].StableId,
                action.CancelWindows[1].StableId,
                "同一动作内的取消窗口必须拥有稳定且唯一的 ItemId。");
        }

        [Test]
        public void LogicSourceEditsHalfOpenWindowsAndAllowsOverlap()
        {
            var undoRecords = new List<(string Key, UnityEngine.Object Owner)>();
            var saveCount = 0;
            var source = new CombatLogicTimelineSource(
                action,
                profile,
                registerUndo: (key, owner, _) => undoRecords.Add((key, owner)),
                save: () => saveCount++);
            var track = EnsureCancelTrack(source);

            var first = track.Clips[0];
            var second = track.CreateClip();
            first.SetFrames(0, 10);
            second.SetFrames(5, 15);

            Assert.AreEqual(2, action.CancelWindows.Count);
            Assert.AreEqual(0, first.StartFrame);
            Assert.AreEqual(10, first.EndFrame);
            Assert.AreEqual(5, second.StartFrame);
            Assert.AreEqual(15, second.EndFrame);
            Assert.IsTrue(track.IsLayoutValid(), "不同取消窗口允许在同一逻辑帧重叠开放。");
            Assert.AreEqual(4, saveCount);
            Assert.IsTrue(undoRecords.TrueForAll(record => ReferenceEquals(action, record.Owner)),
                "逻辑轨所有 Undo 必须记录完整 CombatActionAsset owner。");
        }

        [Test]
        public void WindowTracksCanBeAddedAndRemovedDynamically()
        {
            var source = new CombatLogicTimelineSource(action, profile);

            Assert.AreEqual(0, source.Tracks.Count);
            var cancelTrack = (CombatCancelWindowEditorTrack)source.AddTrack(
                typeof(CombatCancelWindowEditorTrack));
            Assert.IsNotNull(cancelTrack);
            Assert.AreEqual(1, action.CancelWindows.Count);
            Assert.AreEqual(1, source.Tracks.Count);
            Assert.IsTrue(cancelTrack.CanRemove);
            Assert.IsTrue(cancelTrack.Remove());
            Assert.AreEqual(0, action.CancelWindows.Count);
            Assert.AreEqual(0, source.Tracks.Count);

            var hitboxTrack = (CombatHitboxWindowEditorTrack)source.AddTrack(
                typeof(CombatHitboxWindowEditorTrack));
            Assert.IsNotNull(hitboxTrack);
            Assert.AreEqual(1, action.HitboxWindows.Count);
            Assert.IsTrue(hitboxTrack.Remove());
            Assert.AreEqual(0, action.HitboxWindows.Count);
            Assert.AreEqual(0, source.Tracks.Count);
        }

        [Test]
        public void NewWindowDefaultsToReadableMultiFrameSpan()
        {
            var source = new CombatLogicTimelineSource(action, profile);
            var clip = EnsureCancelTrack(source).Clips[0];

            Assert.AreEqual(0, clip.StartFrame);
            Assert.AreEqual(5, clip.EndFrame);
        }

        [Test]
        public void LinkTrackEditsActionLinkWindows()
        {
            var source = new CombatLogicTimelineSource(action, profile);
            var track = (CombatLinkWindowEditorTrack)source.AddTrack(
                typeof(CombatLinkWindowEditorTrack));
            var clip = track.Clips[0];

            clip.SetFrames(6, 18);
            clip.SetTargetActionId(action.ActionId);

            Assert.AreEqual(1, action.LinkWindows.Count);
            Assert.AreEqual(6, action.LinkWindows[0].StartFrame);
            Assert.AreEqual(18, action.LinkWindows[0].EndFrame);
            Assert.AreEqual(action.ActionId, action.LinkWindows[0].TargetActionId);
            Assert.IsNotNull(source.CreateInspector(track));
            Assert.IsNotNull(source.CreateInspector(clip));
        }

        [Test]
        public void LogicClipClampsRangeToActionDuration()
        {
            var source = new CombatLogicTimelineSource(action, profile);
            var track = EnsureCancelTrack(source);
            var clip = track.Clips[0];

            clip.SetFrames(-5, 100);
            Assert.AreEqual(0, clip.StartFrame);
            Assert.AreEqual(30, clip.EndFrame);

            clip.Drag(DragStatus.Right, 0, 30);
            Assert.AreEqual(0, clip.StartFrame);
            Assert.AreEqual(1, clip.EndFrame,
                "EndFrame 是不含边界，但必须至少比 StartFrame 大一帧。");

            clip.SetFrames(29, 30);
            clip.Drag(DragStatus.Move, 40, 29);
            Assert.AreEqual(29, clip.StartFrame);
            Assert.AreEqual(30, clip.EndFrame,
                "整体移动不得越过 CombatActionAsset.DurationFrames。");
        }

        [Test]
        public void HitboxWindowsShareStableIdDomainWithCancelWindows()
        {
            var serialized = new SerializedObject(action);
            var cancelWindows = serialized.FindProperty("cancelWindows");
            cancelWindows.arraySize = 1;
            var cancel = cancelWindows.GetArrayElementAtIndex(0);
            cancel.FindPropertyRelative("stableId").stringValue = "shared";
            cancel.FindPropertyRelative("StartFrame").intValue = 0;
            cancel.FindPropertyRelative("EndFrame").intValue = 1;
            cancel.FindPropertyRelative("TargetActionId").intValue = action.ActionId;

            var hitboxWindows = serialized.FindProperty("hitboxWindows");
            hitboxWindows.arraySize = 1;
            var hit = hitboxWindows.GetArrayElementAtIndex(0);
            hit.FindPropertyRelative("stableId").stringValue = "shared";
            hit.FindPropertyRelative("StartFrame").intValue = 1;
            hit.FindPropertyRelative("EndFrame").intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            action.ValidateData();

            Assert.AreEqual("shared", action.CancelWindows[0].StableId,
                "固定扫描顺序必须优先保留已有取消窗口 ID。");
            Assert.AreNotEqual(
                action.CancelWindows[0].StableId,
                action.HitboxWindows[0].StableId,
                "同一 CombatActionAsset 的全部逻辑子项必须共享 ItemId 唯一域。");
        }

        [Test]
        public void LogicSourceStableIdMigrationDoesNotRepairInvalidWindowRange()
        {
            var invalidWindow = new ActionHitboxWindow
            {
                StartFrame = -5,
                EndFrame = -2,
            };
            var field = typeof(CombatActionAsset).GetField(
                "hitboxWindows",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(action, new List<ActionHitboxWindow> { invalidWindow });

            var source = new CombatLogicTimelineSource(action, profile);

            Assert.IsFalse(string.IsNullOrEmpty(invalidWindow.StableId));
            Assert.AreEqual(-5, invalidWindow.StartFrame,
                "打开逻辑时间轴只能迁移身份，不得静默修正非法业务区间。");
            Assert.AreEqual(-2, invalidWindow.EndFrame);
            Assert.IsFalse(EnsureHitboxTrack(source).IsLayoutValid());
        }

        [Test]
        public void PersistedStableIdMigrationPreservesInvalidBusinessRanges()
        {
            const string path = "Assets/__CombatLogicIdentityMigrationTests.asset";
            AssetDatabase.DeleteAsset(path);
            var persistedAction = ScriptableObject.CreateInstance<CombatActionAsset>();
            try
            {
                SetActionData(persistedAction, 2001, "migration", 10);
                var cancelWindow = new ActionCancelWindow
                {
                    StartFrame = 0,
                    EndFrame = 1,
                    TargetActionId = 2001,
                };
                var hitWindow = new ActionHitboxWindow
                {
                    StartFrame = 0,
                    EndFrame = 1,
                };
                cancelWindow.ValidateData();
                hitWindow.ValidateData();
                var cancelId = cancelWindow.StableId;
                var hitId = hitWindow.StableId;
                typeof(CombatActionAsset).GetField(
                    "cancelWindows",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)?.SetValue(
                    persistedAction,
                    new List<ActionCancelWindow> { cancelWindow });
                typeof(CombatActionAsset).GetField(
                    "hitboxWindows",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)?.SetValue(
                    persistedAction,
                    new List<ActionHitboxWindow> { hitWindow });
                AssetDatabase.CreateAsset(persistedAction, path);

                cancelWindow.StartFrame = -7;
                cancelWindow.EndFrame = -3;
                hitWindow.StartFrame = -5;
                hitWindow.EndFrame = -2;
                EditorUtility.SetDirty(persistedAction);
                AssetDatabase.SaveAssetIfDirty(persistedAction);

                var fullPath = Path.GetFullPath(path);
                var yaml = File.ReadAllText(fullPath)
                    .Replace($"stableId: {cancelId}", "stableId:")
                    .Replace($"stableId: {hitId}", "stableId:");
                File.WriteAllText(fullPath, yaml);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var loaded = AssetDatabase.LoadAssetAtPath<CombatActionAsset>(path);
                Assert.AreEqual(-7, loaded.CancelWindows[0].StartFrame,
                    "导入旧资源不得通过 OnValidate 静默修正取消窗口。");
                Assert.AreEqual(-5, loaded.HitboxWindows[0].StartFrame,
                    "导入旧资源不得通过 OnValidate 静默修正攻击判定。");

                _ = new CombatLogicTimelineSource(loaded);

                var savedYaml = File.ReadAllText(fullPath);
                StringAssert.Contains(
                    $"stableId: {loaded.CancelWindows[0].StableId}",
                    savedYaml);
                StringAssert.Contains(
                    $"stableId: {loaded.HitboxWindows[0].StableId}",
                    savedYaml);
                StringAssert.Contains("StartFrame: -7", savedYaml);
                StringAssert.Contains("EndFrame: -3", savedYaml);
                StringAssert.Contains("StartFrame: -5", savedYaml);
                StringAssert.Contains("EndFrame: -2", savedYaml);
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                if (persistedAction != null)
                {
                    UnityEngine.Object.DestroyImmediate(persistedAction, true);
                }
            }
        }

        [Test]
        public void LogicSourceEditsOverlappingHitboxWindows()
        {
            var undoOwners = new List<UnityEngine.Object>();
            var source = new CombatLogicTimelineSource(
                action,
                profile,
                registerUndo: (_, owner, _) => undoOwners.Add(owner));
            var track = EnsureHitboxTrack(source);

            var first = track.Clips[0];
            var second = track.CreateClip();
            first.SetFrames(2, 8);
            second.SetFrames(5, 12);

            Assert.AreEqual(2, action.HitboxWindows.Count);
            Assert.AreEqual(2, first.StartFrame);
            Assert.AreEqual(8, first.EndFrame);
            Assert.AreEqual(5, second.StartFrame);
            Assert.AreEqual(12, second.EndFrame);
            Assert.IsTrue(track.IsLayoutValid(), "不同攻击判定代表独立命中周期，允许显式重叠。");
            Assert.IsTrue(undoOwners.TrueForAll(owner => ReferenceEquals(action, owner)));
            Assert.IsNotNull(source.CreateInspector(track));
            Assert.IsNotNull(source.CreateInspector(first));
        }

        [Test]
        public void HitWindowGeometryUsesLogicAssetUndoAndSaveOwner()
        {
            var undoOwners = new List<UnityEngine.Object>();
            var saveCount = 0;
            var source = new CombatLogicTimelineSource(
                action,
                profile,
                registerUndo: (_, owner, _) => undoOwners.Add(owner),
                save: () => saveCount++);
            var clip = EnsureHitboxTrack(source).Clips[0];
            var saveBefore = saveCount;

            clip.SetGeometry(ActionHitShape.Circle, 2500);

            Assert.AreEqual(ActionHitShape.Circle, action.HitboxWindows[0].Shape);
            Assert.AreEqual(2500, action.HitboxWindows[0].RadiusMillimeters);
            Assert.AreSame(action, undoOwners[undoOwners.Count - 1]);
            Assert.Greater(saveCount, saveBefore);
            Assert.IsNotNull(source.CreateInspector(clip));
        }

        [Test]
        public void HitWindowDragClampsAndSupportsUndoRedo()
        {
            using var uxUndo = new Ux.Editor.UxUndo();
            var document = new TimelineEditorDocument();
            var source = new CombatLogicTimelineSource(
                action,
                profile,
                registerUndo: (key, owner, _) => uxUndo.RegUndo(
                    key,
                    owner,
                    () => document.RefreshAfterUndo(owner)),
                completeUndo: uxUndo.CompleteUndo);
            document.SetSource(source);
            var clip = EnsureHitboxTrack(source).Clips[0];

            clip.SetFrames(-5, 100);
            Assert.AreEqual(0, clip.StartFrame);
            Assert.AreEqual(30, clip.EndFrame);

            clip.BeginDrag();
            clip.Drag(DragStatus.Right, 10, 30);
            clip.CommitEdit();
            Assert.AreEqual(10, action.HitboxWindows[0].EndFrame);

            Undo.PerformUndo();
            Assert.AreEqual(30, action.HitboxWindows[0].EndFrame);
            Undo.PerformRedo();
            Assert.AreEqual(10, action.HitboxWindows[0].EndFrame);
        }

        [Test]
        public void LogicSourceEditsSpawnEventsWithProfileReference()
        {
            var undoOwners = new List<UnityEngine.Object>();
            var saveCount = 0;
            var source = new CombatLogicTimelineSource(
                action,
                profile,
                registerUndo: (_, owner, _) => undoOwners.Add(owner),
                save: () => saveCount++);
            var frameEvent = EnsureSpawnEvent(source);

            Assert.IsNotNull(frameEvent);
            Assert.AreEqual(1, action.FrameEvents.Count);
            Assert.AreEqual(27, frameEvent.Frame);
            frameEvent.SetDisplayName("刀波");
            Assert.AreEqual("刀波", frameEvent.Name);
            Assert.IsNotNull(source.CreateInspector(frameEvent));
            Assert.IsTrue(undoOwners.TrueForAll(owner => ReferenceEquals(action, owner)),
                "帧事件的 Undo 必须记录完整 CombatActionAsset owner。");

            var spawn = ScriptableObject.CreateInstance<CombatSpawnProfile>();
            try
            {
                var saveBefore = saveCount;
                frameEvent.SetSpawnProfile(spawn);

                Assert.AreSame(spawn, ((ActionSpawnEvent)action.FrameEvents[0]).SpawnProfile);
                Assert.AreSame(action, undoOwners[undoOwners.Count - 1]);
                Assert.Greater(saveCount, saveBefore);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(spawn);
            }
        }

        [Test]
        public void SpawnEventClampsFrameAndSharesItemIdDomain()
        {
            var source = new CombatLogicTimelineSource(action, profile);
            var frameEvent = EnsureSpawnEvent(source);

            frameEvent.SetFrame(-5);
            Assert.AreEqual(0, frameEvent.Frame);
            frameEvent.SetFrame(100);
            Assert.AreEqual(29, frameEvent.Frame);

            EnsureCancelTrack(source);
            EnsureHitboxTrack(source);

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var window in action.CancelWindows)
            {
                Assert.IsTrue(ids.Add(window.StableId));
            }
            foreach (var window in action.HitboxWindows)
            {
                Assert.IsTrue(ids.Add(window.StableId));
            }
            foreach (var frameItem in action.FrameEvents)
            {
                Assert.IsTrue(ids.Add(frameItem.StableId),
                    "帧事件与取消/攻击判定必须共享同一个 ItemId 唯一域。");
            }
        }

        [Test]
        public void ProfileValidationRejectsSpawnEventWithoutSpawnProfile()
        {
            var source = new CombatLogicTimelineSource(action, profile);
            Assert.IsNotNull(EnsureSpawnEvent(source));

            Assert.Throws<InvalidOperationException>(
                () => profile.ValidateRuntime(),
                "生成事件未配置生成物时必须被角色 Profile 校验拒绝。");
        }

        [Test]
        public void LogicUndoRefreshesCurrentAdapters()
        {
            using var uxUndo = new Ux.Editor.UxUndo();
            var document = new TimelineEditorDocument();
            var source = new CombatLogicTimelineSource(
                action,
                profile,
                registerUndo: (key, owner, _) => uxUndo.RegUndo(
                    key,
                    owner,
                    () => document.RefreshAfterUndo(owner)),
                completeUndo: uxUndo.CompleteUndo);
            document.SetSource(source);

            Assert.IsNotNull(EnsureCancelTrack(source).Clips[0]);
            Assert.AreEqual(1, action.CancelWindows.Count);

            Undo.PerformUndo();

            Assert.AreEqual(0, action.CancelWindows.Count);
            Assert.AreEqual(0, source.Tracks.Count,
                "Undo 删除最后一个窗口后，空逻辑轨必须被移除。");
        }

        [Test]
        public void LogicDragUndoRedoPreservesHalfOpenRange()
        {
            using var uxUndo = new Ux.Editor.UxUndo();
            var document = new TimelineEditorDocument();
            var source = new CombatLogicTimelineSource(
                action,
                profile,
                registerUndo: (key, owner, _) => uxUndo.RegUndo(
                    key,
                    owner,
                    () => document.RefreshAfterUndo(owner)),
                completeUndo: uxUndo.CompleteUndo);
            document.SetSource(source);
            var clip = EnsureCancelTrack(source).Clips[0];

            clip.BeginDrag();
            clip.Drag(DragStatus.Move, 5, 0);
            clip.CommitEdit();
            Assert.AreEqual(5, action.CancelWindows[0].StartFrame);
            Assert.AreEqual(6, action.CancelWindows[0].EndFrame);

            Undo.PerformUndo();
            Assert.AreEqual(0, action.CancelWindows[0].StartFrame);
            Assert.AreEqual(1, action.CancelWindows[0].EndFrame);

            Undo.PerformRedo();
            Assert.AreEqual(5, action.CancelWindows[0].StartFrame);
            Assert.AreEqual(6, action.CancelWindows[0].EndFrame);
        }

        [Test]
        public void PresentationAndLogicUndoGroupsRemainIndependent()
        {
            using var uxUndo = new Ux.Editor.UxUndo();
            var document = new TimelineEditorDocument();
            Action<string, UnityEngine.Object, Action> registerUndo = (key, owner, _) =>
                uxUndo.RegUndo(key, owner, () => document.RefreshAfterUndo(owner));
            var presentation = new TimelineAssetEditorSource(
                timeline,
                registerUndo,
                completeUndo: uxUndo.CompleteUndo);
            var logic = new CombatLogicTimelineSource(
                action,
                profile,
                registerUndo: registerUndo,
                completeUndo: uxUndo.CompleteUndo);
            document.SetSources(presentation, logic);

            presentation.AddTrack(typeof(AnimationTrackAsset));
            EnsureCancelTrack(logic);
            Assert.AreEqual(1, timeline.tracks.Count);
            Assert.AreEqual(1, action.CancelWindows.Count);

            Undo.PerformUndo();
            Assert.AreEqual(1, timeline.tracks.Count);
            Assert.AreEqual(0, action.CancelWindows.Count,
                "最近一次逻辑 Undo 不得回滚表现 owner。");

            Undo.PerformUndo();
            Assert.AreEqual(0, timeline.tracks.Count);

            Undo.PerformRedo();
            Undo.PerformRedo();
            Assert.AreEqual(1, timeline.tracks.Count);
            Assert.AreEqual(1, action.CancelWindows.Count);
        }

        [Test]
        public void DocumentCombinesPresentationAndLogicWithIndependentSaveOwners()
        {
            var presentationSaveCount = 0;
            var logicSaveCount = 0;
            var presentation = new TimelineAssetEditorSource(
                timeline,
                (_, _, _) => { },
                () => presentationSaveCount++);
            Assert.IsNotNull(presentation.AddTrack(typeof(AnimationTrackAsset)));
            var logic = new CombatLogicTimelineSource(
                action,
                profile,
                save: () => logicSaveCount++,
                frameRateProvider: () => timeline.FrameRate);
            var document = new TimelineEditorDocument();

            document.SetSources(presentation, logic);

            Assert.AreEqual(2, document.Sources.Count);
            EnsureCancelTrack(logic);
            EnsureHitboxTrack(logic);
            Assert.AreEqual(3, document.TrackCount,
                "表现轨与实际存在的取消/命中逻辑轨必须共用同一个 Document/帧标尺。");
            Assert.AreEqual(0, document.FrameEvents.Count,
                "没有帧事件时不应伪造生成轨或事件标记。");
            var frameEvent = document.AddFrameEvent(typeof(ActionSpawnEvent), 12);
            Assert.IsNotNull(frameEvent);
            Assert.AreEqual(1, document.FrameEvents.Count);
            Assert.AreEqual(3, document.TrackCount,
                "添加帧事件不得创建生成轨。");
            Assert.IsNotNull(document.CreateInspector(frameEvent));
            Assert.AreEqual(30, document.DurationFrames,
                "会话宽度采用所有 source 的最大持续帧。逻辑时长不能从表现 Timeline 推导。");
            Assert.AreEqual(timeline.FrameRate, document.FrameRate);
            Assert.IsNotNull(document.CreateInspector(logic.Tracks[0]),
                "Inspector 必须按 selection 所属 source 分派。");

            var presentationBefore = presentationSaveCount;
            var logicBefore = logicSaveCount;
            document.SaveAll();
            Assert.AreEqual(presentationBefore + 1, presentationSaveCount);
            Assert.AreEqual(logicBefore + 1, logicSaveCount);
        }

        [Test]
        public void InvalidActionDurationIsNotHiddenByLogicSource()
        {
            var serialized = new SerializedObject(action);
            serialized.FindProperty("durationFrames").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var source = new CombatLogicTimelineSource(action, profile);

            Assert.AreEqual(0, action.DurationFrames);
            Assert.AreEqual(0, source.DurationFrames,
                "逻辑时长非法时必须原样暴露给校验，不能从表现 Timeline 或编辑器默认值推导。");
            Assert.AreEqual(0, source.Tracks.Count,
                "非法逻辑时长不得创建看似有效的取消窗口轨。");
            Assert.IsNull(source.AddTrack(typeof(CombatCancelWindowEditorTrack)),
                "非法逻辑时长不得创建看似有效的取消窗口。");
        }

        [Test]
        public void DocumentRejectsInspectorForDetachedSource()
        {
            var document = new TimelineEditorDocument();
            var first = new CombatLogicTimelineSource(action, profile);
            var detachedTrack = EnsureCancelTrack(first);
            document.SetSource(first);
            Assert.IsNotNull(document.CreateInspector(detachedTrack));

            var otherAction = ScriptableObject.CreateInstance<CombatActionAsset>();
            try
            {
                SetActionData(otherAction, 1002, "other", 20);
                document.SetSource(new CombatLogicTimelineSource(otherAction));
                Assert.IsNull(document.CreateInspector(detachedTrack),
                    "资源切换后不得重新创建仍绑定旧 owner 的 Inspector。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(otherAction);
            }
        }

        [Test]
        public void AddedWindowIdIsPersistedWithActionAsset()
        {
            const string path = "Assets/__CombatLogicTimelineSourceTests.asset";
            AssetDatabase.DeleteAsset(path);
            var persistedAction = ScriptableObject.CreateInstance<CombatActionAsset>();
            try
            {
                SetActionData(persistedAction, 2001, "persisted", 10);
                AssetDatabase.CreateAsset(persistedAction, path);
                var source = new CombatLogicTimelineSource(persistedAction);
                var clip = EnsureCancelTrack(source).Clips[0];
                Assert.IsNotNull(clip);
                AssetDatabase.SaveAssets();

                var yaml = File.ReadAllText(Path.GetFullPath(path));
                StringAssert.Contains($"stableId: {clip.Id}", yaml,
                    "取消窗口稳定 ID 必须写入 CombatActionAsset，而不只是停留在 adapter 内存中。");
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                if (persistedAction != null)
                {
                    UnityEngine.Object.DestroyImmediate(persistedAction, true);
                }
            }
        }

        [Test]
        public void DocumentReportsWhichSourceChanged()
        {
            var presentation = new TimelineAssetEditorSource(timeline, (_, _, _) => { });
            var logic = new CombatLogicTimelineSource(action, profile);
            var document = new TimelineEditorDocument();
            var changedSources = new List<ITimelineEditorSource>();
            document.SourceChanged += changedSources.Add;
            document.SetSources(presentation, logic);

            EnsureCancelTrack(logic);
            presentation.AddTrack(typeof(AnimationTrackAsset));

            CollectionAssert.AreEqual(
                new ITimelineEditorSource[] { logic, presentation },
                changedSources,
                "窗口必须能区分逻辑变更与表现变更，避免逻辑编辑触发表现重播。");
        }

        static CombatCancelWindowEditorTrack EnsureCancelTrack(CombatLogicTimelineSource source)
        {
            foreach (var track in source.Tracks)
            {
                if (track is CombatCancelWindowEditorTrack cancelTrack)
                {
                    return cancelTrack;
                }
            }
            return (CombatCancelWindowEditorTrack)source.AddTrack(typeof(CombatCancelWindowEditorTrack));
        }

        static CombatHitboxWindowEditorTrack EnsureHitboxTrack(CombatLogicTimelineSource source)
        {
            foreach (var track in source.Tracks)
            {
                if (track is CombatHitboxWindowEditorTrack hitboxTrack)
                {
                    return hitboxTrack;
                }
            }
            return (CombatHitboxWindowEditorTrack)source.AddTrack(typeof(CombatHitboxWindowEditorTrack));
        }

        static CombatFrameEventEditorAdapter EnsureSpawnEvent(CombatLogicTimelineSource source)
        {
            foreach (var frameEvent in source.FrameEvents)
            {
                if (frameEvent is CombatFrameEventEditorAdapter spawnEvent)
                {
                    return spawnEvent;
                }
            }
            return (CombatFrameEventEditorAdapter)source.AddFrameEvent(typeof(ActionSpawnEvent), 27);
        }

        static void SetActionData(CombatActionAsset target, int actionId, string displayName, int durationFrames)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty("actionId").intValue = actionId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("durationFrames").intValue = durationFrames;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            target.ValidateData();
        }

        static void SetProfileActions(CharacterCombatProfile target, params CombatActionAsset[] actions)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty("actions");
            property.arraySize = actions.Length;
            for (var i = 0; i < actions.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = actions[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            target.ValidateData();
        }
    }
}
