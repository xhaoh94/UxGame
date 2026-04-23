using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor
{
    public static class CodeGenHelper
    {
        static Dictionary<string, string> _RegisterValueChangedCallback = new Dictionary<string, string>()
        {
            { nameof(Toggle),$"ChangeEvent<bool>" },
            { nameof(EnumField),$"ChangeEvent<Enum>" },
            { nameof(TextField),$"ChangeEvent<string>" },
            { nameof(FloatField),$"ChangeEvent<float>" },
            { nameof(IntegerField),$"ChangeEvent<int>" },
            { nameof(ObjectField),$"ChangeEvent<UnityEngine.Object>" },
            { nameof(DropdownField),$"ChangeEvent<string>" },
        };

        static List<object> GetVisualElementAssetsOld(VisualTreeAsset tree)
        {
            var type = tree.GetType();
            var flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var field = type.GetField("m_VisualElementAssets", flag);
            if (field != null)
            {
                return field.GetValue(tree) as List<object>;
            }
            return null;
        }

        static List<object> GetVisualElementAssetsNew(VisualTreeAsset tree)
        {
            var type = tree.GetType();
            var flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var field = type.GetField("m_VisualTree", flag);
            if (field != null)
            {
                List<object> r = new();

                void DumpFields(System.Type t, string prefix)
                {
                    Log.Info($"{prefix}Type: {t.FullName}");
                    foreach (var f in t.GetFields(flag))
                    {
                        Log.Info($"{prefix}  Field: {f.Name} ({f.FieldType.Name})");
                    }
                    if (t.BaseType != null)
                    {
                        DumpFields(t.BaseType, prefix + "  ");
                    }
                }

                void ForeachElement(object child)
                {
                    var cType = child.GetType();

                    // 查找 m_Children 字段（可能在当前类或父类）
                    FieldInfo childrenField = null;
                    var currentType = cType;
                    while (currentType != null && childrenField == null)
                    {
                        childrenField = currentType.GetField("m_Children", flag);
                        if (childrenField == null)
                        {
                            childrenField = currentType.GetField("children", flag);
                        }
                        currentType = currentType.BaseType;
                    }

                    if (childrenField == null)
                    {
                        // 如果没有 children 字段，检查是否有 Properties 或 visualElementAssets
                        var veAssetsProp = cType.GetProperty("visualElementAssets", flag);
                        if (veAssetsProp != null)
                        {
                            var assets = veAssetsProp.GetValue(child) as IList;
                            if (assets != null)
                            {
                                foreach (var asset in assets)
                                {
                                    if (asset != null) r.Add(asset);
                                }
                            }
                            return;
                        }

                        // 最后尝试直接添加（叶子节点）
                        r.Add(child);
                        return;
                    }

                    var children = childrenField.GetValue(child) as IList;
                    if (children == null || children.Count == 0)
                    {
                        r.Add(child);
                    }
                    else
                    {
                        foreach (var cc in children)
                        {
                            if (cc != null) ForeachElement(cc);
                        }
                    }
                }

                try
                {
                    object rootElement = field.GetValue(tree);
                    if (rootElement == null)
                    {
                        Log.Warning("m_VisualTree 字段为空");
                        return r;
                    }

                    ForeachElement(rootElement);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"获取子元素时出错: {ex.Message}");
                }

                return r;
            }
            return null;
        }

        [MenuItem("Assets/UIElements/CodeGenByUxml", false)]
        public static void GenCode()
        {
            VisualTreeAsset[] activeGos = Selection.GetFiltered<VisualTreeAsset>(SelectionMode.Unfiltered);
            foreach (UnityEngine.Object obj in activeGos)
            {
                _GenCode(obj);
            }
            AssetDatabase.Refresh();
        }
        static void _GenCode(UnityEngine.Object activeGo)
        {
            var tree = (activeGo as VisualTreeAsset);
            if (tree == null)
            {
                Log.Error("需要uxml文件");
                return;
            }

            var filePath = AssetDatabase.GetAssetPath(tree).Replace("\\", "/");
            if (!filePath.EndsWith(".uxml"))
            {
                Log.Error("需要uxml文件");
                return;
            }
            string uxmlOriginalContent = File.ReadAllText(filePath);
            IUxmlParser parser = new UxmlParser();
            var namedNodes = parser.GetNamedNodesFromUxmlString(uxmlOriginalContent);
            if (namedNodes.Count == 0)
            {
                Log.Warning("未找到子元素，可能 Unity 版本不兼容");
                return;
            }

            var clsName = tree.name;
            var dir = filePath.Replace($"/{clsName}.uxml", "");
            var ns = dir.Replace("Assets", "Ux").Replace("/", ".").Replace("/Uxml", "").Replace(".Uxml", "");

            var write = new CodeGenWrite();
            write.Writeln(@"//自动生成的代码，请勿修改!!!");
            write.Writeln(@"//CodeGen By [Assets/UIElements/CodeGenByUxml]");
            write.Writeln("using System;");
            write.Writeln("using System.Collections.Generic;");
            write.Writeln("using UnityEditor;");
            write.Writeln("using UnityEngine.UIElements;");
            write.Writeln("using UnityEditor.UIElements;");
            write.Writeln($"namespace {ns}");
            write.StartBlock();
            write.Writeln($"public partial class {clsName}");
            write.StartBlock();

            write.Writeln($"protected VisualElement root;");
            foreach (var none in namedNodes)
            {
                write.Writeln($"public {none.Value.LocalName} {none.Key};");
            }
            write.Writeln($"protected void CreateChildren()");
            write.StartBlock();
            write.Writeln($"var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(\"{filePath}\");");
            write.Writeln($"if (_visualAsset == null) return;");
            write.Writeln($"root = _visualAsset.CloneTree();");
            write.Writeln($"root.style.flexGrow = 1f;");

            foreach (var none in namedNodes)
            {
                var type = none.Value.LocalName;
                var name = none.Key;
                write.Writeln($"{name} = root.Q<{type}>(\"{name}\");");
                if (none.Value.IsReadOnly) continue;                
                if (_RegisterValueChangedCallback.ContainsKey(type))
                {
                    var fnName = $"_On{char.ToUpper(name[0])}{name.Substring(1)}Changed";
                    write.Writeln($"{name}.RegisterValueChangedCallback(e => {fnName}(e));");
                }
                if (type == nameof(Button) || type == nameof(ToolbarButton))
                {
                    var fnName = $"_On{char.ToUpper(name[0])}{name.Substring(1)}Click";
                    write.Writeln($"{name}.clicked += () => {fnName}();");
                }
                if (type == nameof(ListView))
                {
                    var makeName = $"_OnMake{char.ToUpper(name[0])}{name.Substring(1)}Item";
                    write.Writeln($"{name}.makeItem = ()=> {{ var e = new VisualElement(); {makeName}(e); return e; }};");
                    var bindName = $"_OnBind{char.ToUpper(name[0])}{name.Substring(1)}Item";
                    write.Writeln($"{name}.bindItem = (e,i)=> {bindName}(e,i);");
                    var clickName = $"_On{char.ToUpper(name[0])}{name.Substring(1)}ItemClick";
                    write.Writeln($"#if UNITY_2022_1_OR_NEWER");
                    write.Writeln($"{name}.selectionChanged += e => {clickName}(e);");
                    write.Writeln($"#else");
                    write.Writeln($"{name}.onSelectionChange += e => {clickName}(e);");
                    write.Writeln($"#endif");
                }
            }
            write.EndBlock();

            foreach (var none in namedNodes)
            {
                var type = none.Value.LocalName;
                var name = none.Key;
                if (none.Value.IsReadOnly) continue;
                if (_RegisterValueChangedCallback.TryGetValue(type, out var evt))
                {
                    var fnName = $"_On{char.ToUpper(name[0])}{name.Substring(1)}Changed";
                    write.Writeln($"partial void {fnName}({evt} e);");
                }
                if (type == nameof(Button) || type == nameof(ToolbarButton))
                {
                    var fnName = $"_On{char.ToUpper(name[0])}{name.Substring(1)}Click";
                    write.Writeln($"partial void {fnName}();");
                }
                if (type == nameof(ListView))
                {
                    var makeName = $"_OnMake{char.ToUpper(name[0])}{name.Substring(1)}Item";
                    write.Writeln($"partial void {makeName}(VisualElement e);");
                    var bindName = $"_OnBind{char.ToUpper(name[0])}{name.Substring(1)}Item";
                    write.Writeln($"partial void {bindName}(VisualElement e,int index);");
                    var clickName = $"_On{char.ToUpper(name[0])}{name.Substring(1)}ItemClick";
                    write.Writeln($"partial void {clickName}(IEnumerable<object> objs);");
                }
            }

            write.EndBlock();
            write.EndBlock();

            write.Export($"{dir}/GenCode/", $"{clsName}_GenCode");
        }
        static Dictionary<string, string> _Parse(object obj)
        {
            var flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var objType = obj.GetType();
            var baseType = objType.BaseType;

            var typeNameField = baseType.GetField("m_FullTypeName", flag);
            if (typeNameField == null)
            {
                typeNameField = baseType.GetField("fullTypeName", flag);
            }
            if (typeNameField == null)
            {
                typeNameField = objType.GetField("m_FullTypeName", flag);
            }

            var typeName = typeNameField?.GetValue(obj)?.ToString() ?? "VisualElement";
            typeName = typeName.Substring(typeName.LastIndexOf(".") + 1);

            var propertiesField = baseType.GetField("m_Properties", flag);
            if (propertiesField == null)
            {
                propertiesField = baseType.GetField("properties", flag);
            }
            if (propertiesField == null)
            {
                propertiesField = objType.GetField("m_Properties", flag);
            }

            Dictionary<string, string> map = new Dictionary<string, string>();

            if (propertiesField != null)
            {
                IList properties = propertiesField.GetValue(obj) as IList;
                if (properties != null)
                {
                    for (int i = 0; i < properties.Count; i += 2)
                    {
                        var key = properties[i];
                        var value = properties[i + 1];
                        map[key.ToString()] = value.ToString();
                    }
                }
            }

            var nameField = baseType.GetField("m_Name", flag) ?? objType.GetField("m_Name", flag);
            if (nameField != null)
            {
                var name = nameField.GetValue(obj)?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    map["name"] = name;
                }
            }

            map["typeName"] = typeName;
            return map;
        }
    }

}

