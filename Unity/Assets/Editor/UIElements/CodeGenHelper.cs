using System.Collections;
using System.Collections.Generic;
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
        };

        [MenuItem("Assets/UIElements/CodeGenByUxml", false)]
        public static void GenCode()
        {
            VisualTreeAsset[] activeGos = Selection.GetFiltered<VisualTreeAsset>(SelectionMode.Unfiltered);
            foreach (Object obj in activeGos)
            {
                _GenCode(obj);
            }
            AssetDatabase.Refresh();
        }
        static void _GenCode(Object activeGo)
        {
            var tree = (activeGo as VisualTreeAsset);
            if (tree == null)
            {
                Log.Error("需要uxml类型");
                return;
            }

            var filePath = AssetDatabase.GetAssetPath(tree).Replace("\\", "/");
            if (!filePath.EndsWith(".uxml"))
            {
                Log.Error("需要uxml类型");
                return;
            }

            var property = tree.GetType().GetField("m_VisualElementAssets", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            IList listElement = property.GetValue(tree) as IList;

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
            foreach (var element in listElement)
            {
                var map = _Parse(element);
                if (map.TryGetValue("name", out var name) &&
                    map.TryGetValue("typeName", out var type))
                {
                    write.Writeln($"public {type} {name};");
                }
            }
            write.Writeln($"protected void CreateChildren()");
            write.StartBlock();
            write.Writeln($"var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(\"{filePath}\");");
            write.Writeln($"if (_visualAsset == null) return;");
            write.Writeln($"root = _visualAsset.CloneTree();");
            write.Writeln($"root.style.flexGrow = 1f;");

            foreach (var element in listElement)
            {
                var map = _Parse(element);
                if (map.TryGetValue("name", out var name) &&
                    map.TryGetValue("typeName", out var type))
                {
                    write.Writeln($"{name} =root.Q<{type}>(\"{name}\");");
                    if (map.ContainsKey("readonly"))
                    {
                        continue;
                    }
                    if (_RegisterValueChangedCallback.ContainsKey(type))
                    {
                        var fnName = $"_On{char.ToUpper(name[0])}{name.Substring(1)}Changed";
                        write.Writeln($"{name}.RegisterValueChangedCallback(e => {fnName}(e));");
                    }
                    if (type == nameof(Button))
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
            }
            write.EndBlock();

            foreach (var element in listElement)
            {
                var map = _Parse(element);
                if (map.TryGetValue("name", out var name) &&
                    map.TryGetValue("typeName", out var type) &&
                    !map.ContainsKey("readonly"))
                {
                    if (_RegisterValueChangedCallback.TryGetValue(type, out var evt))
                    {
                        var fnName = $"_On{char.ToUpper(name[0])}{name.Substring(1)}Changed";
                        write.Writeln($"partial void {fnName}({evt} e);");
                    }
                    if (type == nameof(Button))
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
            }

            write.EndBlock();
            write.EndBlock();

            write.Export($"{dir}/GenCode/", $"{clsName}_GenCode");
        }
        static Dictionary<string, string> _Parse(object obj)
        {
            var flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var typeName = obj.GetType().BaseType.GetField("m_FullTypeName", flag).GetValue(obj).ToString();
            typeName = typeName.Substring(typeName.LastIndexOf(".") + 1);
            IList properties = obj.GetType().BaseType.GetField("m_Properties", flag).GetValue(obj) as IList;
            Dictionary<string, string> map = new Dictionary<string, string>();
            for (int i = 0; i < properties.Count; i += 2)
            {
                var key = properties[i];
                var value = properties[i + 1];
                map[key.ToString()] = value.ToString();
            }
            map["typeName"] = typeName;
            return map;
        }
    }

}

