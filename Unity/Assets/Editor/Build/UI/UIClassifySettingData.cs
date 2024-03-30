using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UI.Editor
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
        public string path = "Assets/Data/Res/UI";

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
            var dirName = Path.GetDirectoryName(path);
            dirName = dirName.Replace('\\', '/');
            var buildins = rc.proloads;
            var uiPath = rc.path;
            foreach (var buildin in buildins)
            {
                var dir = $"{uiPath}/{buildin}";
                if (dirName == dir)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsLazyload(string path)
        {
            var rc = UIClassifyWindow.ResClassifySettings;
            var dirName = Path.GetDirectoryName(path);
            dirName = dirName.Replace('\\', '/');

            var lazyloads = rc.lazyloads;
            var uiPath = rc.path;
            foreach (var lazyload in lazyloads)
            {
                var dir = $"{uiPath}/{lazyload.key}";
                if (dirName == dir)
                {
                    return true;
                }
            }
            return false;
        }
    }

}
