using dnlib.DotNet;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor
{
    public static class DymTools
    {
        static Dictionary<string, string> _RegisterValueChangedCallback = new Dictionary<string, string>()
        {
            { nameof(Toggle),$"ChangeEvent<bool>" },
            { nameof(EnumField),$"ChangeEvent<Enum>" },
            { nameof(TextField),$"ChangeEvent<string>" },
        };

        [MenuItem("Assets/UIElements/CodeGenByUxml", false)]
        public static void GenCode()
        {
            var tree = (Selection.activeObject as VisualTreeAsset);
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
            var ns = dir.Replace("Assets", "Ux").Replace("/", ".");

            var write = new CodeGenWrite();
            write.Writeln(@"//自动生成的代码，请勿修改!!!");
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
                var (name, type) = _Parse(element);
                if (!string.IsNullOrEmpty(name))
                {
                    write.Writeln($"protected {type} {name};");
                }
            }
            write.Writeln($"protected void CreateChildren()");
            write.StartBlock();
            write.Writeln($"var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(\"{filePath}\");");
            write.Writeln($"if (_visualAsset == null) return;");
            write.Writeln($"root = _visualAsset.CloneTree();");
            foreach (var element in listElement)
            {
                var (name, type) = _Parse(element);
                if (!string.IsNullOrEmpty(name))
                {
                    Debug.Log(name + "_" + type);
                    write.Writeln($"{name} =root.Q<{type}>(\"{name}\");");
                    if(_RegisterValueChangedCallback.ContainsKey(type))
                    {
                        var fnName = $"_On{char.ToUpper(name[0])}{name.Substring(1)}Changed";
                        write.Writeln($"{name}.RegisterValueChangedCallback(e => {fnName}(e));");
                    }
                    if (type==nameof(Button))
                    {
                        var fnName = $"_On{char.ToUpper(name[0])}{name.Substring(1)}Click";
                        write.Writeln($"{name}.clicked += () => {fnName}();");
                    }
                    if(type == nameof(ListView))
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
                var (name, type) = _Parse(element);
                if (!string.IsNullOrEmpty(name))
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

            write.Export($"{dir}/", $"{clsName}_GenCode");
            AssetDatabase.Refresh();
        }
        static (string, string) _Parse(object obj)
        {
            var flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var typeName = obj.GetType().BaseType.GetField("m_FullTypeName", flag).GetValue(obj).ToString();
            typeName = typeName.Substring(typeName.LastIndexOf(".") + 1);
            IList properties = obj.GetType().BaseType.GetField("m_Properties", flag).GetValue(obj) as IList;
            bool name = false;
            foreach (var property in properties)
            {
                if (property.Equals("name"))
                {
                    name = true;
                    continue;
                }
                if (name)
                {
                    return (property.ToString(), typeName);
                }
            }
            return (null, null);
        }
        
    }

}

