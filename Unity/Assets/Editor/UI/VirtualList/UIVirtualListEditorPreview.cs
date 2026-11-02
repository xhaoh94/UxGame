using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor
{
    /// <summary>
    /// 编辑模式预览：用模板副本 + UIFlowLayout 摆出前 N 项，不初始化真正的虚拟列表。
    /// 预览对象带 DontSave 标记，不会写入 Prefab / 场景，进入 Play 前自动清除。
    /// </summary>
    internal static class UIVirtualListEditorPreview
    {
        private const string RootName = "[UIVirtualList Preview]";
        private const string ItemName = "Preview_";

        public static bool HasPreview(UIVirtualListComponent component)
        {
            return component != null && FindRoot(component) != null;
        }

        /// <summary>生成（或刷新）预览，返回实际摆放的项数。</summary>
        public static int Build(UIVirtualListComponent component, int count)
        {
            if (component == null) return 0;
            Clear(component);

            var so = new SerializedObject(component);
            so.Update();
            var scrollRect = so.FindProperty("_scrollRect")?.objectReferenceValue as ScrollRect;
            if (scrollRect == null || scrollRect.content == null) return 0;
            var viewport = scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();
            if (viewport == null) return 0;

            var templates = CollectTemplates(so);
            if (templates.Count == 0) return 0;

            var listType = (UIVirtualListType)so.FindProperty("_listType").intValue;
            var axis = (UIVirtualListAxis)so.FindProperty("_axis").enumValueIndex;
            // 树形与分组固定竖向单列，预览与运行时保持一致。
            if (listType == UIVirtualListType.Tree || listType == UIVirtualListType.Grouped)
                axis = UIVirtualListAxis.Vertical;

            bool variable = so.FindProperty("_layoutMode").intValue ==
                (int)UIVirtualListLayoutMode.VariableLinear;
            bool allowMultiple = (listType == UIVirtualListType.Flat || listType == UIVirtualListType.Paged) &&
                so.FindProperty("_allowMultiple").boolValue;
            bool limit = allowMultiple && so.FindProperty("_limitGridCount").boolValue;
            int maxPerLine = limit ? Mathf.Max(1, so.FindProperty("_gridCount").intValue) : 0;
            float singleSpacing = so.FindProperty("_spacing").floatValue;
            Vector2 gridSpacing = so.FindProperty("_gridSpacing").vector2Value;
            Vector2 spacing = allowMultiple
                ? gridSpacing
                : axis == UIVirtualListAxis.Vertical
                    ? new Vector2(0f, singleSpacing)
                    : new Vector2(singleSpacing, 0f);
            float paddingStart = so.FindProperty("_paddingStart").floatValue;
            float paddingEnd = so.FindProperty("_paddingEnd").floatValue;
            Vector2 itemSize = ResolveItemSize(so, templates[0]);
            // 布局构造要求正数；模板未设置尺寸时给一个可预览的兜底值，不打断预览。
            if (itemSize.x <= 0f || itemSize.y <= 0f ||
                float.IsNaN(itemSize.x) || float.IsNaN(itemSize.y) ||
                float.IsInfinity(itemSize.x) || float.IsInfinity(itemSize.y))
                itemSize = new Vector2(100f, 100f);

            count = Mathf.Clamp(count, 1, 200);
            var layout = new UIFlowLayout(axis, itemSize, variable, allowMultiple, maxPerLine,
                spacing, paddingStart, paddingEnd);
            layout.SetViewportSize(viewport.rect.size);
            layout.SetItemCount(count, true);
            if (variable)
            {
                for (int i = 0; i < count; i++)
                {
                    var template = templates[i % templates.Count];
                    var templateSize = ((RectTransform)template.transform).rect.size;
                    var main = axis == UIVirtualListAxis.Vertical ? templateSize.y : templateSize.x;
                    // 主轴尺寸为 0 时保留布局的估算值，避免测量值非法打断预览。
                    if (main > 0f && !float.IsNaN(main) && !float.IsInfinity(main))
                        layout.SetItemSize(i, templateSize);
                }
            }

            var root = new GameObject(RootName, typeof(RectTransform));
            root.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild |
                HideFlags.HideInHierarchy;
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(scrollRect.content, false);
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = Vector2.zero;
            Vector2 viewportSize = viewport.rect.size;
            rootRect.sizeDelta = axis == UIVirtualListAxis.Vertical
                ? new Vector2(Mathf.Max(viewportSize.x, layout.CrossSize),
                    Mathf.Max(viewportSize.y, layout.ContentLength))
                : new Vector2(Mathf.Max(viewportSize.x, layout.ContentLength),
                    Mathf.Max(viewportSize.y, layout.CrossSize));

            for (int i = 0; i < count; i++)
            {
                var template = templates[i % templates.Count];
                var instance = Object.Instantiate(template);
                instance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild |
                    HideFlags.HideInHierarchy;
                instance.name = ItemName + i;
                var rect = (RectTransform)instance.transform;
                rect.SetParent(rootRect, false);
                var info = layout.GetItemLayout(i);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = info.Position;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, info.Size.x);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, info.Size.y);
            }

            SceneView.RepaintAll();
            return count;
        }

        public static void Clear(UIVirtualListComponent component)
        {
            var root = FindRoot(component);
            if (root != null) Object.DestroyImmediate(root.gameObject);
            SceneView.RepaintAll();
        }

        /// <summary>进入 Play 之前清理全部预览，避免残留对象混入运行时列表。</summary>
        public static void ClearAll()
        {
            foreach (var component in Object.FindObjectsByType<UIVirtualListComponent>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Clear(component);

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.prefabContentsRoot == null) return;
            foreach (var component in stage.prefabContentsRoot
                         .GetComponentsInChildren<UIVirtualListComponent>(true))
                Clear(component);
        }

        private static Transform FindRoot(UIVirtualListComponent component)
        {
            if (component == null) return null;
            var so = new SerializedObject(component);
            var scrollRect = so.FindProperty("_scrollRect")?.objectReferenceValue as ScrollRect;
            var parent = scrollRect != null && scrollRect.content != null
                ? scrollRect.content
                : component.transform;
            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == RootName && child.parent == parent) return child;
            return null;
        }

        private static List<GameObject> CollectTemplates(SerializedObject so)
        {
            var result = new List<GameObject>();
            var entries = so.FindProperty("_itemTemplates");
            if (entries == null || !entries.isArray) return result;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var template = entries.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("Template")?.objectReferenceValue as GameObject;
                if (template != null && template.transform is RectTransform &&
                    !result.Contains(template))
                    result.Add(template);
            }
            return result;
        }

        private static Vector2 ResolveItemSize(SerializedObject so, GameObject template)
        {
            bool fromTemplate = so.FindProperty("_itemSizeMode").intValue ==
                (int)UIVirtualListItemSizeMode.FromTemplate;
            if (fromTemplate && template != null && template.transform is RectTransform rect)
                return rect.rect.size;
            return so.FindProperty("_itemSize").vector2Value;
        }
    }

    /// <summary>域重载后重新挂接：进入 Play 时自动清理编辑模式预览。</summary>
    [InitializeOnLoad]
    internal static class UIVirtualListEditorPreviewHooks
    {
        static UIVirtualListEditorPreviewHooks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                UIVirtualListEditorPreview.ClearAll();
        }
    }
}
