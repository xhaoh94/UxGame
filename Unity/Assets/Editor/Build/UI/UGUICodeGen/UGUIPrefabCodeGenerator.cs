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
    public static class UGUIPrefabCodeGenerator
    {
        private const string DefaultOutputRoot = "Assets/Hotfix/CodeGen/UI";
        private const string ResRoot = "Assets/GameRes/Res/";

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
                members);
        }

        /// <summary>按面板里勾选并调整过的成员生成代码。</summary>
        public static string GenerateConfigured(GameObject prefab, string prefabPath,
            string outputRoot, string namespaceName, string baseType,
            List<UGUIMemberData> members)
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

            var code = BuildCode(className, namespaceName, baseType, selected);
            var directory = outputRoot.Replace("\\", "/");
            Directory.CreateDirectory(directory);
            var outputPath = Path.Combine(directory, className + ".cs").Replace("\\", "/");
            File.WriteAllText(outputPath, code, new UTF8Encoding(false));
            Debug.Log($"UI 代码生成成功：{outputPath}");
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
                if (UGUIBindNaming.ShouldIgnore(child.name))
                    continue;

                var path = new List<int>(parentPath) { i };
                var instanceType = TryGetPrefabInstanceType(child.gameObject);
                var rule = UGUIBindNaming.FindRule(child.name);

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

                var fieldName = MakeUniqueFieldName(UGUIBindNaming.ToFieldName(child.name, type), names);

                list.Add(new UGUIMemberData
                {
                    NodeName = child.name,
                    Path = path,
                    FieldName = fieldName,
                    FieldType = type,
                    Generate = matched,
                    MatchedByRule = matched,
                    Events = UGUIMemberEvents.None,
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
                if (UGUIBindNaming.ShouldIgnore(child.name))
                    continue;

                var path = new List<int>(parentPath) { i };
                // Prefab 实例优先识别为 Wrapper 类型，其次走命名规则。
                var instanceType = TryGetPrefabInstanceType(child.gameObject);
                var rule = UGUIBindNaming.FindRule(child.name);
                var type = instanceType ?? rule?.FieldType;
                if (type != null)
                {
                    var fieldName = MakeUniqueFieldName(UGUIBindNaming.ToFieldName(child.name, type), names);
                    members.Add(new UGUIMemberData { NodeName = child.name, Path = path, FieldName = fieldName, FieldType = type, Generate = true, MatchedByRule = true, Events = UGUIMemberEvents.None });
                }

                // UIList 内部是运行时模板，不参与页面字段。
                if (type == nameof(UIList))
                    continue;

                CollectMembers(child, path, members, names);
            }
        }

        private static string BuildCode(string className, string namespaceName, string baseType,
            List<UGUIMemberData> members)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// 自动生成的代码，请勿修改!!!");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using TMPro;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public partial class {className} : {baseType}");
            sb.AppendLine("    {");

            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                sb.AppendLine($"        protected {member.FieldType} {member.FieldName};");
            }

            if (baseType == "UIModel" || baseType == "RTModel" || baseType == "UITabFrame")
            {
                sb.AppendLine();
                sb.AppendLine($"        public {className}(GameObject gameObject, UIObject parent) : base(gameObject, parent) {{ }}");
            }

            if (baseType == "UITabFrame" || baseType == "UIDialog" || baseType == "UITip")
            {
                for (int i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    var lowerName = member.FieldName.ToLowerInvariant();
                    if (baseType == "UITabFrame")
                    {
                        if (member.FieldType == nameof(UIList))
                            sb.AppendLine($"        protected override UIList __listTab => {member.FieldName};");
                        else if (lowerName.Contains("tabcontent"))
                            sb.AppendLine($"        protected override RectTransform __tabContent => {member.FieldName};");
                        else if (lowerName.Contains("btnclose"))
                            sb.AppendLine($"        protected override GameObject __btnClose => {member.FieldName};");
                    }
                    else if (baseType == "UIDialog")
                    {
                        if (lowerName.Contains("txttitle"))
                            sb.AppendLine($"        protected override TMP_Text __txtTitle => {member.FieldName};");
                        else if (lowerName.Contains("txtcontent"))
                            sb.AppendLine($"        protected override TMP_Text __txtContent => {member.FieldName};");
                        else if (lowerName.Contains("btnclose"))
                            sb.AppendLine($"        protected override GameObject __btnClose => {member.FieldName};");
                        else if (lowerName == "btn1")
                            sb.AppendLine($"        protected override GameObject __btn1 => {member.FieldName};");
                        else if (lowerName == "btn2")
                            sb.AppendLine($"        protected override GameObject __btn2 => {member.FieldName};");
                        else if (lowerName.Contains("checkbox"))
                            sb.AppendLine($"        protected override Toggle __checkbox => {member.FieldName};");
                    }
                    else if (lowerName.Contains("txtcontent"))
                    {
                        sb.AppendLine($"        protected override TMP_Text __txtContent => {member.FieldName};");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("        protected override void CreateChildren()");
            sb.AppendLine("        {");
            sb.AppendLine("            var root = GameObject.transform;");

            var localNames = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                var expression = BuildChildExpression(member.Path, localNames, sb);
                if (IsUIObjectType(member.FieldType))
                    sb.AppendLine($"            {member.FieldName} = new {member.FieldType}({expression}.gameObject, this);");
                else if (member.FieldType == nameof(GameObject))
                    sb.AppendLine($"            {member.FieldName} = {expression}.gameObject;");
                else if (member.FieldType == nameof(RectTransform))
                    sb.AppendLine($"            {member.FieldName} = {expression} as RectTransform;");
                else
                    sb.AppendLine($"            {member.FieldName} = {expression}.GetComponent<{member.FieldType}>();");
            }

            sb.AppendLine("        }");

            var clickMembers = members.FindAll(member => (member.Events & UGUIMemberEvents.Click) != 0);
            var doubleClickMembers = members.FindAll(member => (member.Events & UGUIMemberEvents.DoubleClick) != 0);
            var longPressMembers = members.FindAll(member => (member.Events & UGUIMemberEvents.LongPress) != 0);
            var listMembers = members.FindAll(member => (member.Events & UGUIMemberEvents.ItemClick) != 0);
            var hasEvents = clickMembers.Count > 0 || doubleClickMembers.Count > 0 ||
                            longPressMembers.Count > 0 || listMembers.Count > 0;
            if (baseType != "UIDialog" && baseType != "UITip" && hasEvents)
            {
                sb.AppendLine();
                sb.AppendLine("        protected override void OnAddEvent()");
                sb.AppendLine("        {");
                for (int i = 0; i < clickMembers.Count; i++)
                {
                    var member = clickMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    sb.AppendLine($"            AddClick({member.FieldName}, _On{callbackName}Click);");
                }

                for (int i = 0; i < doubleClickMembers.Count; i++)
                {
                    var member = doubleClickMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    sb.AppendLine($"            AddMultipleClick({member.FieldName}, _On{callbackName}DoubleClick, 2, {member.DoubleClickInterval.ToString("0.##")}f);");
                }

                for (int i = 0; i < longPressMembers.Count; i++)
                {
                    var member = longPressMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    sb.AppendLine($"            AddLongPress({member.FieldName}, {member.LongPressBreak.ToString("0.##")}f, _On{callbackName}LongPress, {member.LongPressInterval.ToString("0.##")}f, {member.LongPressStart}, {member.LongPressEnd});");
                }

                for (int i = 0; i < listMembers.Count; i++)
                {
                    var member = listMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    sb.AppendLine($"            Bind{callbackName}ItemClick();");
                }
                sb.AppendLine("        }");

                for (int i = 0; i < clickMembers.Count; i++)
                {
                    var member = clickMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    sb.AppendLine();
                    sb.AppendLine($"        private void _On{callbackName}Click(UIEventContext e)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            On{callbackName}Click(e);");
                    sb.AppendLine("        }");
                    sb.AppendLine($"        partial void On{callbackName}Click(UIEventContext e);");
                }

                for (int i = 0; i < doubleClickMembers.Count; i++)
                {
                    var member = doubleClickMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    sb.AppendLine();
                    sb.AppendLine($"        private void _On{callbackName}DoubleClick(UIEventContext e)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            On{callbackName}DoubleClick(e);");
                    sb.AppendLine("        }");
                    sb.AppendLine($"        partial void On{callbackName}DoubleClick(UIEventContext e);");
                }

                for (int i = 0; i < longPressMembers.Count; i++)
                {
                    var member = longPressMembers[i];
                    var callbackName = ToCallbackName(member.FieldName);
                    sb.AppendLine();
                    sb.AppendLine($"        private bool _On{callbackName}LongPress()");
                    sb.AppendLine("        {");
                    sb.AppendLine("            var isBreak = false;");
                    sb.AppendLine($"            On{callbackName}LongPress(ref isBreak);");
                    sb.AppendLine("            return isBreak;");
                    sb.AppendLine("        }");
                    sb.AppendLine($"        partial void On{callbackName}LongPress(ref bool isBreak);");
                }
            }

            for (int i = 0; i < listMembers.Count; i++)
            {
                var member = listMembers[i];
                var callbackName = ToCallbackName(member.FieldName);
                sb.AppendLine();
                sb.AppendLine($"        protected void Bind{callbackName}ItemClick()");
                sb.AppendLine("        {");
                sb.AppendLine($"            AddItemClick({member.FieldName}, _On{callbackName}ItemClick);");
                sb.AppendLine("        }");
                sb.AppendLine($"        private void _On{callbackName}ItemClick(IItemRenderer item)");
                sb.AppendLine("        {");
                sb.AppendLine($"            On{callbackName}ItemClick(item);");
                sb.AppendLine("        }");
                sb.AppendLine($"        partial void On{callbackName}ItemClick(IItemRenderer item);");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildChildExpression(List<int> path,
            Dictionary<string, string> localNames, StringBuilder sb)
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
                    sb.AppendLine($"            var {localName} = {expression};");
                }

                expression = localName;
            }

            return expression;
        }

        private static string GetOutputDirectory(string prefabPath)
        {
            var normalized = prefabPath.Replace('\\', '/');
            const string uiRoot = "Assets/GameRes/Res/UI/";
            if (normalized.StartsWith(uiRoot, StringComparison.OrdinalIgnoreCase))
            {
                var relative = normalized.Substring(uiRoot.Length);
                var slash = relative.LastIndexOf('/');
                var group = slash >= 0 ? relative.Substring(0, slash) : string.Empty;
                return string.IsNullOrEmpty(group)
                    ? DefaultOutputRoot
                    : Path.Combine(DefaultOutputRoot, group).Replace('\\', '/');
            }

            return DefaultOutputRoot;
        }

        private static string ResolveBaseType(string prefabName)
        {
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
        public UGUIMemberEvents Events;

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
