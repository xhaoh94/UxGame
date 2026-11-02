using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor
{
    [UnityEditor.CustomEditor(typeof(Ux.UIVirtualListComponent))]
    [UnityEditor.CanEditMultipleObjects]
    public sealed class UIVirtualListComponentEditor : UnityEditor.Editor
    {
        private static readonly string[] SizeModes = { "固定", "变化" };
        private static readonly string[] ListTypes = { "普通列表", "树形列表", "分页列表", "分组列表" };

        private void OnEnable()
        {
            foreach (var item in targets)
            {
                if (!(item is UIVirtualListComponent component) || component == null) continue;
                var settings = new SerializedObject(item);
                bool changed = false;
                var configured = settings.FindProperty("_listTypeConfigured");
                if (!configured.boolValue && (!Application.isPlaying || !component.IsInitialized))
                {
                    var listType = settings.FindProperty("_listType");
                    if (listType.intValue == (int)UIVirtualListType.Flat && HasLegacyTreeDemo(component))
                        listType.intValue = (int)UIVirtualListType.Tree;
                    configured.boolValue = true;
                    changed = true;
                }
                if (changed) settings.ApplyModifiedProperties();
            }
        }

        private static bool HasLegacyTreeDemo(UIVirtualListComponent component)
        {
            foreach (var demo in component.GetComponentsInParent<Ux.UI.UGUIVirtualTreeDemo>(true))
                if (IsDemoFor(demo, component)) return true;
            foreach (var demo in Object.FindObjectsByType<Ux.UI.UGUIVirtualTreeDemo>(FindObjectsInactive.Include))
                if (IsDemoFor(demo, component)) return true;
            return false;
        }

        private static bool IsDemoFor(Ux.UI.UGUIVirtualTreeDemo demo, UIVirtualListComponent component)
        {
            if (!demo.enabled) return false;
            var reference = new SerializedObject(demo).FindProperty("_list").objectReferenceValue;
            return reference == component || (reference == null && demo.gameObject == component.gameObject);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("脚本", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                Field("_scrollRect", "Scroll Rect", "拖入拥有 Viewport / Content 的 ScrollRect。");
                var listType = serializedObject.FindProperty("_listType");
                bool oldTypeMixed = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = listType.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                int type = EditorGUILayout.Popup("列表类型", Mathf.Clamp(listType.intValue, 0, ListTypes.Length - 1), ListTypes);
                if (EditorGUI.EndChangeCheck())
                {
                    listType.intValue = type;
                    serializedObject.FindProperty("_listTypeConfigured").boolValue = true;
                }
                EditorGUI.showMixedValue = oldTypeMixed;
                bool mixedTypes = listType.hasMultipleDifferentValues;
                bool tree = !mixedTypes && listType.intValue == (int)UIVirtualListType.Tree;
                bool paged = !mixedTypes && listType.intValue == (int)UIVirtualListType.Paged;
                bool grouped = !mixedTypes && listType.intValue == (int)UIVirtualListType.Grouped;
                bool singleVertical = tree || grouped;
                if (tree || mixedTypes)
                {
                    Field("_treeToggleOnClick", "点击节点展开/折叠", "自动切换后再触发 ItemClicked；业务回调不要再 Toggle 一次。");
                    EditorGUILayout.HelpBox("树模式为竖向单列。通过 SetTreeRoots 提供数据，Renderer 接收 UIVirtualTreeItem，用 Depth 绘制缩进。此选项不会自动生成业务数据；示例可用 Create UI Tree Demo 菜单。", MessageType.Info);
                }
                if (paged || mixedTypes)
                {
                    Field("_pageSize", "每页数量", "请求页号从 0 开始，业务加载器通过 SetPageLoader 提供。");
                    Field("_autoLoadPages", "接近末尾自动加载", "只根据实际视口判断，不使用 Buffer 范围；失败后不会无限自动重试。");
                    var auto = serializedObject.FindProperty("_autoLoadPages");
                    if (auto.boolValue || auto.hasMultipleDifferentValues)
                        Field("_pagePreloadItems", "预加载项数", "距离当前已加载内容末尾还剩多少条业务数据时请求下一页，不是 UI 像素。");
                    Field("_pageStatusClick", "点击状态项加载/重试", "状态项自动处理时不触发业务 ItemClicked，避免同步返回后旧索引指向另一条数据。");
                    EditorGUILayout.HelpBox("通过 SetPageLoader 绑定异步函数。Renderer 接收 UIVirtualPageItem；尾部状态项与业务项共用模板系统，多列时状态项占一个普通单元格，不跨整行。", MessageType.Info);
                }
                if (grouped || mixedTypes)
                {
                    Field("_showEmptyGroups", "显示空分组组头", "关闭时空分组不产生可见行，分组 Id 仍可查询。");
                    Field("_stickyGroupHeaders", "组头吸顶", "创建独立显示副本，下一组到来时顶出旧组头；不移动真实组头，不拦截滚动输入。");
                    EditorGUILayout.HelpBox("分组模式为竖向单列，通过 SetGroups 提供数据。Renderer 接收 UIVirtualGroupItem，可通过 IsHeader / IsSticky 区分内容、组头与吸顶副本。", MessageType.Info);
                }                                
                if (!singleVertical) Field("_axis", "滚动方向", "Vertical：竖向滚动；Horizontal：横向滚动。");
                else EditorGUILayout.LabelField("滚动方向", grouped ? "竖向（分组单列）" : "竖向（树形单列）");
                var axis = serializedObject.FindProperty("_axis");
                bool vertical = singleVertical || axis.intValue == (int)UIVirtualListAxis.Vertical;
                string unit = axis.hasMultipleDifferentValues ? "行/列" : vertical ? "列" : "行";

                var mode = serializedObject.FindProperty("_layoutMode");
                bool oldMixed = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = mode.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                int selected = EditorGUILayout.Popup("子项尺寸", mode.intValue == 1 ? 1 : 0, SizeModes);
                if (EditorGUI.EndChangeCheck())
                    mode.intValue = selected;
                EditorGUI.showMixedValue = oldMixed;
                bool variable = mode.intValue == (int)UIVirtualListLayoutMode.VariableLinear;

                // 尺寸来源只在固定尺寸时才有意义；可变尺寸不展示。
                if (!variable)
                {
                    Field("_itemSizeMode", "尺寸来源", "Custom 自定义；FromTemplate 读取默认模板根节点尺寸。");
                    var sizeMode = serializedObject.FindProperty("_itemSizeMode");
                    if (sizeMode.hasMultipleDifferentValues || sizeMode.intValue == (int)UIVirtualListItemSizeMode.Custom)
                        Field("_itemSize", "子项尺寸", "固定尺寸下作为精确宽高。");
                }

                if (!singleVertical) Field("_allowMultiple", "启用多" + unit, "关闭就是单行/单列；开启后根据视口可用空间自动换行。");
                var multiple = serializedObject.FindProperty("_allowMultiple");
                if (!singleVertical && (multiple.boolValue || multiple.hasMultipleDifferentValues))
                {
                    Field("_gridSpacing", "间距（X 水平 / Y 垂直）", "相邻 Item 的水平间距和相邻行的垂直间距。");
                    Field("_limitGridCount", "限制最多" + unit + "数", "不勾选时只按可用空间计算；勾选后再应用数量上限。");
                    var limited = serializedObject.FindProperty("_limitGridCount");
                    if (limited.boolValue || limited.hasMultipleDifferentValues)
                        Field("_gridCount", "最多" + unit + "数", "这是上限，不是强制数量；空间不足时自动减少。");     
                }
                if (singleVertical || !multiple.boolValue || multiple.hasMultipleDifferentValues)
                    Field("_spacing", axis.hasMultipleDifferentValues ? "主轴间距" : vertical ? "垂直间距" : "水平间距", "仅在单行/单列模式下使用。");

                Field("_paddingStart", "起点留白", "滚动主轴起点的留白。");
                Field("_paddingEnd", "终点留白", "滚动主轴终点的留白。");
                Field("_bufferSize", "视口外缓冲距离(像素)", "视口前后各预留的 UI 布局距离，不是 Item 数量。");
                if (!singleVertical)
                {
                    Field("_loopEnabled", "无限循环", "单列/单行内容按周期循环；需要内容长度大于视口，且至少 3 个周期。");
                    var loop = serializedObject.FindProperty("_loopEnabled");
                    if (loop.boolValue || loop.hasMultipleDifferentValues)
                    {
                        Field("_loopRepeat", "循环周期数", "默认 3；越多两侧缓冲越充足，内容长度也越大。");
                        EditorGUILayout.HelpBox("循环列表不支持多列、树形、分组；分页会在末尾加载状态行，不建议与循环组合。点击回调传入的是真实数据索引。", MessageType.Info);
                    }
                }
                Field("_selectionMode", "选择模式", "选择按数据索引保存，滚动复用不会丢失。OnlyView 渲染时用 IsSelected 画选中态。");
                if (!singleVertical && !paged) Field("_keepEndOnChange", "在末尾时保持贴底", "数据增删或 Item 变高/视口变化前已在末尾才跟随；浏览历史时不强制跳底。");
                if (variable || mode.hasMultipleDifferentValues)
                {
                    Field("_preserveScrollAnchor", "变化时重定位可见项", "重排后仍守住首个可见 Item 的视口偏移；关闭则恢复绝对滚动位置。");
                }

                Field("_itemTemplates", "Item模板", "可混合配置场景对象和 Project Prefab。");
                Field("_defaultItemType", "默认模板", "未设置回调时使用的模板，也是自动尺寸的来源。");
                DrawEmptyTemplateCleanup();
                if (!serializedObject.isEditingMultipleObjects)
                    DrawSizeHints(!singleVertical && multiple.boolValue, vertical, variable);
            }
            serializedObject.ApplyModifiedProperties();
            DrawUpgradeHint();

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("运行中配置只读。请退出运行后修改；视口大小变化会自动重新计算容量。", MessageType.Info);
                var component = (UIVirtualListComponent)target;
                if (component.ListType == UIVirtualListType.Tree && component.TryGetTreeAdapter(out var adapter))
                {
                    EditorGUILayout.LabelField("树节点总数", adapter.TotalNodeCount.ToString());
                    EditorGUILayout.LabelField("展开后的行数", adapter.VisibleCount.ToString());
                    if (!adapter.OwnsCurrentData)
                        EditorGUILayout.HelpBox("树投影已被底层直接数据调用替换，请重新 SetTreeRoots。", MessageType.Warning);
                }
                else if (component.ListType == UIVirtualListType.Grouped && component.TryGetGroupAdapter(out var groups))
                {
                    EditorGUILayout.LabelField("分组数", groups.GroupCount.ToString());
                    EditorGUILayout.LabelField("业务条数", groups.DataCount.ToString());
                    EditorGUILayout.LabelField("当前吸顶分组", component.CurrentStickyGroupId ?? "无");
                    if (!groups.OwnsCurrentData) EditorGUILayout.HelpBox("分组投影被底层直接修改，请重新 SetGroups。", MessageType.Warning);
                }
                else if (component.ListType == UIVirtualListType.Paged)
                {
                    if (component.TryGetPagedAdapter(out var pager))
                    {
                        EditorGUILayout.LabelField("已加载业务条数", pager.LoadedCount.ToString());
                        EditorGUILayout.LabelField("下一页（从0开始）", pager.NextPageIndex.ToString());
                        EditorGUILayout.LabelField("分页状态", pager.State.ToString());
                        if (!pager.OwnsCurrentData) EditorGUILayout.HelpBox("分页投影被底层修改，请 RefreshPagesAsync 重新接管。", MessageType.Warning);
                    }
                    else EditorGUILayout.HelpBox("尚未绑定分页函数，请调用 SetPageLoader，或使用 Create UI Paging Demo 菜单。", MessageType.Info);
                }
                else EditorGUILayout.LabelField("数据条数", component.Count.ToString());
                if (component.IsInitialized)
                {
                    EditorGUILayout.LabelField("实际初始尺寸", component.ResolvedItemSize.ToString("F1"));
                    if (component.AllowMultiple)
                        EditorGUILayout.LabelField("实际每行/列数量", component.ActualCountPerLine.ToString());
                }
            }
            else
            {
                DrawPreview();
            }
        }

        private int _previewCount = 12;

        private void DrawPreview()
        {
            if (serializedObject.isEditingMultipleObjects) return;
            EditorGUILayout.Space();            
            var component = (UIVirtualListComponent)target;
            var listType = (UIVirtualListType)serializedObject.FindProperty("_listType").intValue;
            if (listType != UIVirtualListType.Flat)
                EditorGUILayout.HelpBox("树形/分组/分页按普通列表的摆放方式预览，不包含状态项与缩进。", MessageType.None);
            _previewCount = EditorGUILayout.IntSlider("预览项数", _previewCount, 1, 50);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("生成 / 刷新预览"))
                {
                    int built = UIVirtualListEditorPreview.Build(component, _previewCount);
                    if (built == 0)
                        Debug.LogWarning("预览需要 ScrollRect 的 Content 以及至少一个 RectTransform 模板。", component);
                }
                using (new EditorGUI.DisabledScope(!UIVirtualListEditorPreview.HasPreview(component)))
                {
                    if (GUILayout.Button("清除预览"))
                        UIVirtualListEditorPreview.Clear(component);
                }
            }
            EditorGUILayout.HelpBox("预览对象带 DontSave 标记，不会写入 Prefab 或场景，进入 Play 前自动清除。预览不改 Content 尺寸，滚动范围仍是原配置。", MessageType.None);
        }

        private void DrawUpgradeHint()
        {
            var current = (UIVirtualListComponent)target;
            if (serializedObject.isEditingMultipleObjects ||
                current.ScrollRect == null || current.ScrollRect is UIVirtualListScrollRect)
                return;
            EditorGUILayout.HelpBox("若拖拽期间会增删数据、改变 Item 尺寸或视口容量，建议升级滚动组件，避免拖拽起点回跳和伪速度。", MessageType.Warning);
            using (new EditorGUI.DisabledScope(Application.isPlaying || current.ScrollRect.GetType() != typeof(ScrollRect)))
            {
                if (GUILayout.Button("升级 ScrollRect（保留配置和场景引用）"))
                {
                    UIVirtualListScrollRectUpgrade.Upgrade(current.ScrollRect);
                    serializedObject.Update();
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawSizeHints(bool multiple, bool vertical, bool variable)
        {
            Vector2 size = serializedObject.FindProperty("_itemSize").vector2Value;
            if (serializedObject.FindProperty("_itemSizeMode").intValue == (int)UIVirtualListItemSizeMode.FromTemplate)
            {
                var entries = serializedObject.FindProperty("_itemTemplates");
                int id = serializedObject.FindProperty("_defaultItemType").intValue;
                GameObject template = null;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("TypeId").intValue == id)
                    {
                        template = entry.FindPropertyRelative("Template").objectReferenceValue as GameObject;
                        break;
                    }
                }
                if (template == null || !(template.transform is RectTransform rect))
                {
                    EditorGUILayout.HelpBox("请为默认类型指定一个 RectTransform 模板，以便读取尺寸。", MessageType.Warning);
                    return;
                }
                size = rect.rect.size;
            }
            if (size.x <= 0f || size.y <= 0f || float.IsNaN(size.x) || float.IsNaN(size.y) ||
                float.IsInfinity(size.x) || float.IsInfinity(size.y))
            {
                EditorGUILayout.HelpBox("Item 宽高必须为有限正数。零尺寸模板请设置根尺寸，或改用 Custom。", MessageType.Error);
                return;
            }
            if (!multiple)
                return;
            var spacing = serializedObject.FindProperty("_gridSpacing").vector2Value;
            bool limited = serializedObject.FindProperty("_limitGridCount").boolValue;
            int cap = limited ? serializedObject.FindProperty("_gridCount").intValue : 0;
            if ((limited && cap < 1) || spacing.x < 0 || spacing.y < 0 || float.IsNaN(spacing.x) ||
                float.IsNaN(spacing.y) || float.IsInfinity(spacing.x) || float.IsInfinity(spacing.y))
            {
                EditorGUILayout.HelpBox("数量上限至少为 1，间距必须为有限非负数。", MessageType.Error);
                return;
            }
            var scroll = serializedObject.FindProperty("_scrollRect").objectReferenceValue as ScrollRect;
            if (scroll == null || scroll.viewport == null)
                return;
            float available = vertical ? scroll.viewport.rect.width : scroll.viewport.rect.height;
            float cross = vertical ? size.x : size.y;
            int count = UIFlowLayout.CalculateCountPerLine(available, cross, vertical ? spacing.x : spacing.y, true, cap);
            EditorGUILayout.LabelField(vertical ? "当前视口可排列数" : "当前视口可排行数", count.ToString());
            if (available > 0f && cross > available)
                EditorGUILayout.HelpBox("单个 Item 已超过视口空间，将保留一行/列且可能被裁剪；布局不会自动缩小 Item。", MessageType.Warning);
        }

        private void DrawEmptyTemplateCleanup()
        {
            var entries = serializedObject.FindProperty("_itemTemplates");
            int empty = 0;
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("Template").objectReferenceValue == null)
                    empty++;
            if (empty == 0) return;
            EditorGUILayout.HelpBox($"有 {empty} 个模板槽位没有指定对象，它们不会参与预览或运行。", MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("清理空模板", EditorStyles.miniButton, GUILayout.Width(96f)))
                {
                    for (int i = entries.arraySize - 1; i >= 0; i--)
                        if (entries.GetArrayElementAtIndex(i)
                                .FindPropertyRelative("Template").objectReferenceValue == null)
                            entries.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void Field(string name, string label, string tooltip)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(name), new GUIContent(label, tooltip), true);
        }
    }
}
