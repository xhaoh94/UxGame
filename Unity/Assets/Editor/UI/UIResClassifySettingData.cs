using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.Editor
{
    [CreateAssetMenu(fileName = "UIResClassifySettingData", menuName = "Ux/UI/Create ResClassifySettings")]
    public class UIResClassifySettingData : ScriptableObject
    {
        [Serializable]
        public class ResClassify
        {            
            public string key;         
            public string value;
        }        
        public string path = "Assets/Data/Res/UI";

        [Header("不在内置资源或懒加载资源的，都是预加载资源")]                
        public string[] builtins;        
        public ResClassify[] lazyloads;

        public static List<string> GetLazyloadsByKeys(UIResClassifySettingData rc, List<string> pkgs)
        {
            if (pkgs == null || pkgs.Count == 0) return null;
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
    }

}
