using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ux.UI;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnityEventSystem = UnityEngine.EventSystems.EventSystem;

namespace Ux.Editor
{
    /// <summary>更新当前打开的场景，不直接覆盖磁盘上的场景，保留用户未保存的改动。</summary>
    public static class UGUIVirtualListDemoSetup
    {
        private const string TestScenePath = "Assets/Scenes/Test.unity";
        private const string TemplateName = "[Template] UGUIVirtualListDemoItem";
        private const string TreeLeafTemplateName = "[Template] UGUIVirtualTreeLeaf";
        private const string PageStatusTemplateName = "[Template] UGUIVirtualPageStatus";
        private const string GroupItemTemplateName = "[Template] UGUIVirtualGroupItem";
        private static string RequestPath => Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/UGUIVirtualListDemoSync.request"));

        // 外部写入一次性请求后，等 Unity 完成脚本刷新，在编辑器主线程同步当前 Test 场景。
        [InitializeOnLoadMethod]
        private static void ScheduleRequestedSync()
        {
            EditorApplication.delayCall += TryRequestedSync;
        }

        private static void TryRequestedSync()
        {
            if (!File.Exists(RequestPath))
                return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                return;
            }
            if (SceneManager.GetActiveScene().path != TestScenePath)
            {
                Debug.LogWarning("UGUI 列表示例等待同步：请在 Test 场景执行 UxGame/Test/Create UGUI VirtualList Demo。");
                return;
            }
            CreateInCurrentScene();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall += TryRequestedSync;
        }

