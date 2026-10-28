using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Ux.Editor.Tests
{
    public sealed class UGUIVirtualListInspectorTests
    {
        [Test]
        public void RefreshRestoresNormalTrackerAndAutomaticallySelectedInspector()
        {
            var oldSelection = Selection.objects;
            var tracker = ActiveEditorTracker.sharedTracker;
            var oldMode = tracker.inspectorMode;
            var root = new GameObject("Inspector mode test");
            try
            {
                root.AddComponent<UGUIVirtualListComponent>();
                Selection.activeGameObject = root;
                tracker.inspectorMode = InspectorMode.Debug;
                tracker.ForceRebuild();
                UGUIVirtualListInspectorTools.RestoreSelectedInspector();
                Assert.AreEqual(InspectorMode.Normal, tracker.inspectorMode);
                bool matched = false;
                foreach (var editor in tracker.activeEditors)
                    if (editor is UGUIVirtualListComponentEditor) matched = true;
                Assert.IsTrue(matched, "切换回 Normal 后必须实际使用统一布局 Inspector。");
            }
            finally
            {
                Selection.objects = oldSelection;
                tracker.inspectorMode = oldMode;
                tracker.ForceRebuild();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RefreshChangesInspectorWindowModeSoDebugCannotComeBackOnRebuild()
        {
            var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            Assert.IsNotNull(inspectorType);
            var mode = inspectorType.GetProperty("inspectorMode");
            var trackerProperty = inspectorType.GetProperty("tracker");
            Assert.IsNotNull(mode);
            Assert.IsNotNull(trackerProperty);
            var oldSelection = Selection.objects;
            var oldMode = ActiveEditorTracker.sharedTracker.inspectorMode;
            var root = new GameObject("Inspector window mode test");
            EditorWindow window = null;
            try
            {
                root.AddComponent<UGUIVirtualListComponent>();
                Selection.activeGameObject = root;
                window = ScriptableObject.CreateInstance(inspectorType) as EditorWindow;
                window.Show();
                mode.SetValue(window, InspectorMode.Debug);
                var tracker = (ActiveEditorTracker)trackerProperty.GetValue(window);
                tracker.ForceRebuild();
                Assert.AreEqual(InspectorMode.Debug, mode.GetValue(window));
                UGUIVirtualListInspectorTools.RestoreSelectedInspector();
                Assert.AreEqual(InspectorMode.Normal, mode.GetValue(window));
                // 再次取窗口 tracker 会执行 CreateTracker，验证窗口不会把模式改回 Debug。
                tracker = (ActiveEditorTracker)trackerProperty.GetValue(window);
                tracker.ForceRebuild();
                Assert.AreEqual(InspectorMode.Normal, tracker.inspectorMode);
                bool matched = false;
                foreach (var editor in tracker.activeEditors)
                    if (editor is UGUIVirtualListComponentEditor) matched = true;
                Assert.IsTrue(matched);
            }
            finally
            {
                if (window != null) window.Close();
                Selection.objects = oldSelection;
                ActiveEditorTracker.sharedTracker.inspectorMode = oldMode;
                ActiveEditorTracker.sharedTracker.ForceRebuild();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnityAutomaticallySelectsVirtualListInspector(bool multiple)
        {
            var a = new GameObject("Inspector Test A");
            var b = new GameObject("Inspector Test B");
            UnityEditor.Editor inspector = null;
            try
            {
                var first = a.AddComponent<UGUIVirtualListComponent>();
                var second = b.AddComponent<UGUIVirtualListComponent>();
                inspector = multiple
                    ? UnityEditor.Editor.CreateEditor(new UnityEngine.Object[] { first, second })
                    : UnityEditor.Editor.CreateEditor(first);
                Assert.IsInstanceOf<UGUIVirtualListComponentEditor>(inspector,
                    "必须验证 Unity 自动选中的 Inspector，不能通过指定 editorType 绕过注册匹配。");
            }
            finally
            {
                if (inspector != null) UnityEngine.Object.DestroyImmediate(inspector);
                UnityEngine.Object.DestroyImmediate(a);
                UnityEngine.Object.DestroyImmediate(b);
            }
        }
    }
}
