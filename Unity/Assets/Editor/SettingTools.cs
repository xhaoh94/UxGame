using System;
using UnityEditor;
using UnityEngine;

public class EditorDefine
{
#if UNITY_2019_4_OR_NEWER
    /// <summary>
    /// 停靠窗口类型集合
    /// </summary>
    public static readonly Type[] DebuggerWindowTypes =
    {
        typeof(UIDebuggerWindow),
        typeof(ResDebuggerWindow),
        typeof(EventDebuggerWindow),
        typeof(TimeDebuggerWindow),
    };
#endif
}
public class SettingTools
{
    [MenuItem("Assets/Cov/MyItem", false)]
    public static void Test()
    {
        if (Selection.gameObjects.Length > 0)
        {
            Debug.Log(Selection.activeGameObject.name);
            var go = UnityEngine.GameObject.Instantiate(Selection.activeGameObject);
            var tr = go.transform.GetChild(0);
            tr = tr.GetChild(0);
            _Tset(tr);
            var p = AssetDatabase.GetAssetPath(Selection.activeGameObject);
            PrefabUtility.SaveAsPrefabAsset(go, p);
            UnityEngine.Object.DestroyImmediate(go);
        }
        else
        {
            Debug.Log("请选择至少一个游戏物体");
        }
    }
    static void _Tset(Transform transform)
    {
        transform.name = transform.name.Replace("_", " ");
        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                _Tset(child);
            }
        }
    }

    public static T GetSingletonAssets<T>(string path = "Assets", bool isCreate = true) where T : ScriptableObject, new()
    {
        string assetType = typeof(T).Name;
        string[] globalAssetPaths = AssetDatabase.FindAssets($"t:{assetType}");
        if (globalAssetPaths == null || globalAssetPaths.Length == 0)
        {
            if (!isCreate) return null;
            Debug.LogWarning($"没找到 {assetType} asset，自动创建创建一个:{assetType}.");
            var newAsset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(newAsset, $"{path}/{assetType}.asset");
            return newAsset;
        }
        if (globalAssetPaths.Length > 1)
        {
            foreach (var assetPath in globalAssetPaths)
            {
                Debug.LogError($"不能有多个 {assetType}. 路径: {AssetDatabase.GUIDToAssetPath(assetPath)}");
            }
            throw new Exception($"不能有多个 {assetType}");
        }
        string assPath = AssetDatabase.GUIDToAssetPath(globalAssetPaths[0]);
        return AssetDatabase.LoadAssetAtPath<T>(assPath);
    }
}