        [MenuItem("UxGame/Test/Create UGUI VirtualList Demo")]
        public static void CreateInCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请退出 Play Mode 后再同步虚拟列表示例。");
                return;
            }
            CreateInScene(SceneManager.GetActiveScene());
            if (File.Exists(RequestPath))
                File.Delete(RequestPath);
        }

        [MenuItem("UxGame/Test/Create UGUI Multi Column Demo")]
        public static void CreateFixedGridInCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请退出 Play Mode 后再同步网格示例。");
                return;
            }
            CreateInScene(SceneManager.GetActiveScene(), true);
        }

        [MenuItem("UxGame/Test/Create UGUI Tree Demo")]
        public static void CreateTreeInCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请退出 Play Mode 后再创建树形示例。");
                return;
            }
            CreateInScene(SceneManager.GetActiveScene(), tree: true);
        }

        [MenuItem("UxGame/Test/Create UGUI Paging Demo")]
        public static void CreatePagingInCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请退出 Play Mode 后再创建分页示例。");
                return;
            }
            CreateInScene(SceneManager.GetActiveScene(), paged: true);
        }

        [MenuItem("UxGame/Test/Create UGUI Grouped Demo")]
        public static void CreateGroupedInCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请退出 Play Mode 后再创建分组吸顶示例。");
                return;
            }
            CreateInScene(SceneManager.GetActiveScene(), grouped: true);
        }

        [MenuItem("UxGame/Test/Create UGUI Loop Demo")]
        public static void CreateLoopInCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请退出 Play Mode 后再创建循环示例。");
                return;
            }
            CreateInScene(SceneManager.GetActiveScene(), loop: true);
        }

        public static void CreateInTestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            CreateInScene(scene);
        }

        private static void CreateInScene(UnityScene scene, bool grid = false, bool tree = false, bool paged = false,
            bool grouped = false, bool loop = false)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new System.InvalidOperationException("当前场景无效。");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("同步 UGUI 虚拟列表示例");

            Canvas canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                    break;
            }
            if (canvas == null)
            {
                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                Undo.RegisterCreatedObjectUndo(canvasObject, "创建 Canvas");
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            EnsureEventSystem(scene);
            var component = canvas.GetComponent<UGUIVirtualListComponent>();
            if (component == null)
                component = Undo.AddComponent<UGUIVirtualListComponent>(canvas.gameObject);
            // 优先保留用户已拖入的 ScrollRect，否则复用 Canvas 下现有列表。
            var scroll = component.ScrollRect;
            if (scroll == null)
                scroll = canvas.GetComponentInChildren<ScrollRect>(true);
            if (scroll == null)
                scroll = CreateScrollRect(canvas.transform);
            if (scroll.viewport == null || scroll.content == null)
                throw new System.InvalidOperationException("现有 ScrollRect 的 Viewport / Content 未配置，请补齐后同步。");

            if (scroll.GetType() == typeof(ScrollRect))
                scroll = UGUIVirtualListScrollRectUpgrade.Upgrade(scroll);

            Undo.RecordObject(scroll, "配置滚动方向");
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
            EditorUtility.SetDirty(scroll);

            var templateTransform = scroll.content.Find(TemplateName);
            var template = templateTransform != null
                ? templateTransform.gameObject
                : CreateTemplate(scroll.content);
            ConfigureTemplate(template, grid, tree || grouped);
            if (grouped) template.GetComponentInChildren<Text>(true).text = "分组组头";

            var settings = new SerializedObject(component);
            settings.FindProperty("_scrollRect").objectReferenceValue = scroll;
            settings.FindProperty("_listType").intValue = (int)(tree ? UGUIVirtualListType.Tree
                : paged ? UGUIVirtualListType.Paged : grouped ? UGUIVirtualListType.Grouped : UGUIVirtualListType.Flat);
            settings.FindProperty("_listTypeConfigured").boolValue = true;
            settings.FindProperty("_treeToggleOnClick").boolValue = true;
            settings.FindProperty("_pageSize").intValue = 20;
            settings.FindProperty("_autoLoadPages").boolValue = true;
            settings.FindProperty("_pagePreloadItems").intValue = 5;
            settings.FindProperty("_pageStatusClick").boolValue = true;
            settings.FindProperty("_stickyGroupHeaders").boolValue = true;
            settings.FindProperty("_loopEnabled").boolValue = loop;
            settings.FindProperty("_loopRepeat").intValue = 3;
            settings.FindProperty("_showEmptyGroups").boolValue = true;
            settings.FindProperty("_layoutMode").enumValueIndex = (int)(grid || loop
                ? UGUIVirtualListLayoutMode.FixedLinear : UGUIVirtualListLayoutMode.VariableLinear);
            settings.FindProperty("_allowMultiple").boolValue = grid;
            settings.FindProperty("_limitGridCount").boolValue = grid;
            settings.FindProperty("_axis").enumValueIndex = (int)UGUIVirtualListAxis.Vertical;
            settings.FindProperty("_itemSizeMode").enumValueIndex = (int)UGUIVirtualListItemSizeMode.Manual;
            settings.FindProperty("_itemSize").vector2Value = loop ? new Vector2(620f, 120f)
                : grid ? new Vector2(148f, 110f) : new Vector2(620f, tree ? 64f : 76f);
            settings.FindProperty("_gridCount").intValue = 4;
            settings.FindProperty("_gridSpacing").vector2Value = new Vector2(8f, 8f);
            settings.FindProperty("_spacing").floatValue = 8f;
            settings.FindProperty("_paddingStart").floatValue = 12f;
            settings.FindProperty("_paddingEnd").floatValue = 12f;
            settings.FindProperty("_bufferSize").floatValue = 240f;
            settings.FindProperty("_preserveScrollAnchor").boolValue = true;
            settings.FindProperty("_keepEndOnSizeChange").boolValue = false;
            settings.FindProperty("_keepEndOnDataChange").boolValue = false;
            settings.FindProperty("_defaultItemType").intValue = 0;
            var templates = settings.FindProperty("_itemTemplates");
            templates.arraySize = tree || paged || grouped ? 2 : 1;
            var entry = templates.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("TypeId").intValue = 0;
            entry.FindPropertyRelative("Template").objectReferenceValue = template;
            if (tree || paged || grouped)
            {
                string secondName = tree ? TreeLeafTemplateName : grouped ? GroupItemTemplateName : PageStatusTemplateName;
                var leafTransform = scroll.content.Find(secondName);
                var leaf = leafTransform != null ? leafTransform.gameObject : CreateTemplate(scroll.content, secondName);
                ConfigureTemplate(leaf, false, tree, tree);
                if (paged)
                {
                    ((RectTransform)leaf.transform).sizeDelta = new Vector2(620f, 64f);
                    leaf.GetComponentInChildren<Text>(true).text = "点击加载更多";
                }
                var leafEntry = templates.GetArrayElementAtIndex(1);
                leafEntry.FindPropertyRelative("TypeId").intValue = 1;
                leafEntry.FindPropertyRelative("Template").objectReferenceValue = leaf;
            }
            settings.ApplyModifiedProperties();

            var demo = canvas.GetComponent<UGUIVirtualListDemo>();
            if (demo == null)
                demo = Undo.AddComponent<UGUIVirtualListDemo>(canvas.gameObject);
            var loopDemo = canvas.GetComponent<UGUIVirtualLoopDemo>();
            if (loop)
            {
                if (loopDemo == null) loopDemo = Undo.AddComponent<UGUIVirtualLoopDemo>(canvas.gameObject);
                var loopSettings = new SerializedObject(loopDemo);
                loopSettings.FindProperty("_list").objectReferenceValue = component;
                loopSettings.FindProperty("_snapPerPage").boolValue = true;
                loopSettings.FindProperty("_itemCount").intValue = 8;
                loopSettings.ApplyModifiedProperties();
            }
            if (loopDemo != null)
            {
                Undo.RecordObject(loopDemo, "切换循环示例");
                loopDemo.enabled = loop;
            }
            var demoSettings = new SerializedObject(demo);
            demoSettings.FindProperty("_list").objectReferenceValue = component;
            demoSettings.FindProperty("_dataCount").intValue = loop ? 8 : 1000;
            demoSettings.ApplyModifiedProperties();
            Undo.RecordObjects(new Object[] { component, demo }, "启用示例组件");
            component.enabled = true;
            demo.enabled = !tree && !paged && !grouped && !loop;
            var treeDemo = canvas.GetComponent<UGUIVirtualTreeDemo>();
            if (tree)
            {
                if (treeDemo == null) treeDemo = Undo.AddComponent<UGUIVirtualTreeDemo>(canvas.gameObject);
                var treeSettings = new SerializedObject(treeDemo);
                treeSettings.FindProperty("_list").objectReferenceValue = component;
                treeSettings.ApplyModifiedProperties();
            }
            if (treeDemo != null)
            {
                Undo.RecordObject(treeDemo, "切换树形示例");
                treeDemo.enabled = tree;
            }

            var pageDemo = canvas.GetComponent<UGUIVirtualPagingDemo>();
            if (paged)
            {
                if (pageDemo == null) pageDemo = Undo.AddComponent<UGUIVirtualPagingDemo>(canvas.gameObject);
                var pageSettings = new SerializedObject(pageDemo);
                pageSettings.FindProperty("_list").objectReferenceValue = component;
                pageSettings.ApplyModifiedProperties();
            }
            if (pageDemo != null)
            {
                Undo.RecordObject(pageDemo, "切换分页示例");
                pageDemo.enabled = paged;
            }

            var groupDemo = canvas.GetComponent<UGUIVirtualGroupDemo>();
            if (grouped)
            {
                if (groupDemo == null) groupDemo = Undo.AddComponent<UGUIVirtualGroupDemo>(canvas.gameObject);
                var groupSettings = new SerializedObject(groupDemo);
                groupSettings.FindProperty("_list").objectReferenceValue = component;
                groupSettings.ApplyModifiedProperties();
            }
            if (groupDemo != null)
            {
                Undo.RecordObject(groupDemo, "切换分组示例");
                groupDemo.enabled = grouped;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log(loop
                ? "已切换为无限循环示例：8 条数据、3 个周期、固定 620×120；首尾各回补一个周期。请保存场景后运行。"
                : grouped
                ? "已切换为分组吸顶示例：20组业务数据，竖向单列、可变高度，下一组会顶出当前吸顶 Header。请保存场景后运行。"
                : paged ? "已切换为分页示例：每页20条，模拟延迟，第三页首次失败可点击重试。请保存场景后运行。"
                : tree ? "已切换为树形示例：竖向单列、可变高度、分类/叶子两类模板，点击行展开折叠。请保存场景后运行。"
                : grid ? "已切换为多列示例：固定尺寸 / Vertical / 自动换行且最多4列 / 148×110 / 间距8×8 / 1000条数据。请保存场景后运行。"
                : "已同步 Canvas 的列表组件：VariableLinear / Vertical / 620×76 / 间距8 / 首尾12 / 缓冲240 / 1000条数据。请保存场景后运行。", canvas);
        }

        private static void EnsureEventSystem(UnityScene scene)
        {
            foreach (var system in Object.FindObjectsByType<UnityEventSystem>(FindObjectsInactive.Exclude))
                if (system.isActiveAndEnabled) return;
            var root = new GameObject("EventSystem", typeof(UnityEventSystem));
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "创建 UI EventSystem");
#if ENABLE_INPUT_SYSTEM
            var module = Undo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(root);
            module.AssignDefaultActions();
#else
            Undo.AddComponent<StandaloneInputModule>(root);
#endif
        }

        private static ScrollRect CreateScrollRect(Transform parent)
        {
            var root = new GameObject("Scroll View", typeof(RectTransform), typeof(Image), typeof(UGUIVirtualListScrollRect));
            root.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(root, "创建 ScrollRect");
            ((RectTransform)root.transform).sizeDelta = new Vector2(660f, 600f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = (RectTransform)content.transform;
            return scroll;
        }

        private static GameObject CreateTemplate(Transform parent, string name = TemplateName)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            var text = new GameObject("Text", typeof(RectTransform), typeof(Text));
            text.transform.SetParent(item.transform, false);
            Undo.RegisterCreatedObjectUndo(item, "创建 Item 模板");
            return item;
        }

        private static void ConfigureTemplate(GameObject template, bool grid, bool tree = false, bool leaf = false)
        {
            var rect = (RectTransform)template.transform;
            var image = template.GetComponent<Image>();
            var button = template.GetComponent<Button>();
            var text = template.GetComponentInChildren<Text>(true);
            if (image == null || button == null || text == null)
                throw new System.InvalidOperationException("Demo 模板需包含 Image、Button 和 Text。");

            Undo.RecordObjects(new Object[] { template, rect, image, button, text, text.rectTransform }, "配置 Item 模板");
            template.layer = 5;
            text.gameObject.layer = 5;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = grid ? new Vector2(148f, 110f) : new Vector2(620f, tree ? leaf ? 48f : 64f : 76f);
            image.color = Color.white;
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = tree && !leaf ? new Color(0.14f, 0.23f, 0.38f, 1f) : new Color(0.12f, 0.16f, 0.23f, 0.96f);
            colors.highlightedColor = new Color(0.2f, 0.3f, 0.48f, 1f);
            colors.pressedColor = new Color(0.28f, 0.42f, 0.65f, 1f);
            button.colors = colors;

            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(18f, 10f);
            text.rectTransform.offsetMax = new Vector2(-18f, -10f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = tree ? leaf ? "    叶子模板" : "[+] 分类模板" : grid ? "道具 0000\n数量：1" : "虚拟列表示例模板（运行时会克隆此对象）";
            template.SetActive(false);
        }
    }
}
