#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 单个 Prefab 的 UI 绑定代码生成面板。
    /// 左侧是节点树：逐级缩进、可折叠、带层级引导线；
    /// 右侧是固定列：生成、类型、字段名、事件、规则，列位置不随层级移动。
    /// </summary>
    public sealed class UICodeGenWindow : EditorWindow
    {
        private const string PrefsPrefix = "Ux.UI.CodeGen.";

        // ---------------------------------------------------------------- 布局常量

        private const float IndentStep = 18f;

        /// <summary>列宽基准值：窗口变宽时按比例放大，取值见 CalcColumns。</summary>
        private const float BaseNodeWidth = 330f;
        private const float BaseTypeWidth = 150f;
        private const float BaseFieldWidth = 170f;
        private const float BaseEventWidth = 220f;

        // 缩到最窄也不能低于这些值，否则下拉框和字段名会挤到看不清。
        private const float MinNodeWidth = 200f;
        private const float MinTypeWidth = 110f;
        private const float MinFieldWidth = 130f;
        private const float MinEventWidth = 150f;

        private const float SourceWidth = 64f;
        private const float GenerateWidth = 44f;
        private const float RowGap = 2f;

        /// <summary>当前帧实际的列宽（不含固定宽度的「规则」「生成」两列）。</summary>
        private struct Columns
        {
            public float Node;
            public float Type;
            public float Field;
            public float Event;
        }

        private Columns _columns;

        /// <summary>按窗口宽度等比放大四个可伸缩列；窄到放不下时整体压缩，避免控件互相叠住。</summary>
        private static Columns CalcColumns(float windowWidth)
        {
            // 预留一个竖滚动条的宽度；「生成」列会拉伸到行尾，所以不留额外空白。
            var available = windowWidth - SourceWidth - GenerateWidth - 16f;
            var baseSum = BaseNodeWidth + BaseTypeWidth + BaseFieldWidth + BaseEventWidth;

            var scale = baseSum > 0f ? available / baseSum : 1f;
            var columns = new Columns
            {
                Node = Mathf.Max(MinNodeWidth, BaseNodeWidth * scale),
                Type = Mathf.Max(MinTypeWidth, BaseTypeWidth * scale),
                Field = Mathf.Max(MinFieldWidth, BaseFieldWidth * scale),
                Event = Mathf.Max(MinEventWidth, BaseEventWidth * scale),
            };

            var total = columns.Node + columns.Type + columns.Field + columns.Event;
            if (total <= available || total <= 0f) return columns;

            var shrink = available / total;
            columns.Node *= shrink;
            columns.Type *= shrink;
            columns.Field *= shrink;
            columns.Event *= shrink;
            return columns;
        }

        private static readonly Color ZebraColor = new Color(1f, 1f, 1f, 0.035f);
        private static readonly Color GuideColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        private static readonly Color ChildNameColor = new Color(1f, 1f, 1f, 0.85f);

        private static readonly string[] BaseFieldTypes =
        {
            "GameObject",
            "TMP_Text",
            "Text",
            "Button",
            "Toggle",
            "Image",
            "RawImage",
            "TMP_InputField",
            "InputField",
            "RectTransform",
            "UIList",
            "UIProgressBar",
        };

        // ---------------------------------------------------------------- 状态

        private string _prefabPath;
        private GameObject _prefab;
        private List<UGUIMemberData> _members = new List<UGUIMemberData>();
        private string _baseType;
        private string _namespaceName = "Ux.UI";
        private string _outputRoot;
        private Vector2 _scroll;
        private bool _showOnlyManual = false;

        /// <summary>页面基类固定字段的显式绑定：槽位 → 成员 PathKey。没配的槽位不生成 override。</summary>
        private readonly Dictionary<UIOverrideSlot, string> _bindings =
            new Dictionary<UIOverrideSlot, string>();

        /// <summary>收起的父节点路径（PathKey）。</summary>
        private readonly HashSet<string> _collapsed = new HashSet<string>(StringComparer.Ordinal);

        private List<string> _fieldTypes;
        private string[] _fieldTypesArray;

        // ---------------------------------------------------------------- 入口

        [MenuItem("Assets/UxGame/生成 UI 绑定代码", false, 1000)]
        private static void OpenFromContextMenu()
        {
            var folders = new List<string>();
            var prefabs = new List<string>();
            CollectSelection(folders, prefabs);
            Debug.Log($"[UI 代码生成] 选择：文件夹 {folders.Count} 个，Prefab {prefabs.Count} 个" +
                      (folders.Count > 0 ? $"，目录：{string.Join(", ", folders)}" : string.Empty));

            if (folders.Count > 0 || prefabs.Count > 1)
            {
                GenerateSelection(folders, prefabs);
                return;
            }

            if (prefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("生成 UI 绑定代码", "请在 Project 中选中一个 Prefab 或文件夹。", "确定");
                return;
            }

            Open(prefabs[0]);
        }

        [MenuItem("Assets/UxGame/生成 UI 绑定代码", true)]
        private static bool ValidateOpenFromContextMenu()
        {
            var folders = new List<string>();
            var prefabs = new List<string>();
            CollectSelection(folders, prefabs);
            return folders.Count > 0 || prefabs.Count > 0;
        }

        private static void CollectSelection(List<string> folders, List<string> prefabs)
        {
            var folderSet = new HashSet<string>(StringComparer.Ordinal);
            var prefabSet = new HashSet<string>(StringComparer.Ordinal);

            var objects = Selection.objects;
            for (int i = 0; i < objects.Length; i++)
            {
                var path = AssetDatabase.GetAssetPath(objects[i]);
                if (string.IsNullOrEmpty(path)) continue;

                if (AssetDatabase.IsValidFolder(path))
                {
                    if (folderSet.Add(path)) folders.Add(path);
                }
                else if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) && prefabSet.Add(path))
                {
                    prefabs.Add(path);
                }
            }

            // 只在 Project 窗口里回退：否则顶部 Assets 菜单在别的窗口点会拿 "Assets" 当目录，扫全工程。
            if (objects.Length > 0 || !IsProjectWindowFocused()) return;

            // 左树的文件夹只是导航状态，右键时 Selection 是空的；退回 Project 窗口当前打开的目录。
            var active = GetActiveProjectFolder();
            if (!string.IsNullOrEmpty(active) && AssetDatabase.IsValidFolder(active))
                folders.Add(active);
        }

        private static bool IsProjectWindowFocused()
        {
            var focused = EditorWindow.focusedWindow;
            return focused != null && focused.GetType().Name == "ProjectBrowser";
        }

        private static MethodInfo _getActiveFolderPath;

        /// <summary>Project 窗口当前打开的目录。Unity 只把这方法做成 internal，只能反射。</summary>
        private static string GetActiveProjectFolder()
        {
            try
            {
                _getActiveFolderPath ??= typeof(ProjectWindowUtil).GetMethod("GetActiveFolderPath",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return _getActiveFolderPath?.Invoke(null, null) as string;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>文件夹递归 + 直接选中的 prefab 一起批量生成，沿用面板里保存过的字段配置。</summary>
        private static void GenerateSelection(List<string> folders, List<string> prefabs)
        {
            var roots = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < folders.Count; i++)
            {
                foreach (var file in Directory.GetFiles(folders[i], "*.prefab", SearchOption.AllDirectories))
                {
                    var path = file.Replace('\\', '/');
                    if (seen.Add(path)) roots.Add(path);
                }
            }

            for (int i = 0; i < prefabs.Count; i++)
                if (seen.Add(prefabs[i])) roots.Add(prefabs[i]);

            if (roots.Count == 0)
            {
                EditorUtility.DisplayDialog("生成 UI 绑定代码", "选中的位置下没有 prefab。", "确定");
                return;
            }

            roots.Sort(StringComparer.Ordinal);

            var ok = 0;
            var failed = new List<string>();
            for (int i = 0; i < roots.Count; i++)
            {
                var prefabPath = roots[i];
                try
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                        throw new InvalidOperationException("prefab 加载失败");

                    var saved = LoadSavedConfig(prefabPath);
                    var members = MergeWithSaved(saved, UIPrefabCodeGenerator.Scan(prefab));
                    UIPrefabCodeGenerator.GenerateConfigured(prefab, prefabPath,
                        UIPrefabCodeGenerator.ResolveOutputDirectory(prefabPath), "Ux.UI",
                        ResolveSavedBaseType(saved, prefabPath), members, ToBindings(saved));
                    ok++;
                }
                catch (Exception ex)
                {
                    failed.Add($"{prefabPath}: {ex.Message}");
                }
            }

            AssetDatabase.Refresh();

            if (failed.Count == 0)
            {
                Debug.Log($"[UI 代码生成] 完成，共 {ok} 个 prefab。");
                return;
            }

            foreach (var message in failed)
                Debug.LogError($"[UI 代码生成] {message}");
            EditorUtility.DisplayDialog("生成 UI 绑定代码",
                $"成功 {ok}，失败 {failed.Count}。失败详情见 Console。", "确定");
        }

        public static void Open(string prefabPath)
        {
            var window = GetWindow<UICodeGenWindow>("UI 代码生成");
            window.minSize = new Vector2(1000f, 560f);
            window.Load(prefabPath);
            window.Show();
        }

        // ---------------------------------------------------------------- 数据

        private void Load(string prefabPath)
        {
            _prefabPath = prefabPath;
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            _collapsed.Clear();

            if (_prefab == null)
            {
                _members = new List<UGUIMemberData>();
                _bindings.Clear();
                return;
            }

            var saved = LoadSavedConfig(prefabPath);
            _outputRoot = UIPrefabCodeGenerator.ResolveOutputDirectory(prefabPath);
            _namespaceName = "Ux.UI";
            _baseType = ResolveSavedBaseType(saved, prefabPath);

            EnsureFieldTypes();

            var scanned = UIPrefabCodeGenerator.Scan(_prefab);
            _members = MergeWithSaved(saved, scanned);
            LoadBindings(saved);
            foreach (var member in _members)
                if (member != null) EnsureTypeInList(member.FieldType);
        }

        private static SavedConfig LoadSavedConfig(string prefabPath)
        {
            var json = EditorPrefs.GetString(PrefsPrefix + prefabPath, string.Empty);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonUtility.FromJson<SavedConfig>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>优先用保存的基类（面板里改过），否则按文件名推断。</summary>
        private static string ResolveSavedBaseType(SavedConfig saved, string prefabPath)
        {
            if (saved != null && !string.IsNullOrEmpty(saved.BaseType) &&
                Array.IndexOf(UIPrefabCodeGenerator.SupportedBaseTypes, saved.BaseType) >= 0)
                return saved.BaseType;
            return UIPrefabCodeGenerator.ResolveBaseTypeByPath(prefabPath);
        }

        /// <summary>恢复固定字段绑定；成员已删除的悬空绑定直接丢弃。</summary>
        private void LoadBindings(SavedConfig saved)
        {
            _bindings.Clear();
            if (saved?.Bindings == null) return;

            var alive = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in _members)
                if (member != null) alive.Add(PathKey(member.Path));

            foreach (var binding in saved.Bindings)
            {
                if (binding == null || string.IsNullOrEmpty(binding.PathKey)) continue;
                if (!alive.Contains(binding.PathKey)) continue;
                _bindings[binding.Slot] = binding.PathKey;
            }
        }

        private static IReadOnlyDictionary<UIOverrideSlot, string> ToBindings(SavedConfig saved)
        {
            if (saved?.Bindings == null || saved.Bindings.Count == 0) return null;

            var map = new Dictionary<UIOverrideSlot, string>();
            foreach (var binding in saved.Bindings)
            {
                if (binding == null || string.IsNullOrEmpty(binding.PathKey)) continue;
                map[binding.Slot] = binding.PathKey;
            }

            return map;
        }

        /// <summary>优先使用上次保存的配置；节点已不存在时丢弃，新增节点沿用扫描结果。</summary>
        private static List<UGUIMemberData> MergeWithSaved(SavedConfig saved,
            List<UGUIMemberData> scanned)
        {
            if (saved?.Members == null) return scanned;

            var map = new Dictionary<string, UGUIMemberData>(StringComparer.Ordinal);
            foreach (var member in saved.Members)
                if (member != null && !string.IsNullOrEmpty(member.PathKey))
                    map[member.PathKey] = member;

            foreach (var member in scanned)
            {
                if (!map.TryGetValue(PathKey(member.Path), out var old)) continue;

                // 手动指定过类型：以保存的配置为准。
                // 否则扫描识别出 Wrapper 时用扫描结果，不被历史配置覆盖。
                var autoDetected = !string.IsNullOrEmpty(member.FieldType) &&
                                   member.FieldType != nameof(GameObject);
                if (old.ManualType)
                {
                    member.Generate = old.Generate;
                    member.FieldType = old.FieldType;
                    member.ManualType = true;
                }
                else if (autoDetected)
                {
                    member.Generate = true;
                }
                else
                {
                    member.Generate = old.Generate;
                    member.FieldType = old.FieldType;
                }

                member.FieldName = old.FieldName;
                member.Events = old.Events;
                member.DoubleClickInterval = old.DoubleClickInterval;
                member.LongPressInterval = old.LongPressInterval;
                member.LongPressStart = old.LongPressStart;
                member.LongPressEnd = old.LongPressEnd;
                member.LongPressBreak = old.LongPressBreak;
            }
            return scanned;
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(_prefabPath) || _members == null) return;

            var data = new SavedConfig
            {
                BaseType = _baseType,
                Members = new List<UGUIMemberData>(),
                Bindings = new List<SavedBinding>(),
            };
            foreach (var member in _members)
            {
                if (member == null) continue;
                member.PathKey = PathKey(member.Path);
                data.Members.Add(member);
            }

            foreach (var pair in _bindings)
                data.Bindings.Add(new SavedBinding { Slot = pair.Key, PathKey = pair.Value });

            EditorPrefs.SetString(PrefsPrefix + _prefabPath, JsonUtility.ToJson(data));
        }

        private void OnDisable() => Save();

        private static string PathKey(List<int> path) => UIPrefabCodeGenerator.PathKey(path);

        private static string ParentPathKey(List<int> path)
        {
            if (path == null || path.Count <= 1) return string.Empty;
            return UIPrefabCodeGenerator.PathKey(path.GetRange(0, path.Count - 1));
        }

        // ---------------------------------------------------------------- 类型列表

        private void EnsureFieldTypes()
        {
            if (_fieldTypes != null) return;

            _fieldTypes = new List<string>(BaseFieldTypes);
            var seen = new HashSet<string>(_fieldTypes, StringComparer.Ordinal);
            foreach (var name in UIPrefabCodeGenerator.CollectUIObjectTypeNames())
                if (seen.Add(name)) _fieldTypes.Add(name);

            _fieldTypesArray = _fieldTypes.ToArray();
        }

        private void EnsureTypeInList(string typeName)
        {
            EnsureFieldTypes();
            if (string.IsNullOrEmpty(typeName) || _fieldTypes.Contains(typeName)) return;
            _fieldTypes.Add(typeName);
            _fieldTypesArray = _fieldTypes.ToArray();
        }

        // ---------------------------------------------------------------- 绘制

        private void OnGUI()
        {
            if (_prefab == null)
            {
                EditorGUILayout.HelpBox("没有可用的 Prefab。请从 Project 右键重新打开。", MessageType.Warning);
                return;
            }

            _columns = CalcColumns(position.width);

            DrawHeader();
            DrawToolbar();
            DrawBindings();
            DrawTree();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(_prefab.name, EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("输出目录");
                EditorGUILayout.SelectableLabel(_outputRoot, EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("命名空间");
                EditorGUILayout.SelectableLabel(_namespaceName, EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var types = UIPrefabCodeGenerator.SupportedBaseTypes;
                var index = Array.IndexOf(types, _baseType);
                var next = EditorGUILayout.Popup("基类", index < 0 ? 0 : index, types,
                    GUILayout.Width(240f));
                if (next != index && next >= 0) _baseType = types[next];
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全选", EditorStyles.miniButton, GUILayout.Width(56f)))
                    foreach (var member in _members)
                        if (member != null) member.Generate = true;
                if (GUILayout.Button("全不选", EditorStyles.miniButton, GUILayout.Width(64f)))
                    foreach (var member in _members)
                        if (member != null) member.Generate = false;
                if (GUILayout.Button("恢复命名规则", EditorStyles.miniButton, GUILayout.Width(100f)))
                    RestoreRuleDefaults();
                if (GUILayout.Button(_showOnlyManual ? "显示全部节点" : "只显示已勾选",
                    EditorStyles.miniButton, GUILayout.Width(110f)))
                    _showOnlyManual = !_showOnlyManual;
                if (GUILayout.Button("全部展开", EditorStyles.miniButton, GUILayout.Width(70f)))
                    _collapsed.Clear();
                if (GUILayout.Button("全部收起", EditorStyles.miniButton, GUILayout.Width(70f)))
                    CollapseAll();

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"共 {_members.Count} 个节点", GUILayout.Width(110f));
            }
        }

        private static readonly (UIOverrideSlot Slot, string Label, string FieldType)[] DialogSlots =
        {
            (UIOverrideSlot.DialogTitle, "标题", "TMP_Text"),
            (UIOverrideSlot.DialogContent, "内容", "TMP_Text"),
            (UIOverrideSlot.DialogClose, "关闭按钮", "GameObject"),
            (UIOverrideSlot.DialogBtn1, "按钮1", "GameObject"),
            (UIOverrideSlot.DialogBtn2, "按钮2", "GameObject"),
            (UIOverrideSlot.DialogCheckbox, "勾选框", "Toggle"),
        };

        private static readonly (UIOverrideSlot Slot, string Label, string FieldType)[] TabFrameSlots =
        {
            (UIOverrideSlot.TabList, "页签列表", "UIList"),
            (UIOverrideSlot.TabContent, "页签内容", "RectTransform"),
            (UIOverrideSlot.TabClose, "关闭按钮", "GameObject"),
        };

        /// <summary>按当前基类显示对应的固定字段选择区；没选槽位的就不生成。</summary>
        private void DrawBindings()
        {
            var slots = _baseType == "UIDialog" ? DialogSlots
                : _baseType == "UITabFrame" ? TabFrameSlots
                : null;
            if (slots == null) return;

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"{_baseType} 固定字段（留空则不生成对应 override）",
                    EditorStyles.boldLabel);
                for (int i = 0; i < slots.Length; i++)
                    DrawSlotRow(slots[i].Slot, slots[i].Label, slots[i].FieldType);
            }
        }

        private void DrawSlotRow(UIOverrideSlot slot, string label, string fieldType)
        {
            var candidates = new List<UGUIMemberData>();
            for (int i = 0; i < _members.Count; i++)
            {
                var member = _members[i];
                if (member == null || !member.Generate || member.FieldType != fieldType) continue;
                candidates.Add(member);
            }

            var options = new string[candidates.Count + 1];
            options[0] = "（不生成）";

            _bindings.TryGetValue(slot, out var currentKey);
            var current = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                options[i + 1] = candidates[i].FieldName;
                if (current == 0 && !string.IsNullOrEmpty(currentKey) &&
                    string.Equals(PathKey(candidates[i].Path), currentKey, StringComparison.Ordinal))
                    current = i + 1;
            }

            var next = EditorGUILayout.Popup(label, current, options, GUILayout.Width(320f));
            if (next == current) return;

            if (next <= 0)
            {
                _bindings.Remove(slot);
                return;
            }

            // 同一字段只能占一个槽位，否则会生成两个 override 指向同一个字段。
            var pickedKey = PathKey(candidates[next - 1].Path);
            var conflict = new List<UIOverrideSlot>();
            foreach (var pair in _bindings)
                if (pair.Key != slot && string.Equals(pair.Value, pickedKey, StringComparison.Ordinal))
                    conflict.Add(pair.Key);
            for (int i = 0; i < conflict.Count; i++)
                _bindings.Remove(conflict[i]);

            _bindings[slot] = pickedKey;
        }

        private void RestoreRuleDefaults()
        {
            EditorPrefs.DeleteKey(PrefsPrefix + _prefabPath);
            _collapsed.Clear();
            _bindings.Clear();
            _members = UIPrefabCodeGenerator.Scan(_prefab);
            foreach (var member in _members)
                if (member != null) EnsureTypeInList(member.FieldType);
        }

        private void CollapseAll()
        {
            _collapsed.Clear();
            foreach (var parent in BuildParentSet()) _collapsed.Add(parent);
        }

        // ---------------------------------------------------------------- 节点树

        private HashSet<string> BuildParentSet()
        {
            var parents = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in _members)
            {
                if (member == null) continue;
                var parent = ParentPathKey(member.Path);
                if (!string.IsNullOrEmpty(parent)) parents.Add(parent);
            }
            return parents;
        }

        /// <summary>被设成 Wrapper 的节点，其内部节点不单独显示。</summary>
        private List<string> BuildWrapperPrefixes()
        {
            var prefixes = new List<string>();
            foreach (var member in _members)
            {
                if (member == null || !member.Generate) continue;
                if (UIPrefabCodeGenerator.IsUIObjectType(member.FieldType))
                    prefixes.Add(PathKey(member.Path) + "/");
            }
            return prefixes;
        }

        private void DrawTree()
        {
            DrawTableHeader();

            var parents = BuildParentSet();
            var wrapperPrefixes = BuildWrapperPrefixes();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (var i = 0; i < _members.Count; i++)
            {
                var member = _members[i];
                if (member == null) continue;
                if (IsHiddenByCollapse(member.Path)) continue;
                if (IsInsideWrapper(member, wrapperPrefixes)) continue;
                if (_showOnlyManual && !member.Generate) continue;

                DrawRow(member, i, parents);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawTableHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("节点", EditorStyles.toolbarButton, GUILayout.Width(_columns.Node));
                GUILayout.Label("类型", EditorStyles.toolbarButton, GUILayout.Width(_columns.Type));
                GUILayout.Label("字段名", EditorStyles.toolbarButton, GUILayout.Width(_columns.Field));
                GUILayout.Label("事件", EditorStyles.toolbarButton, GUILayout.Width(_columns.Event));
                GUILayout.Label("规则", EditorStyles.toolbarButton, GUILayout.Width(SourceWidth));
                // 不设固定宽度：占满剩余空间，窗口变宽时不会在右侧留空白。
                GUILayout.Label("生成", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            }
        }

        private void DrawRow(UGUIMemberData member, int rowIndex, HashSet<string> parents)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var row = EditorGUILayout.GetControlRect(false, lineHeight + RowGap);
            row.height = lineHeight;

            if ((rowIndex & 1) == 1)
                EditorGUI.DrawRect(row, ZebraColor);

            var key = PathKey(member.Path);
            var depth = Mathf.Max(0, (member.Path?.Count ?? 1) - 1);
            var hasChildren = parents.Contains(key) &&
                              !UIPrefabCodeGenerator.IsUIObjectType(member.FieldType);
            var collapsed = _collapsed.Contains(key);

            // 缩进：最多到节点区还能放下名字为止。
            var indent = Mathf.Min(depth * IndentStep, _columns.Node - 140f);

            // 层级引导线：每一级祖先一条竖线。
            for (var level = 1; level <= depth; level++)
            {
                var lineX = row.x + (level - 1) * IndentStep + IndentStep * 0.5f;
                if (lineX > row.x + indent) break;
                EditorGUI.DrawRect(new Rect(lineX, row.y, 1f, lineHeight), GuideColor);
            }

            // 折叠三角跟着缩进，节点名紧挨它。
            var foldX = row.x + indent;
            if (hasChildren)
            {
                var expanded = !collapsed;
                var next = EditorGUI.Foldout(new Rect(foldX, row.y, IndentStep, lineHeight),
                    expanded, GUIContent.none, true);
                if (next != expanded)
                {
                    if (!_collapsed.Remove(key)) _collapsed.Add(key);
                    Repaint();
                }
            }
            else if (depth > 0)
            {
                // 叶子节点画一小段横线连到父级。
                EditorGUI.DrawRect(
                    new Rect(foldX + IndentStep * 0.5f, row.y + lineHeight * 0.5f,
                        IndentStep * 0.5f, 1f), GuideColor);
            }

            var nameX = foldX + IndentStep;
            var nameWidth = Mathf.Max(60f, _columns.Node - indent - IndentStep);
            var previousColor = GUI.color;
            if (depth > 0) GUI.color = ChildNameColor;
            GUI.Label(new Rect(nameX, row.y, nameWidth, lineHeight), member.NodeName ?? string.Empty);
            GUI.color = previousColor;

            var x = row.x + _columns.Node;

            EnsureTypeInList(member.FieldType);
            var typeIndex = Array.IndexOf(_fieldTypesArray, member.FieldType);
            var nextType = EditorGUI.Popup(new Rect(x, row.y, _columns.Type - 6f, lineHeight),
                typeIndex < 0 ? 0 : typeIndex, _fieldTypesArray);
            if (nextType >= 0 && _fieldTypesArray[nextType] != member.FieldType)
            {
                member.FieldType = _fieldTypesArray[nextType];
                member.ManualType = true;
            }
            x += _columns.Type;

            member.FieldName = EditorGUI.TextField(new Rect(x, row.y, _columns.Field - 6f, lineHeight),
                member.FieldName ?? string.Empty);
            x += _columns.Field;

            member.Events = (UIMemberEvents)EditorGUI.EnumFlagsField(
                new Rect(x, row.y, _columns.Event - 6f, lineHeight), member.Events);
            x += _columns.Event;

            // 表头是居中的 toolbarButton，内容也要居中，否则视觉上偏左。
            var source = member.ManualType ? "手动" : member.MatchedByRule ? "命中" : "待选";
            GUI.Label(new Rect(x, row.y, SourceWidth, lineHeight), source, EditorStyles.centeredGreyMiniLabel);
            x += SourceWidth;

            var generateWidth = Mathf.Max(GenerateWidth - 6f, row.xMax - x - 4f);
            const float toggleSize = 16f;
            member.Generate = EditorGUI.Toggle(
                new Rect(x + (generateWidth - toggleSize) * 0.5f, row.y, toggleSize, lineHeight),
                member.Generate);

            DrawEventParameters(member);
        }

        private void DrawEventParameters(UGUIMemberData member)
        {
            if ((member.Events & UIMemberEvents.DoubleClick) != 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(_columns.Node + 40f);
                    GUILayout.Label("双击间隔", EditorStyles.miniLabel, GUILayout.Width(60f));
                    member.DoubleClickInterval = EditorGUILayout.FloatField(
                        member.DoubleClickInterval, GUILayout.Width(56f));
                    GUILayout.Label("秒", EditorStyles.miniLabel, GUILayout.Width(16f));
                    GUILayout.FlexibleSpace();
                }
            }

            if ((member.Events & UIMemberEvents.LongPress) != 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(_columns.Node + 40f);
                    GUILayout.Label("间隔", EditorStyles.miniLabel, GUILayout.Width(32f));
                    member.LongPressInterval = EditorGUILayout.FloatField(
                        member.LongPressInterval, GUILayout.Width(48f));

                    GUILayout.Label("起始", EditorStyles.miniLabel, GUILayout.Width(32f));
                    member.LongPressStart = EditorGUILayout.IntField(
                        member.LongPressStart, GUILayout.Width(44f));

                    GUILayout.Label("最大", EditorStyles.miniLabel, GUILayout.Width(32f));
                    member.LongPressEnd = EditorGUILayout.IntField(
                        member.LongPressEnd, GUILayout.Width(44f));

                    GUILayout.Label("中断", EditorStyles.miniLabel, GUILayout.Width(32f));
                    member.LongPressBreak = EditorGUILayout.FloatField(
                        member.LongPressBreak, GUILayout.Width(48f));
                    GUILayout.FlexibleSpace();
                }
            }
        }

        private bool IsHiddenByCollapse(List<int> path)
        {
            if (path == null || _collapsed.Count == 0) return false;
            for (var length = path.Count - 1; length > 0; length--)
                if (_collapsed.Contains(UIPrefabCodeGenerator.PathKey(path.GetRange(0, length))))
                    return true;
            return false;
        }

        private static bool IsInsideWrapper(UGUIMemberData member, List<string> wrapperPrefixes)
        {
            if (wrapperPrefixes.Count == 0) return false;
            var key = PathKey(member.Path);
            foreach (var prefix in wrapperPrefixes)
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            return false;
        }

        // ---------------------------------------------------------------- 生成

        private void DrawFooter()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                var duplicate = FindDuplicateFieldName();
                if (duplicate != null)
                    EditorGUILayout.HelpBox($"字段名重复：{duplicate}", MessageType.Error);

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("生成", GUILayout.Width(120f), GUILayout.Height(26f)))
                {
                    if (duplicate != null)
                    {
                        EditorUtility.DisplayDialog("生成失败", $"字段名重复：{duplicate}", "确定");
                        return;
                    }

                    Save();
                    try
                    {
                        var path = UIPrefabCodeGenerator.GenerateConfigured(_prefab, _prefabPath,
                            _outputRoot, _namespaceName, _baseType, _members, _bindings);
                        AssetDatabase.Refresh();
                        Debug.Log($"UI 代码生成成功：{path}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        EditorUtility.DisplayDialog("生成失败", ex.Message, "确定");
                    }
                }
            }
        }

        private string FindDuplicateFieldName()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in _members)
            {
                if (member == null || !member.Generate) continue;
                if (string.IsNullOrEmpty(member.FieldName)) return "（空字段名）";
                if (!seen.Add(member.FieldName)) return member.FieldName;
            }
            return null;
        }
    }

    [Serializable]
    internal sealed class SavedConfig
    {
        public string BaseType;
        public List<UGUIMemberData> Members;
        public List<SavedBinding> Bindings;
    }

    [Serializable]
    internal sealed class SavedBinding
    {
        public UIOverrideSlot Slot;
        public string PathKey;
    }
}
#endif
