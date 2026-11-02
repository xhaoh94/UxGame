using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    [CreateAssetMenu(fileName = "UIClassifySettingData", menuName = "Ux/UI/Create ClassifySettings")]
    public class UIClassifySettingData : ScriptableObject
    {
        [Serializable]
        public class ResClassify
        {
            public string key;
            public string value;
        }
        public string path = "Assets/GameRes/Res/UI";

        [Header("不在预加载资源或懒加载资源的，都是内置资源，出包的时候会打进包体里的")]
        public string[] proloads;
        public ResClassify[] lazyloads;

        public static List<string> GetLazyloadsByKeys(List<string> pkgs)
        {
            if (pkgs == null || pkgs.Count == 0) return null;
            var rc = UIClassifyWindow.ResClassifySettings;
            var r = new List<string>();
            var temLazyloads = rc.lazyloads.ToList();
            foreach (var key in pkgs)
            {
                var lazyload = temLazyloads.Find(x => x.key == key);
                if (lazyload != null)
                {
                    var lazyloadSplit = lazyload.value.Split(';');
                    foreach (var item in lazyloadSplit)
                    {
                        if (r.Contains(item)) continue;
                        r.Add(item);
                    }
                }
            }
            return r;
        }

        public static bool IsProload(string path)
        {
            var rc = UIClassifyWindow.ResClassifySettings;
            var buildins = rc.proloads;
            for (int i = 0; i < buildins.Length; i++)
            {
                if (IsUnder(path, $"{rc.path}/{buildins[i]}"))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsLazyload(string path)
        {
            var rc = UIClassifyWindow.ResClassifySettings;
            var lazyloads = rc.lazyloads;
            for (int i = 0; i < lazyloads.Length; i++)
            {
                if (IsUnder(path, $"{rc.path}/{lazyloads[i].key}"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>目录本身或它的子目录都算命中：包目录下还会再分 Prefab/Atlas 子目录。</summary>
        private static bool IsUnder(string assetPath, string dir)
        {
            var dirName = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(dirName)) return false;
            dirName = dirName.Replace('\\', '/');
            return dirName == dir || dirName.StartsWith(dir + "/", StringComparison.Ordinal);
        }
    }

}
