using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor
{
    public static class DymTools
    {
        [MenuItem("Assets/UIElements/CodeGen", false)]
        public static void Test()
        {
            var tree = (Selection.activeObject as VisualTreeAsset);
            var ve = new VisualElement();
            tree.CloneTree(ve);            
            for (int i = 0; i < ve.childCount; i++) { 
                var child = ve.ElementAt(i);
                Debug.Log(child);            
            }
            //if (Selection.gameObjects.Length > 0)
            //{                
            //    var go = UnityEngine.GameObject.Instantiate(Selection.activeGameObject);
            //    var tr = go.transform.GetChild(0);
            //    tr = tr.GetChild(0);
            //    _Tset(tr);
            //    var p = AssetDatabase.GetAssetPath(Selection.activeGameObject);
            //    PrefabUtility.SaveAsPrefabAsset(go, p);
            //    UnityEngine.Object.DestroyImmediate(go);
            //}
            //else
            //{
            //    Debug.Log("请选择至少一个游戏物体");
            //}
        }

        public static void DymBind(this VisualElement root,object obj)
        {
            var t = obj.GetType();
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<BindFieldAttributeAttribute>();
                if (attr != null)
                {
                    var name = attr.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = field.Name;
                    }
                    var f =  root.Q(name);
                    field.SetValue(obj, f);
                }
            }
        }
    }

}

