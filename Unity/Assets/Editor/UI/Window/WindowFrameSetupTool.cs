#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 窗口框架的一键工具。全部走 PrefabUtility，不手写 YAML，保证 fileID / PrefabInstance 正确。
    ///
    /// 流程：
    /// 1. 「1. 创建 CommonWindowFrame 预制」→ 生成 框架根 + panelTitleBar；
    /// 2. 选中窗口预制（如 BagWindow.prefab）→「2. 把框架挂到选中窗口」。
    /// </summary>
    public static class WindowFrameSetupTool
    {
        private const string FramePrefabPath = "Assets/GameRes/Res/UI/Common/Prefab/CommonWindowFrame.prefab";
        private const string BodyNodeName = "panelBody";

        [MenuItem("UxGame/工具/UI/窗口/1. 创建 CommonWindowFrame 预制", false, 200)]
        public static void CreateFramePrefab()
        {
            if (File.Exists(FramePrefabPath) &&
                !EditorUtility.DisplayDialog("创建窗口框架",
                    $"已存在：\n{FramePrefabPath}\n\n是否覆盖？", "覆盖", "取消"))
                return;

            var root = new GameObject("CommonWindowFrame", typeof(RectTransform));
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var titleBar = new GameObject("panelTitleBar", typeof(RectTransform));
            var barRect = (RectTransform)titleBar.transform;
            barRect.SetParent(rootRect, false);
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0f, 40f);

            // 标题文本：左侧，右边留出三个按钮的宽度。
            var title = new GameObject("txtTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            var titleRect = (RectTransform)title.transform;
            titleRect.SetParent(barRect, false);
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(8f, 0f);
            titleRect.offsetMax = new Vector2(-148f, 0f);

            var titleText = title.GetComponent<TextMeshProUGUI>();
            titleText.text = "窗口标题";
            titleText.fontSize = 20f;
            titleText.alignment = TextAlignmentOptions.Left;
            if (TMP_Settings.defaultFontAsset != null)
                titleText.font = TMP_Settings.defaultFontAsset;

            // 三个按钮：右对齐，从右往左为 关闭 / 最大化 / 最小化。
            CreateButton(barRect, "btnClose", 0);
            CreateButton(barRect, "btnMax", 1);
            CreateButton(barRect, "btnMin", 2);
            CreateButton(barRect, "btnPin", 3);

            var dir = Path.GetDirectoryName(FramePrefabPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            PrefabUtility.SaveAsPrefabAsset(root, FramePrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(FramePrefabPath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"[窗口框架] 已创建 {FramePrefabPath}。\n" +
                      "panelTitleBar 高 40，可按实际美术调整；" +
                      "接下来选中窗口预制（如 BagWindow.prefab），执行「2. 把框架挂到选中窗口」。");
        }

        private static void CreateButton(RectTransform parent, string name, int index)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-8f - index * 36f, 0f);
            rect.sizeDelta = new Vector2(32f, 32f);

            // 占位图：方便在编辑器里看到位置，美术直接替换 sprite 即可。
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.25f);
        }

        [MenuItem("UxGame/工具/UI/窗口/2. 把框架挂到选中窗口", false, 201)]
        public static void AttachToSelectedWindow()
        {
            var path = GetSelectedPrefabPath();
            if (path == null) return;

            var framePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FramePrefabPath);
            if (framePrefab == null)
            {
                EditorUtility.DisplayDialog("窗口框架",
                    $"找不到框架预制：\n{FramePrefabPath}\n\n先执行「1. 创建 CommonWindowFrame 预制」。", "确定");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var body = FindChild(contents.transform, BodyNodeName);
                if (body == null)
                {
                    Debug.LogError($"[窗口框架] {path} 里找不到 {BodyNodeName} 节点。" +
                                   "Body 是硬性要求：先执行「3. 给选中窗口补 panelBody」，再挂框架。");
                    return;
                }

                if (HasFrame(body))
                {
                    Debug.LogWarning($"[窗口框架] {path} 的 {BodyNodeName} 下已有框架节点，跳过。");
                    return;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(framePrefab, body);
                instance.name = "CommonWindowFrame";
                // 放在最上层，否则内容会盖住标题栏，拖拽热区收不到射线。
                instance.transform.SetAsLastSibling();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log($"[窗口框架] 已挂到 {path} 的 {BodyNodeName} 下。\n" +
                          "注意：panelTitleBar 占顶部 40px，业务内容需要留出同等高度或调整标题栏高度。");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("UxGame/工具/UI/窗口/3. 给选中窗口补 panelBody", false, 202)]
        public static void AddPanelBody()
        {
            var path = GetSelectedPrefabPath();
            if (path == null) return;

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (FindChild(contents.transform, BodyNodeName) != null)
                {
                    Debug.LogWarning($"[窗口框架] {path} 已经有 {BodyNodeName}，跳过。");
                    return;
                }

                if (!(contents.transform is RectTransform rootRect))
                {
                    Debug.LogError($"[窗口框架] {path} 的根不是 RectTransform。");
                    return;
                }

                var body = new GameObject(BodyNodeName, typeof(RectTransform));
                var bodyRect = (RectTransform)body.transform;
                bodyRect.SetParent(rootRect, false);

                // 固定锚点（不拉伸）：缩放/最大化都依赖 sizeDelta 等于尺寸。
                bodyRect.anchorMin = new Vector2(0f, 1f);
                bodyRect.anchorMax = new Vector2(0f, 1f);
                bodyRect.pivot = new Vector2(0f, 1f);
                bodyRect.sizeDelta = rootRect.rect.size;
                bodyRect.anchoredPosition = Vector2.zero;

                // 把根下原有节点搬进 panelBody，保持世界位置。
                var children = new List<Transform>();
                for (int i = 0; i < rootRect.childCount; i++)
                {
                    var child = rootRect.GetChild(i);
                    if (child != bodyRect)
                        children.Add(child);
                }
                for (int i = 0; i < children.Count; i++)
                    children[i].SetParent(bodyRect, true);

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log($"[窗口框架] 已在 {path} 里添加 {BodyNodeName}，并把原有内容移入。\n" +
                          "下一步：在生成面板把「窗口主体」绑定到 panelBody（或依赖 panelBody 字段名兜底）。");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.Refresh();
        }

        private static string GetSelectedPrefabPath()
        {
            var selected = Selection.activeObject;
            var path = selected != null ? AssetDatabase.GetAssetPath(selected) : null;

            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
            {
                EditorUtility.DisplayDialog("窗口框架",
                    "请先在 Project 窗口里选中一个窗口 Prefab（如 BagWindow.prefab）。", "确定");
                return null;
            }

            var fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.EndsWith("WindowFrame"))
            {
                EditorUtility.DisplayDialog("窗口框架",
                    "选中的是框架预制本身，请选窗口预制（如 BagWindow.prefab）。", "确定");
                return null;
            }

            return path;
        }

        private static Transform FindChild(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name)
                    return child;

                var found = FindChild(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static bool HasFrame(Transform body)
        {
            for (int i = 0; i < body.childCount; i++)
            {
                if (body.GetChild(i).name.Contains("WindowFrame"))
                    return true;
            }
            return false;
        }
    }
}
#endif
