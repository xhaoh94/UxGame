using System;
using System.Collections.Generic;
using System.IO;
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
        public static List<T> GetAssets<T>(string path) where T : ScriptableObject, new()
        {
            List<T> assets = new List<T>();
            //获取指定路径下面的所有资源文件  
            if (Directory.Exists(path))
            {
                DirectoryInfo direction = new DirectoryInfo(path);
                FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    //忽略关联文件
                    if (files[i].Name.EndsWith(".meta"))
                    {
                        continue;
                    }
                    //Debug.Log("文件名:" + files[i].Name);
                    //Debug.Log("文件绝对路径:" + files[i].FullName);
                    //Debug.Log("文件所在目录:" + files[i].DirectoryName);
                    var fPath = files[i].FullName.Replace("\\","/");
                    fPath = fPath.Substring(fPath.IndexOf("Assets"));
                    var asset =AssetDatabase.LoadAssetAtPath<T>(fPath);
                    assets.Add(asset);
                }
            }
            return assets;
        }
        public static T GetPlayerPrefs<T>(string key) where T : UnityEngine.Object
        {
            var id = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            string assPath = AssetDatabase.GUIDToAssetPath(id);
            return AssetDatabase.LoadAssetAtPath<T>(assPath);
        }

        public static void SavePlayerPrefs(string key, string id)
        {
            PlayerPrefs.SetString(key, id);
            PlayerPrefs.Save();
        }
    }
}