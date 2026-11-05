#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Ux;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 从 UI Prefab 生成纯 C# UI 绑定代码。
    /// 普通节点生成 GameObject；只有 UIVirtualListComponent 和 Slider 自动生成保留 Wrapper。
    /// 节点访问使用 GetChild(index) 链，不依赖名称查找和 Inspector 引用。
    /// </summary>
    public static class UIPrefabCodeGenerator
    {
        private const string DefaultOutputRoot = "Assets/Hotfix/CodeGen/UI";
        private const string ResRoot = "Assets/GameRes/Res/";
        /// <summary>代码目录只跟模块走，资源里的 Prefab 子目录不参与命名空间层级。</summary>
        private const string PrefabFolderName = "Prefab";

        /// <summary>可用于生成的基类列表。顺序即窗口下拉框顺序。</summary>
        public static readonly string[] SupportedBaseTypes =
        {
            "UIView",
            "UIWindow",
            "UITabView",
            "UITip",
            "UIDialog",
            "UIModel",
            "RTModel",
            "UITabFrame",
            "UIWindowFrame",
            "ItemRenderer",
        };

        /// <summary>按 Prefab 路径自动推断输出目录、命名空间和基类。</summary>
        public static string Generate(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var baseType = prefab != null ? ResolveBaseType(prefab.name) : "UIView";
            return Generate(prefabPath, baseType);
        }

        /// <summary>指定基类生成。</summary>
        public static string Generate(string prefabPath, string baseType)
        {
            if (string.IsNullOrEmpty(prefabPath))
                throw new ArgumentException("Prefab 路径为空。", nameof(prefabPath));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"无法加载 Prefab：{prefabPath}");

            if (string.IsNullOrEmpty(baseType))
                baseType = ResolveBaseType(prefab.name);

            return Generate(prefab, prefabPath, GetOutputDirectory(prefabPath), "Ux.UI", baseType);
        }

        /// <summary>推断某个 Prefab 将使用的输出目录。</summary>
        public static string ResolveOutputDirectory(string prefabPath) => GetOutputDirectory(prefabPath);

        /// <summary>推断某个 Prefab 将继承的基类。</summary>
        public static string ResolveBaseTypeByPath(string prefabPath)
        {
            var name = Path.GetFileNameWithoutExtension(prefabPath);
            return ResolveBaseType(name);
        }

        public static string Generate(GameObject prefab, string prefabPath,
            string outputRoot, string namespaceName, string baseType)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (string.IsNullOrEmpty(prefabPath)) throw new ArgumentException("Prefab 路径为空。", nameof(prefabPath));
            if (string.IsNullOrEmpty(outputRoot)) throw new ArgumentException("输出目录为空。", nameof(outputRoot));
            if (string.IsNullOrEmpty(namespaceName)) throw new ArgumentException("命名空间为空。", nameof(namespaceName));
            if (string.IsNullOrEmpty(baseType)) throw new ArgumentException("基类为空。", nameof(baseType));

            var members = new List<UGUIMemberData>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            CollectMembers(prefab.transform, new List<int>(), members, names);

            return GenerateConfigured(prefab, prefabPath, outputRoot, namespaceName, baseType,
                members, null);
        }

        /// <summary>按面板里勾选并调整过的成员生成代码。</summary>
        public static string GenerateConfigured(GameObject prefab, string prefabPath,
            string outputRoot, string namespaceName, string baseType,
            List<UGUIMemberData> members,
            IReadOnlyDictionary<UIOverrideSlot, string> bindings = null)
        {
            var className = SanitizeIdentifier(prefab.name, "UI");
            var selected = members != null
                ? members.FindAll(member => member.Generate && !string.IsNullOrEmpty(member.FieldType))
                : new List<UGUIMemberData>();

            // 手动把某节点改成 Wrapper 类型后，它内部的节点不再单独生成字段。
            var wrapperPrefixes = new List<string>();
            foreach (var member in selected)
                if (IsUIObjectType(member.FieldType))
                    wrapperPrefixes.Add(PathKey(member.Path) + "/");
            if (wrapperPrefixes.Count > 0)
            {
                selected.RemoveAll(member =>
                {
                    var key = PathKey(member.Path);
                    foreach (var prefix in wrapperPrefixes)
                        if (key.StartsWith(prefix, StringComparison.Ordinal))
                            return true;
                    return false;
                });
            }

            var code = BuildCode(className, namespaceName, baseType, selected, bindings);
            var outputPath = Path.Combine(outputRoot, className + ".cs").Replace('\\', '/');
            var changed = CodeFileWriter.Write(outputPath, code);
            Debug.Log(changed
                ? $"UI 代码生成成功：{outputPath}"
                : $"UI 代码无变化，跳过写入：{outputPath}");
            return outputPath;
        }

        private static readonly Dictionary<string, bool> UIObjectTypeCache =
            new Dictionary<string, bool>(StringComparer.Ordinal);

        private static List<string> _uiObjectTypeNames;

        /// <summary>类型名是否对应一个 UIObject 派生类型（UIList、UIModel、TabFrame 等 Wrapper）。</summary>
        public static bool IsUIObjectType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            if (UIObjectTypeCache.TryGetValue(typeName, out var cached)) return cached;

            var result = false;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in SafeGetTypes(assembly))
                {
                    if (type == null || type.Name != typeName) continue;
                    if (typeof(UIBase).IsAssignableFrom(type)) continue;
                    if (typeof(UIObject).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        result = true;
                        break;
                    }
                }
                if (result) break;
            }

            UIObjectTypeCache[typeName] = result;
            return result;
        }

        /// <summary>项目里所有可绑定的 Wrapper 类型名，供生成面板下拉使用。</summary>
        public static List<string> CollectUIObjectTypeNames()
        {
            if (_uiObjectTypeNames != null) return _uiObjectTypeNames;

            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in SafeGetTypes(assembly))
                {
                    if (type == null || type.IsAbstract || type == typeof(UIObject)) continue;
                    if (!typeof(UIObject).IsAssignableFrom(type)) continue;
                    // 页面类型（UIView/UIWindow/UITip/UIDialog/UITabView）不能内嵌到其他界面。
                    if (typeof(UIBase).IsAssignableFrom(type)) continue;
                    if (seen.Add(type.Name)) names.Add(type.Name);
                }
            }

            names.Sort(StringComparer.Ordinal);
            _uiObjectTypeNames = names;
            return names;
        }

        /// <summary>节点路径的字符串形式，用于判断父子关系。</summary>
        public static string PathKey(IEnumerable<int> path)
        {
            if (path == null) return string.Empty;
            var sb = new StringBuilder();
            foreach (var index in path)
            {
                if (sb.Length > 0) sb.Append('/');
                sb.Append(index);
            }
            return sb.ToString();
        }

        private static Type[] SafeGetTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                var list = new List<Type>();
                foreach (var type in ex.Types)
                    if (type != null) list.Add(type);
                return list.ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<Type>();
            }
        }

        /// <summary>
        /// 节点是 Prefab 实例时，用源 Prefab 名反查同名的 Wrapper 类型。
        /// 例如从 Common2TabFrame.prefab 拉进来的节点会识别为 Common2TabFrame。
        /// </summary>
        public static string TryGetPrefabInstanceType(GameObject gameObject)
        {
            if (gameObject == null) return null;

            var source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (source == null) return null;

            var name = source.name;
            return IsUIObjectType(name) ? name : null;
        }

        /// <summary>
        /// 扫描 Prefab 的所有可绑定节点，供生成面板展示。
        /// 命中命名规则的默认勾选，未命中的默认不勾选但仍列出，方便手动补。
        /// </summary>
        public static List<UGUIMemberData> Scan(GameObject prefab)
        {
            var list = new List<UGUIMemberData>();
            if (prefab == null) return list;
            ScanMembers(prefab.transform, new List<int>(), list,
                new HashSet<string>(StringComparer.Ordinal));
            return list;
        }

        private static void ScanMembers(Transform parent, List<int> parentPath,
            List<UGUIMemberData> list, HashSet<string> names)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (UIBindNaming.ShouldIgnore(child.name))
                    continue;

                var path = new List<int>(parentPath) { i };
                var instanceType = TryGetPrefabInstanceType(child.gameObject);
                var rule = UIBindNaming.FindRule(child.name);

                string type;
                bool matched;
                if (instanceType != null)
                {
                    // Prefab 实例优先：从 Common2TabFrame.prefab 拉进来的节点直接用 Wrapper 类型。
                    type = instanceType;
                    matched = true;
                }
                else if (rule != null && !string.IsNullOrEmpty(rule.FieldType))
                {
                    type = rule.FieldType;
                    matched = true;
                }
                else
                {
                    type = nameof(GameObject);
                    matched = false;
                }

                var fieldName = MakeUniqueFieldName(UIBindNaming.ToFieldName(child.name, type), names);

                list.Add(new UGUIMemberData
                {
                    NodeName = child.name,
                    Path = path,
                    FieldName = fieldName,
                    FieldType = type,
                    Generate = matched,
                    MatchedByRule = matched,
                    Events = UIMemberEvents.None,
                });

                // UIList 内部是运行时模板（Viewport/Content/ItemTemplate），不列出。
                // 其他 Wrapper 先扫描出来，由面板按当前类型决定是否显示。
                if (type == nameof(UIList))
                    continue;

                ScanMembers(child, path, list, names);
            }
        }

        private static void CollectMembers(Transform parent, List<int> parentPath,
            List<UGUIMemberData> members, HashSet<string> names)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);

                // _ 前缀：忽略该节点与整棵子树。
                if (UIBindNaming.ShouldIgnore(child.name))
                    continue;

                var path = new List<int>(parentPath) { i };
                // Prefab 实例优先识别为 Wrapper 类型，其次走命名规则。
                var instanceType = TryGetPrefabInstanceType(child.gameObject);
                var rule = UIBindNaming.FindRule(child.name);
                var type = instanceType ?? rule?.FieldType;
                if (type != null)
                {
                    var fieldName = MakeUniqueFieldName(UIBindNaming.ToFieldName(child.name, type), names);
                    members.Add(new UGUIMemberData { NodeName = child.name, Path = path, FieldName = fieldName, FieldType = type, Generate = true, MatchedByRule = true, Events = UIMemberEvents.None });
                }

                // UIList 内部是运行时模板，不参与页面字段。
                if (type == nameof(UIList))
                    continue;

                CollectMembers(child, path, members, names);
            }
        }

        private static string BuildCode(string className, string namespaceName, string baseType,
            List<UGUIMemberData> members,
            IReadOnlyDictionary<UIOverrideSlot, string> bindings)
        {
            var cb = new CodeBuilder();
            cb.Line("// 自动生成的代码，请勿修改!!!");
            cb.Line("using UnityEngine;");
            cb.Line("using UnityEngine.UI;");
            cb.Line("using TMPro;");
            cb.Blank();
            cb.Open($"namespace {namespaceName}");
            cb.Open($"public partial class {className} : {baseType}");

            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                cb.Line($"protected {member.FieldType} {member.FieldName};");
            }

            if (baseType == "UIModel" || baseType == "RTModel" || baseType == "UITabFrame" || baseType == "UIWindowFrame")
            {
                cb.Blank();
                cb.Line($"public {className}(GameObject gameObject, UIObject parent) : base(gameObject, parent) {{ }}");
            }

            if (baseType == "UITabFrame")
            {
                // 页签框架的固定字段同样只认显式绑定，名字推断不参与。
                EmitSlot(cb, bindings, members, UIOverrideSlot.TabList, nameof(UIList), "__listTab");
                EmitSlot(cb, bindings, members, UIOverrideSlot.TabContent, nameof(RectTransform), "__tabContent");
                EmitSlot(cb, bindings, members, UIOverrideSlot.TabClose, nameof(GameObject), "__btnClose");
            }
            else if (baseType == "UITip")
            {
                // Tip 还没有独立选择区，暂时保留名字推断。
                for (int i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    if (member.FieldName.ToLowerInvariant().Contains("txtcontent"))
                        cb.Line($"protected override TMP_Text __txtContent => {member.FieldName};");
                }
            }
            else if (baseType == "UIDialog")
            {
                // 对话框的固定字段只认面板里的显式绑定，没配就不生成——名字推断对对话框一律不生效。
                EmitSlot(cb, bindings, members, UIOverrideSlot.DialogTitle, "TMP_Text", "__txtTitle");
                EmitSlot(cb, bindings, members, UIOverrideSlot.DialogContent, "TMP_Text", "__txtContent");
                EmitSlot(cb, bindings, members, UIOverrideSlot.DialogClose, "GameObject", "__btnClose");
                EmitSlot(cb, bindings, members, UIOverrideSlot.DialogBtn1, "GameObject", "__btn1");
                EmitSlot(cb, bindings, members, UIOverrideSlot.DialogBtn2, "GameObject", "__btn2");
                EmitSlot(cb, bindings, members, UIOverrideSlot.DialogCheckbox, "Toggle", "__checkbox");
            }
            else if (baseType == "UIWindow")
            {
                // 窗口只生成「主体」与「拖动范围」；标题栏/标题/按钮属于框架预制。
                // panelBody / panelDragArea 作为默认字段名兜底，没配槽位也能自动接上。
                EmitSlot(cb, bindings, members, UIOverrideSlot.WindowBody, nameof(RectTransform), "__winBody", "panelBody");
                EmitSlot(cb, bindings, members, UIOverrideSlot.WindowDragArea, nameof(RectTransform), "__winDragArea", "panelDragArea");
            }
            else if (baseType == "UIWindowFrame")
            {
                // 窗口框架：标题栏 + 标题 + 三个按钮。
                EmitSlot(cb, bindings, members, UIOverrideSlot.FrameTitleBar, nameof(RectTransform), "__frameTitleBar", "panelTitleBar");
                EmitSlot(cb, bindings, members, UIOverrideSlot.FrameTitle, "TMP_Text", "__frameTitle", "txtTitle");
                EmitSlot(cb, bindings, members, UIOverrideSlot.FrameBtnMin, nameof(GameObject), "__frameBtnMin", "btnMin");
                EmitSlot(cb, bindings, members, UIOverrideSlot.FrameBtnMax, nameof(GameObject), "__frameBtnMax", "btnMax");
                EmitSlot(cb, bindings, members, UIOverrideSlot.FrameBtnClose, nameof(GameObject), "__frameBtnClose", "btnClose");
                EmitSlot(cb, bindings, members, UIOverrideSlot.FrameBtnPin, nameof(GameObject), "__frameBtnPin", "btnPin");
            }

            cb.Blank();
            cb.Open("protected override void CreateChildren()");
            cb.Line("var root = Transform;");

            var localNames = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                var expression = BuildChildExpression(member.Path, localNames, cb);
                if (IsUIObjectType(member.FieldType))
                    cb.Line($"{member.FieldName} = new {member.FieldType}({expression}.gameObject, this);");
                else if (member.FieldType == nameof(GameObject))
                    cb.Line($"{member.FieldName} = {expression}.gameObject;");
                else if (member.FieldType == nameof(RectTransform))
                    cb.Line($"{member.FieldName} = {expression} as RectTransform;");
                else
                    cb.Line($"{member.FieldName} = {expression}.GetComponent<{member.FieldType}>();");
            }

            cb.Close();

            var clickMembers = members.FindAll(member => (member.Events & UIMemberEvents.Click) != 0);
            var doubleClickMembers = members.FindAll(member => (member.Events & UIMemberEvents.DoubleClick) != 0);
            var longPressMembers = members.FindAll(member => (member.Events & UIMemberEvents.LongPress) != 0);
            var listMembers = members.FindAll(member => (member.Events & UIMemberEvents.ItemClick) != 0);
            var hasEvents = clickMembers.Count > 0 || doubleClickMembers.Count > 0 ||
                            longPressMembers.Count > 0 || listMembers.Count > 0;
            if (baseType != "UIDialog" && baseType != "UITip" && hasEvents)
            {
                cb.Blank();
                cb.Open("protected override void OnAddEvent()");
                for (int i = 0; i < clickMembers.Count; i++)
                {
                    var member = clickMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    cb.Line($"AddClick({member.FieldName}, _On{callbackName}Click);");
                }

                for (int i = 0; i < doubleClickMembers.Count; i++)
                {
                    var member = doubleClickMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    cb.Line($"AddMultipleClick({member.FieldName}, _On{callbackName}DoubleClick, 2, {member.DoubleClickInterval.ToString("0.##")}f);");
                }

                for (int i = 0; i < longPressMembers.Count; i++)
                {
                    var member = longPressMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    cb.Line($"AddLongPress({member.FieldName}, {member.LongPressBreak.ToString("0.##")}f, _On{callbackName}LongPress, {member.LongPressInterval.ToString("0.##")}f, {member.LongPressStart}, {member.LongPressEnd});");
                }

                for (int i = 0; i < listMembers.Count; i++)
                {
                    var member = listMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    cb.Line($"Bind{callbackName}ItemClick();");
                }
                cb.Close();

                for (int i = 0; i < clickMembers.Count; i++)
                {
                    var member = clickMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    cb.Blank();
                    cb.Open($"private void _On{callbackName}Click(UIEventContext e)");
                    cb.Line($"On{callbackName}Click(e);");
                    cb.Close();
                    cb.Line($"partial void On{callbackName}Click(UIEventContext e);");
                }

                for (int i = 0; i < doubleClickMembers.Count; i++)
                {
                    var member = doubleClickMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    cb.Blank();
                    cb.Open($"private void _On{callbackName}DoubleClick(UIEventContext e)");
                    cb.Line($"On{callbackName}DoubleClick(e);");
                    cb.Close();
                    cb.Line($"partial void On{callbackName}DoubleClick(UIEventContext e);");
                }

                for (int i = 0; i < longPressMembers.Count; i++)
                {
                    var member = longPressMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    cb.Blank();
                    cb.Open($"private bool _On{callbackName}LongPress()");
                    cb.Line("var isBreak = false;");
                    cb.Line($"On{callbackName}LongPress(ref isBreak);");
                    cb.Line("return isBreak;");
                    cb.Close();
                    cb.Line($"partial void On{callbackName}LongPress(ref bool isBreak);");
                }
            }

            for (int i = 0; i < listMembers.Count; i++)
            {
                var member = listMembers[i];
                var callbackName = ToCallbackName(member.FieldName);
                cb.Blank();
                cb.Open($"protected void Bind{callbackName}ItemClick()");
                cb.Line($"AddItemClick({member.FieldName}, _On{callbackName}ItemClick);");
                cb.Close();

                cb.Open($"private void _On{callbackName}ItemClick(IItemRenderer item)");
                cb.Line($"On{callbackName}ItemClick(item);");
                cb.Close();
                cb.Line($"partial void On{callbackName}ItemClick(IItemRenderer item);");
            }

            cb.Close();
            cb.Close();
            return cb.ToString();
        }

        /// <summary>按槽位取显式绑定的成员并输出 override；没绑定、类型不符、成员已删除都跳过。</summary>
        private static void EmitSlot(CodeBuilder cb, IReadOnlyDictionary<UIOverrideSlot, string> bindings,
            List<UGUIMemberData> members, UIOverrideSlot slot, string fieldType, string overrideName,
            string defaultMemberName = null)
        {
            string pathKey = null;
            if (bindings != null && bindings.TryGetValue(slot, out var configured) &&
                !string.IsNullOrEmpty(configured))
                pathKey = configured;

            // 没配置槽位时，允许按字段名兜底（如 panelTitleBar），避免每个预制都要手动绑一次。
            if (pathKey == null && string.IsNullOrEmpty(defaultMemberName))
                return;

            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                if (member == null || member.FieldType != fieldType) continue;

                var matched = pathKey != null
                    ? string.Equals(PathKey(member.Path), pathKey, StringComparison.Ordinal)
                    : string.Equals(member.FieldName, defaultMemberName, StringComparison.Ordinal);

                if (!matched) continue;

                cb.Line($"protected override {fieldType} {overrideName} => {member.FieldName};");
                return;
            }
        }

        private static string BuildChildExpression(List<int> path,
            Dictionary<string, string> localNames, CodeBuilder cb)
        {
            var expression = "root";
            var key = string.Empty;
            for (int i = 0; i < path.Count; i++)
            {
                expression += $".GetChild({path[i]})";
                key += "_" + path[i];
                if (i == path.Count - 1)
                    break;

                if (!localNames.TryGetValue(key, out var localName))
                {
                    localName = "node" + key.Replace("_", "_");
                    localNames[key] = localName;
                    cb.Line($"var {localName} = {expression};");
                }

                expression = localName;
            }

            return expression;
        }

        private static string GetOutputDirectory(string prefabPath)
        {
            var normalized = prefabPath.Replace('\\', '/');
            const string uiRoot = "Assets/GameRes/Res/UI/";
            if (!normalized.StartsWith(uiRoot, StringComparison.OrdinalIgnoreCase))
                return DefaultOutputRoot;

            var relative = normalized.Substring(uiRoot.Length);
            var slash = relative.LastIndexOf('/');
            if (slash < 0) return DefaultOutputRoot;

            var segments = relative.Substring(0, slash).Split('/');
            var group = string.Empty;
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (string.IsNullOrEmpty(segment) ||
                    segment.Equals(PrefabFolderName, StringComparison.OrdinalIgnoreCase))
                    continue;

                group = group.Length == 0 ? segment : group + "/" + segment;
            }

            return string.IsNullOrEmpty(group)
                ? DefaultOutputRoot
                : Path.Combine(DefaultOutputRoot, group).Replace('\\', '/');
        }

        private static string ResolveBaseType(string prefabName)
        {
            if (prefabName.EndsWith("WindowFrame", StringComparison.OrdinalIgnoreCase))
                return "UIWindowFrame";
            if (prefabName.EndsWith("Window", StringComparison.OrdinalIgnoreCase))
                return "UIWindow";
            if (prefabName.EndsWith("TabView", StringComparison.OrdinalIgnoreCase))
                return "UITabView";
            if (prefabName.EndsWith("MessageBox", StringComparison.OrdinalIgnoreCase) ||
                prefabName.EndsWith("Dialog", StringComparison.OrdinalIgnoreCase))
                return "UIDialog";
            if (prefabName.EndsWith("Tip", StringComparison.OrdinalIgnoreCase))
                return "UITip";
            if (prefabName.EndsWith("UIModel", StringComparison.OrdinalIgnoreCase))
                return "UIModel";
            if (prefabName.EndsWith("RTModel", StringComparison.OrdinalIgnoreCase))
                return "RTModel";
            if (prefabName.EndsWith("TabFrame", StringComparison.OrdinalIgnoreCase))
                return "UITabFrame";
            return "UIView";
        }

        /// <summary>
        /// YooAsset 地址规则为 AddressByGroupAndFileName：{GroupName}_{FileName}。
        /// 收集器组名为 UI，所以实际地址是 UI_LoginView 这类形式。
        /// </summary>
        /// <summary>
        /// YooAsset 地址规则为 AddressByGroupAndFileName：{GroupName}_{FileName}。
        /// UI 收集器组名为 UI，所以实际地址是 UI_LoginView 这种形式。
        /// </summary>

        private static string ToCallbackName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return "Node";
            return char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
        }

        private static string MakeUniqueFieldName(string source, HashSet<string> names)
        {
            var baseName = SanitizeIdentifier(source, "node");
            if (char.IsUpper(baseName[0]))
                baseName = char.ToLowerInvariant(baseName[0]) + baseName.Substring(1);

            var name = baseName;
            var index = 1;
            while (!names.Add(name))
                name = baseName + (++index);
            return name;
        }

        private static string SanitizeIdentifier(string source, string fallback)
        {
            if (string.IsNullOrEmpty(source))
                return fallback;

            var sb = new StringBuilder(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                var c = source[i];
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }

            if (sb.Length == 0 || char.IsDigit(sb[0]))
                sb.Insert(0, '_');
            return sb.ToString();
        }

    }

    /// <summary>
    /// 生成面板里的一行：节点路径、字段名、类型、默认事件和事件参数。
    /// 手动配置时可以直接修改这些值，生成器按它输出代码。
    /// </summary>
    [Serializable]
    public sealed class UGUIMemberData
    {
        public string NodeName;
        public List<int> Path = new List<int>();

        [Tooltip("路径的字符串形式，仅用于保存面板配置。")]
        public string PathKey;
        public string FieldName;
        public string FieldType;
        public bool Generate;
        public bool MatchedByRule;

        [Tooltip("用户手动改过类型，重新扫描时不再被自动识别覆盖。")]
        public bool ManualType;
        public UIMemberEvents Events;

        [Tooltip("双击判定间隔（秒）。")]
        public float DoubleClickInterval = 0.2f;

        [Tooltip("长按回调间隔（秒）。")]
        public float LongPressInterval = 0.2f;

        [Tooltip("长按触发前的启动次数。")]
        public int LongPressStart;

        [Tooltip("长按最多重复次数。")]
        public int LongPressEnd = 50;

        [Tooltip("长按中断时间；负数表示不按时间中断。")]
        public float LongPressBreak = -1f;
    }
}
#endif
