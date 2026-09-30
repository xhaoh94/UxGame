using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Ux.Editor;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat.Tests
{
    public sealed class TimelineEditorSourceTests
    {
        TimelineAsset asset;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
            asset = ScriptableObject.CreateInstance<TimelineAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            if (asset != null)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void SourceAddsAndRemovesTracksAndClips()
        {
            var saveCount = 0;
            var source = new TimelineAssetEditorSource(
                asset,
                (_, _, _) => { },
                () => saveCount++);

            var track = source.AddTrack(typeof(AnimationTrackAsset));
            Assert.IsNotNull(track);
            Assert.AreEqual(1, source.TrackCount);
            Assert.AreEqual(1, asset.tracks.Count);
            var addedClip = track.AddClip(typeof(AnimationClipAsset), 0);
            Assert.IsNotNull(addedClip);
            Assert.AreSame(track, addedClip.Track);
            Assert.AreEqual(1, track.Clips.Count);
            Assert.AreEqual(1, asset.tracks[0].clips.Count);

            var clip = track.Clips[0];
            Assert.IsTrue(track.RemoveClip(clip));
            Assert.AreEqual(0, track.Clips.Count);
            Assert.AreEqual(0, asset.tracks[0].clips.Count);

            Assert.IsTrue(track.Remove());
            Assert.AreEqual(0, source.TrackCount);
            Assert.AreEqual(0, asset.tracks.Count);
            Assert.AreEqual(4, saveCount, "添加/删除轨道和 Clip 均必须经 source 保存。");
        }

        [Test]
        public void ClipOverlapUsesHalfOpenRangesAndUpdatesMixFrames()
        {
            var source = new TimelineAssetEditorSource(asset, (_, _, _) => { }, () => { });
            var track = source.AddTrack(typeof(AnimationTrackAsset));
            var first = track.AddClip(new AnimationClipAsset
            {
                clipName = "first",
                StartFrame = 0,
                EndFrame = 10,
            });
            var second = track.AddClip(new AnimationClipAsset
            {
                clipName = "second",
                StartFrame = 10,
                EndFrame = 20,
            });

            Assert.IsTrue(track.IsLayoutValid(), "[0,10) 与 [10,20) 仅接触，不应重叠。");
            track.UpdateMixData();
            Assert.AreEqual(0, first.OutFrame);
            Assert.AreEqual(0, second.InFrame);

            second.SetFrames(9, 20, false);
            Assert.IsTrue(track.IsLayoutValid());
            track.UpdateMixData();
            Assert.AreEqual(9, first.OutFrame);
            Assert.AreEqual(10, second.InFrame);

            second.SetFrames(2, 8, false);
            Assert.IsFalse(track.IsLayoutValid(), "完整包含仍是非法重叠。");
        }

        [Test]
        public void EveryMutationUsesTimelineAsUndoOwnerAndSaveRoutesThroughSource()
        {
            var undoRecords = new List<UndoRecord>();
            var saveCount = 0;
            var source = new TimelineAssetEditorSource(
                asset,
                (key, owner, callback) => undoRecords.Add(new UndoRecord(key, owner, callback)),
                () => saveCount++);

            Assert.IsTrue(source.SetFrameRate(24));
            var track = source.AddTrack(typeof(AnimationTrackAsset));
            track.Rename("renamed track");
            var clip = track.AddClip(new AnimationClipAsset
            {
                StartFrame = 0,
                EndFrame = 10,
            });
            clip.Rename("renamed clip");
            clip.BeginDrag();
            clip.Drag(DragStatus.Move, 2, 0);
            clip.CommitEdit();
            clip.SetFrames(3, 13);
            Assert.IsTrue(track.RemoveClip(clip));
            Assert.IsTrue(track.Remove());

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "timeline_frame_rate",
                    "timeline_add_track",
                    "timeline_rename_track",
                    "track_add_clip_item",
                    "timeline_rename_clip",
                    "drag_track",
                    "timeline_clip_frames",
                    "track_remove_clip_item",
                    "timeline_remove_track",
                },
                undoRecords.ConvertAll(record => record.Key));
            Assert.IsTrue(undoRecords.TrueForAll(record => ReferenceEquals(asset, record.Owner)),
                "所有编辑 Undo owner 必须是完整 TimelineAsset，而不是 managed-reference 子对象。");
            Assert.AreEqual(9, saveCount,
                "BeginDrag 只登记 Undo；拖拽提交及其余变更必须保存。");

            var beforeCallbacks = saveCount;
            foreach (var record in undoRecords)
            {
                record.Callback?.Invoke();
            }
            Assert.AreEqual(beforeCallbacks + undoRecords.Count, saveCount,
                "Undo/Redo 回调也必须通过 source 保存完整 owner。");
        }

        [Test]
        public void SourceRejectsMutationsWhileEditingIsLocked()
        {
            var canEdit = false;
            var source = new TimelineAssetEditorSource(
                asset,
                (_, _, _) => { },
                () => { },
                () => canEdit);

            Assert.IsNull(source.AddTrack(typeof(AnimationTrackAsset)));
            Assert.IsFalse(source.SetFrameRate(24));

            canEdit = true;
            var track = source.AddTrack(typeof(AnimationTrackAsset));
            var clip = track.AddClip(new AnimationClipAsset
            {
                clipName = "locked",
                StartFrame = 0,
                EndFrame = 10,
            });

            canEdit = false;
            track.Rename("changed");
            clip.SetFrames(5, 15);
            Assert.AreNotEqual("changed", track.Name);
            Assert.AreEqual(0, clip.StartFrame);
            Assert.AreEqual(10, clip.EndFrame);
            Assert.IsFalse(track.RemoveClip(clip));
            Assert.IsFalse(track.Remove());
        }

        [Test]
        public void UxUndoIgnoresUnrelatedUndoGroups()
        {
            var other = ScriptableObject.CreateInstance<TimelineAsset>();
            var callbackCount = 0;
            using var uxUndo = new UxUndo();
            try
            {
                var originalAssetName = asset.name;
                var originalOtherName = other.name;
                uxUndo.RegUndo("tracked_timeline", asset, () => callbackCount++);
                asset.name = "tracked";
                uxUndo.CompleteUndo();

                // UxUndo 必须自行结束 tracked group；调用方不应额外创建分组来维持隔离。
                Undo.RecordObject(other, "unrelated_object");
                other.name = "unrelated";
                Undo.FlushUndoRecordObjects();

                Undo.PerformUndo();
                Assert.AreEqual(0, callbackCount, "无关 Unity Undo 不得弹出 Timeline 回调。");
                Assert.AreEqual(originalOtherName, other.name);
                Assert.AreEqual("tracked", asset.name);

                Undo.PerformUndo();
                Assert.AreEqual(1, callbackCount);
                Assert.AreEqual(originalAssetName, asset.name);

                Undo.PerformRedo();
                Assert.AreEqual(2, callbackCount);
                Assert.AreEqual("tracked", asset.name);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void UxUndoTracksAdditionalObjectsInOneGroup()
        {
            var other = ScriptableObject.CreateInstance<TimelineAsset>();
            var callbackCount = 0;
            using var uxUndo = new UxUndo();
            try
            {
                var originalAssetName = asset.name;
                var originalOtherName = other.name;
                uxUndo.RegUndo("tracked_timeline", asset, () => callbackCount++);
                asset.name = "tracked";
                uxUndo.RecordAdditionalObject("tracked_other", other, () => callbackCount++);
                other.name = "other";
                uxUndo.CompleteUndo();

                Undo.PerformUndo();
                Assert.AreEqual(originalAssetName, asset.name);
                Assert.AreEqual(originalOtherName, other.name);
                Assert.AreEqual(2, callbackCount);

                Undo.PerformRedo();
                Assert.AreEqual("tracked", asset.name);
                Assert.AreEqual("other", other.name);
                Assert.AreEqual(4, callbackCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void NoOpTimelineRecordDoesNotCaptureLaterUnrelatedUndo()
        {
            var other = ScriptableObject.CreateInstance<TimelineAsset>();
            var callbackCount = 0;
            using var uxUndo = new UxUndo();
            try
            {
                uxUndo.RegUndo("timeline_no_op", asset, () => callbackCount++);
                uxUndo.CompleteUndo();

                Undo.RecordObject(other, "unrelated_after_no_op");
                other.name = "changed";
                Undo.FlushUndoRecordObjects();

                Undo.PerformUndo();
                Assert.AreEqual(0, callbackCount,
                    "没有产生差异的 Timeline 记录不得与后续无关对象共享 Undo group。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void UndoAfterSourceReplacementRefreshesCurrentAdapters()
        {
            using var uxUndo = new UxUndo();
            var document = new TimelineEditorDocument();
            TimelineAssetEditorSource CreateSource()
            {
                return new TimelineAssetEditorSource(
                    asset,
                    (key, owner, _) => uxUndo.RegUndo(
                        key,
                        owner,
                        () => document.RefreshAfterUndo(owner)),
                    () => { },
                    completeUndo: uxUndo.CompleteUndo);
            }

            var originalSource = CreateSource();
            document.SetSource(originalSource);
            Assert.IsNotNull(originalSource.AddTrack(typeof(AnimationTrackAsset)));
            Assert.AreEqual(1, asset.tracks.Count);

            var currentSource = CreateSource();
            document.SetSource(currentSource);
            Assert.AreEqual(1, currentSource.TrackCount);

            Undo.PerformUndo();
            Assert.AreEqual(0, asset.tracks.Count);
            Assert.AreEqual(0, currentSource.TrackCount,
                "Undo 必须刷新当前 Document source，而不是已被替换的旧 source 实例。");
        }

        [Test]
        public void CoreViewsDoNotReachThroughTimelineWindowAsset()
        {
            var paths = new[]
            {
                "Assets/Editor/Timeline/TimelineTrackView.cs",
                "Assets/Editor/Timeline/TimelineTrackItem.cs",
                "Assets/Editor/Timeline/TimelineClipItem.cs",
                "Assets/Editor/Timeline/TimelineClipView.cs",
                "Assets/Editor/Timeline/TimelineInspectorView.cs",
            };

            foreach (var path in paths)
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                Assert.IsNotNull(script, path);
                StringAssert.DoesNotContain("TimelineWindow.Asset", script.text, path);
                StringAssert.DoesNotContain("TimelineAssetEditorSource", script.text, path);
                StringAssert.DoesNotContain("TimelineAssetEditorTrack", script.text, path);
                StringAssert.DoesNotContain("TimelineAssetEditorClip", script.text, path);
                StringAssert.DoesNotContain("TimelineTrackAsset", script.text, path);
                StringAssert.DoesNotContain("TimelineClipAsset", script.text, path);
                StringAssert.DoesNotContain("CombatLogicTimelineSource", script.text, path);
                StringAssert.DoesNotContain("CombatCancelWindowEditorTrack", script.text, path);
                StringAssert.DoesNotContain("CombatCancelWindowEditorClip", script.text, path);
                StringAssert.DoesNotContain("CombatHitWindowEditorTrack", script.text, path);
                StringAssert.DoesNotContain("CombatHitWindowEditorClip", script.text, path);
            }
        }

        readonly struct UndoRecord
        {
            public UndoRecord(string key, UnityEngine.Object owner, Action callback)
            {
                Key = key;
                Owner = owner;
                Callback = callback;
            }

            public string Key { get; }
            public UnityEngine.Object Owner { get; }
            public Action Callback { get; }
        }
    }
}
