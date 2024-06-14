using System;
using UnityEditor;
using UnityEngine;
namespace Ux.Editor
{
    public class SettingTools
    {
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
}