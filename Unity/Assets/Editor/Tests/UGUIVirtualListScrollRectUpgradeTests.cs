using NUnit.Framework;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualListScrollRectUpgradeTests
    {
        [Test]
        public void UpgradePreservesSettingsPersistentEventsAndSceneReferences()
        {
            Assert.IsFalse(Application.isPlaying, "升级用例必须在 Edit Mode 中执行。");
            var root = new GameObject("Upgrade Test", typeof(RectTransform), typeof(ScrollRect));
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(root.transform, false);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var host = root.AddComponent<UGUIVirtualListComponent>();
            try
            {
                var original = root.GetComponent<ScrollRect>();
                original.viewport = (RectTransform)viewport.transform;
                original.content = (RectTransform)content.transform;
                original.horizontal = true;
                original.vertical = false;
                original.movementType = ScrollRect.MovementType.Clamped;
                original.scrollSensitivity = 37;
                original.decelerationRate = 0.2f;
                original.elasticity = 0.3f;
                original.inertia = false;
                original.enabled = false;
                UnityEventTools.AddVoidPersistentListener(original.onValueChanged, original.StopMovement);
                var serialized = new SerializedObject(host);
                serialized.FindProperty("_scrollRect").objectReferenceValue = original;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var upgraded = UGUIVirtualListScrollRectUpgrade.Upgrade(original);
                Assert.AreSame(upgraded, host.ScrollRect);
                Assert.AreSame(upgraded, root.GetComponent<ScrollRect>());
                Assert.AreEqual(1, root.GetComponents<ScrollRect>().Length);
                Assert.AreSame(viewport.transform, upgraded.viewport);
                Assert.AreSame(content.transform, upgraded.content);
                Assert.IsTrue(upgraded.horizontal);
                Assert.IsFalse(upgraded.vertical);
                Assert.AreEqual(ScrollRect.MovementType.Clamped, upgraded.movementType);
                Assert.AreEqual(37, upgraded.scrollSensitivity);
                Assert.AreEqual(0.2f, upgraded.decelerationRate);
                Assert.AreEqual(0.3f, upgraded.elasticity);
                Assert.IsFalse(upgraded.inertia);
                Assert.IsFalse(upgraded.enabled);
                Assert.AreEqual(1, upgraded.onValueChanged.GetPersistentEventCount());
                Assert.AreSame(upgraded, upgraded.onValueChanged.GetPersistentTarget(0));
                Assert.AreSame(upgraded, UGUIVirtualListScrollRectUpgrade.Upgrade(upgraded));

                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.AreEqual(typeof(ScrollRect), host.ScrollRect.GetType());
                Assert.IsNull(root.GetComponent<UGUIVirtualListScrollRect>());
                Undo.PerformRedo();
                Assert.IsInstanceOf<UGUIVirtualListScrollRect>(host.ScrollRect);
                Assert.AreSame(content.transform, host.ScrollRect.content);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
