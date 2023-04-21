using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
    [CreateAssetMenu(fileName = "UIResClassifySettingData", menuName = "UI/ResClassifySettings")]
    public class UIResClassifySettingData : ScriptableObject
    {
        [Serializable]
        public class ResClassify
        {
            [LabelText("资源包")]
            public string key;
            [LabelText("资源标签")]
            public string value;
        }
        [LabelText("UI资源目录")]
        public string path = "Assets/Data/Res/UI";

        [InfoBox("不在内置资源或懒加载资源的，都是预加载资源")]
        [LabelText("内置资源")]
        public string[] builtins;
        [LabelText("懒加载资源")]
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
