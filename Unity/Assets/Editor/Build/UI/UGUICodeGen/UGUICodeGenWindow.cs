#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 单个 Prefab 的 UI 绑定代码生成面板。
    /// 左侧是节点树：逐级缩进、可折叠、带层级引导线；
    /// 右侧是固定列：生成、类型、字段名、事件、规则，列位置不随层级移动。
    /// </summary>
    public sealed class UGUICodeGenWindow : EditorWindow
    {
        private const string PrefsPrefix = "Ux.UI.CodeGen.";

        // ---------------------------------------------------------------- 布局常量

        private const float IndentStep = 18f;

        /// <summary>节点区总宽度：缩进 + 节点名。节点名紧跟在折叠三角后面。</summary>
        private const float NodeAreaWidth = 330f;

        private const float TypeWidth = 150f;
        private const float FieldWidth = 170f;
        private const float EventWidth = 220f;
        private const float SourceWidth = 64f;
        private const float GenerateWidth = 44f;
        private const float RowGap = 2f;

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

        /// <summary>收起的父节点路径（PathKey）。</summary>
        private readonly HashSet<string> _collapsed = new HashSet<string>(StringComparer.Ordinal);

        private List<string> _fieldTypes;
        private string[] _fieldTypesArray;

        // ---------------------------------------------------------------- 入口

        [MenuItem("Assets/UxGame/生成 UI 绑定代码", false, 1000)]
        private static void OpenFromContextMenu()
        {
            var path = GetSelectedPrefabPath();
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("生成 UI 绑定代码", "请在 Project 中选中一个 Prefab。", "确定");
                return;
            }
            Open(path);
        }

        [MenuItem("Assets/UxGame/生成 UI 绑定代码", true)]
        private static bool ValidateOpenFromContextMenu() => !string.IsNullOrEmpty(GetSelectedPrefabPath());


        public static void Open(string prefabPath)
        {
            var window = GetWindow<UGUICodeGenWindow>("UI 代码生成");
            window.minSize = new Vector2(1000f, 560f);
            window.Load(prefabPath);
            window.Show();
        }

        private static string GetSelectedPrefabPath()
        {
            if (Selection.objects.Length != 1) return null;
            var path = AssetDatabase.GetAssetPath(Selection.objects[0]);
            return !string.IsNullOrEmpty(path) &&
                   path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                ? path
                : null;
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
                return;
            }

            _outputRoot = UGUIPrefabCodeGenerator.ResolveOutputDirectory(prefabPath);
            _namespaceName = "Ux.UI";
            _baseType = UGUIPrefabCodeGenerator.ResolveBaseTypeByPath(prefabPath);

            EnsureFieldTypes();

            var scanned = UGUIPrefabCodeGenerator.Scan(_prefab);
            _members = MergeWithSaved(prefabPath, scanned);
            foreach (var member in _members)
                if (member != null) EnsureTypeInList(member.FieldType);
        }

        /// <summary>优先使用上次保存的配置；节点已不存在时丢弃，新增节点沿用扫描结果。</summary>
        private static List<UGUIMemberData> MergeWithSaved(string prefabPath,
            List<UGUIMemberData> scanned)
        {
            var json = EditorPrefs.GetString(PrefsPrefix + prefabPath, string.Empty);
            if (string.IsNullOrEmpty(json)) return scanned;

            SavedConfig saved;
            try
            {
                saved = JsonUtility.FromJson<SavedConfig>(json);
            }
            catch (Exception)
            {
                return scanned;
            }
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

            var data = new SavedConfig { BaseType = _baseType, Members = new List<UGUIMemberData>() };
            foreach (var member in _members)
            {
                if (member == null) continue;
                member.PathKey = PathKey(member.Path);
                data.Members.Add(member);
            }

            EditorPrefs.SetString(PrefsPrefix + _prefabPath, JsonUtility.ToJson(data));
        }

        private void OnDisable() => Save();

        private static string PathKey(List<int> path) => UGUIPrefabCodeGenerator.PathKey(path);

        private static string ParentPathKey(List<int> path)
        {
            if (path == null || path.Count <= 1) return string.Empty;
            return UGUIPrefabCodeGenerator.PathKey(path.GetRange(0, path.Count - 1));
        }

        // ---------------------------------------------------------------- 类型列表

        private void EnsureFieldTypes()
        {
            if (_fieldTypes != null) return;

            _fieldTypes = new List<string>(BaseFieldTypes);
            var seen = new HashSet<string>(_fieldTypes, StringComparer.Ordinal);
            foreach (var name in UGUIPrefabCodeGenerator.CollectUIObjectTypeNames())
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

            DrawHeader();
            DrawToolbar();
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
                var types = UGUIPrefabCodeGenerator.SupportedBaseTypes;
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

        private void RestoreRuleDefaults()
        {
            EditorPrefs.DeleteKey(PrefsPrefix + _prefabPath);
            _collapsed.Clear();
            _members = UGUIPrefabCodeGenerator.Scan(_prefab);
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
                if (UGUIPrefabCodeGenerator.IsUIObjectType(member.FieldType))
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
                GUILayout.Label("节点", EditorStyles.toolbarButton, GUILayout.Width(NodeAreaWidth));
                GUILayout.Label("类型", EditorStyles.toolbarButton, GUILayout.Width(TypeWidth));
                GUILayout.Label("字段名", EditorStyles.toolbarButton, GUILayout.Width(FieldWidth));
                GUILayout.Label("事件", EditorStyles.toolbarButton, GUILayout.Width(EventWidth));
                GUILayout.Label("规则", EditorStyles.toolbarButton, GUILayout.Width(SourceWidth));
                GUILayout.Label("生成", EditorStyles.toolbarButton, GUILayout.Width(GenerateWidth));
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
                              !UGUIPrefabCodeGenerator.IsUIObjectType(member.FieldType);
            var collapsed = _collapsed.Contains(key);

            // 缩进：最多到节点区还能放下名字为止。
            var indent = Mathf.Min(depth * IndentStep, NodeAreaWidth - 140f);

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
            var nameWidth = Mathf.Max(60f, NodeAreaWidth - indent - IndentStep);
            var previousColor = GUI.color;
            if (depth > 0) GUI.color = ChildNameColor;
            GUI.Label(new Rect(nameX, row.y, nameWidth, lineHeight), member.NodeName ?? string.Empty);
            GUI.color = previousColor;

            var x = row.x + NodeAreaWidth;

            EnsureTypeInList(member.FieldType);
            var typeIndex = Array.IndexOf(_fieldTypesArray, member.FieldType);
            var nextType = EditorGUI.Popup(new Rect(x, row.y, TypeWidth - 6f, lineHeight),
                typeIndex < 0 ? 0 : typeIndex, _fieldTypesArray);
            if (nextType >= 0 && _fieldTypesArray[nextType] != member.FieldType)
            {
                member.FieldType = _fieldTypesArray[nextType];
                member.ManualType = true;
            }
            x += TypeWidth;

            member.FieldName = EditorGUI.TextField(new Rect(x, row.y, FieldWidth - 6f, lineHeight),
                member.FieldName ?? string.Empty);
            x += FieldWidth;

            member.Events = (UGUIMemberEvents)EditorGUI.EnumFlagsField(
                new Rect(x, row.y, EventWidth - 6f, lineHeight), member.Events);
            x += EventWidth;

            var source = member.ManualType ? "手动" : member.MatchedByRule ? "命中" : "待选";
            GUI.Label(new Rect(x, row.y, SourceWidth, lineHeight), source, EditorStyles.miniLabel);
            x += SourceWidth;

            member.Generate = EditorGUI.Toggle(new Rect(x, row.y, GenerateWidth - 6f, lineHeight),
                member.Generate);

            DrawEventParameters(member);
        }

        private void DrawEventParameters(UGUIMemberData member)
        {
            if ((member.Events & UGUIMemberEvents.DoubleClick) != 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(NodeAreaWidth + 40f);
                    GUILayout.Label("双击间隔", EditorStyles.miniLabel, GUILayout.Width(60f));
                    member.DoubleClickInterval = EditorGUILayout.FloatField(
                        member.DoubleClickInterval, GUILayout.Width(56f));
                    GUILayout.Label("秒", EditorStyles.miniLabel, GUILayout.Width(16f));
                    GUILayout.FlexibleSpace();
                }
            }

            if ((member.Events & UGUIMemberEvents.LongPress) != 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(NodeAreaWidth + 40f);
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
                if (_collapsed.Contains(UGUIPrefabCodeGenerator.PathKey(path.GetRange(0, length))))
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
                        var path = UGUIPrefabCodeGenerator.GenerateConfigured(_prefab, _prefabPath,
                            _outputRoot, _namespaceName, _baseType, _members);
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
    }
}
#endif
