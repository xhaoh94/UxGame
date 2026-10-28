using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor
{
    /// <summary>仅修复 Inspector 的模式与缓存，不修改组件参数或保存场景。</summary>
    public static class UGUIVirtualListInspectorTools
    {
        private static string RequestPath => Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/UGUIVirtualListInspectorRefresh.request"));

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedRefresh()
        {
            if (!File.Exists(RequestPath))
                return;
            EditorApplication.delayCall += TryRequestedRefresh;
            Selection.selectionChanged -= TryRequestedRefresh;
            Selection.selectionChanged += TryRequestedRefresh;
        }

        private static void TryRequestedRefresh()
        {
            if (!File.Exists(RequestPath))
            {
                Selection.selectionChanged -= TryRequestedRefresh;
                return;
            }
            if (!HasSelectedList())
                return;
            // 下一次脚本刷新或重新选中列表时执行一次，之后不再干预用户的 Inspector 模式。
            File.Delete(RequestPath);
            Selection.selectionChanged -= TryRequestedRefresh;
            RestoreSelectedInspector();
        }

        [MenuItem("UxGame/UI/刷新虚拟列表 Inspector")]
        public static void RestoreSelectedInspector()
        {
            if (!HasSelectedList())
            {
                Debug.LogWarning("请先选中挂有 UGUIVirtualListComponent 的 Canvas 或节点。");
                return;
            }
            var tracker = ActiveEditorTracker.sharedTracker;
            var previous = tracker.inspectorMode;
            int windows = RestoreMatchingWindows();
            tracker.inspectorMode = InspectorMode.Normal;
            tracker.ForceRebuild();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            int count = 0;
            foreach (var editor in tracker.activeEditors)
            {
                if (editor is UGUIVirtualListComponentEditor)
                    count++;
            }
            if (count > 0 || windows > 0)
                Debug.Log($"[UGUIVirtualList] Inspector 已恢复。原模式：{previous}；当前模式：Normal；已匹配统一布局 Inspector；已刷新窗口：{windows}。");
            else
                Debug.LogWarning($"[UGUIVirtualList] 已请求重建 Inspector（原模式 {previous}）。若当前 Inspector 已锁定，请先解锁并重新选中 Canvas；也请确认窗口菜单使用 Normal 而非 Debug 模式。");
        }

        private static int RestoreMatchingWindows()
        {
            int matched = 0;
            // InspectorWindow 是 Unity 内部类型，但 inspectorMode / tracker 是其公开属性。
            // 必须设置窗口本身的 mode：只修改 sharedTracker，窗口下一次重建时会恢复旧 Debug 状态。
            // 此处只做编辑器窗口适配，不读写引擎运行时私有字段。
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                var type = window.GetType();
                if (type.FullName != "UnityEditor.InspectorWindow")
                    continue;
                var mode = type.GetProperty("inspectorMode", BindingFlags.Public | BindingFlags.Instance);
                var trackerProperty = type.GetProperty("tracker", BindingFlags.Public | BindingFlags.Instance);
                if (mode == null || !mode.CanWrite || trackerProperty == null)
                    continue;
                var windowTracker = trackerProperty.GetValue(window) as ActiveEditorTracker;
                if (windowTracker == null)
                    continue;
                windowTracker.ForceRebuild();
                bool hasList = false;
                foreach (var editor in windowTracker.activeEditors)
                foreach (var item in editor.targets)
                    if (item is UGUIVirtualListComponent) hasList = true;
                if (!hasList)
                    continue;
                mode.SetValue(window, InspectorMode.Normal);
                windowTracker.ForceRebuild();
                window.Repaint();
                foreach (var editor in windowTracker.activeEditors)
                    if (editor is UGUIVirtualListComponentEditor) matched++;
            }
            return matched;
        }

        private static bool HasSelectedList()
        {
            foreach (var go in Selection.gameObjects)
                if (go != null && go.GetComponent<UGUIVirtualListComponent>() != null)
                    return true;
            return Selection.activeObject is UGUIVirtualListComponent;
        }
    }
}
