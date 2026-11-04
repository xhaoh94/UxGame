using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    [CreateAssetMenu(fileName = "UIClassifySettingData", menuName = "Ux/UI/Create ClassifySettings")]
    public class UIClassifySettingData : ScriptableObject
    {
        /// <summary>
        /// 一个模块目录的归类。tag 为空表示预加载（默认），填 builtin 表示进首包，其余值就是下载标签。
        /// </summary>
        [Serializable]
        public class UIModule
        {
            public string name;
            public string tag;
        }

        /// <summary>散图子目录的打包规则覆盖。没列出的子目录按分桶打包。</summary>
        [Serializable]
        public class TextureRule
        {
            public string folder;
            public string packRule;
        }

        [Serializable]
        public class ResClassify
        {
            public string key;
            public string value;
        }

        public string path = "Assets/GameRes/Res/UI";

        /// <summary>散图保留目录名：UI/Texture 不算模块，由独立收集器 + 分桶打包处理。</summary>
        public const string LooseTextureFolderName = "Texture";

        [Header("默认归类：没有单独配置的模块都按这个 tag 走")]
        public string preloadTag = "preload";

        public string builtinTag = "builtin";

        [Header("模块 -> 归类：只给需要偏离默认值的模块建条目")]
        public UIModule[] modules;

        [Header("散图打包规则：只给需要偏离分桶的子目录建条目")]
        public TextureRule[] textureRules;

        // 旧字段：只用于把老的「内置/预加载/懒加载」三档翻译成模块 tag，迁移后置空。
        [SerializeField, HideInInspector] private string[] proloads;
        [SerializeField, HideInInspector] private ResClassify[] lazyloads;

        /// <summary>
        /// 旧配置迁移：老的内置（没列出的）→ 显式 builtin 条目，老的 lazyloads → 各自的标签。
        /// 老的 proloads 在新模型里就是默认值，不需要落条目。
        /// </summary>
        public void MigrateLegacy()
        {
            if (modules != null) return;

            var lazyloadTags = new Dictionary<string, string>();
            if (lazyloads != null)
            {
                foreach (var item in lazyloads)
                {
                    if (item == null || string.IsNullOrEmpty(item.key)) continue;
                    lazyloadTags[item.key] = item.value;
                }
            }

            var preloads = new HashSet<string>();
            if (proloads != null)
            {
                foreach (var item in proloads)
                {
                    if (!string.IsNullOrEmpty(item)) preloads.Add(item);
                }
            }

            var list = new List<UIModule>();
            foreach (var name in ScanModules())
            {
                if (lazyloadTags.TryGetValue(name, out var lazyloadTag))
                {
                    list.Add(new UIModule { name = name, tag = lazyloadTag });
                }
                else if (preloads.Contains(name))
                {
                    // 默认就是 preload，不用落条目
                }
                else
                {
                    list.Add(new UIModule { name = name, tag = builtinTag });
                }
            }

            modules = list.ToArray();
            proloads = null;
            lazyloads = null;
        }

        public string GetTag(string moduleName)
        {
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    if (module == null || module.name != moduleName) continue;
                    return string.IsNullOrEmpty(module.tag) ? preloadTag : module.tag;
                }
            }

            return preloadTag;
        }

        /// <summary>取资源所属的模块名（path 下的第一段目录）；不在 path 下返回 null。</summary>
        public string GetModuleNameOfAsset(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(dir)) return null;
            dir = dir.Replace('\\', '/');

            var root = path.TrimEnd('/') + "/";
            if (!dir.StartsWith(root, StringComparison.Ordinal)) return null;

            var rest = dir.Substring(root.Length);
            var slash = rest.IndexOf('/');
            return slash < 0 ? rest : rest.Substring(0, slash);
        }

        /// <summary>资源所属模块的归类 tag；没配置的模块返回 preloadTag。</summary>
        public string GetAssetTag(string assetPath)
        {
            var moduleName = GetModuleNameOfAsset(assetPath);
            return string.IsNullOrEmpty(moduleName) ? preloadTag : GetTag(moduleName);
        }

        /// <summary>散图根目录：path/Texture。</summary>
        public string GetTextureRoot()
        {
            return $"{path}/{LooseTextureFolderName}";
        }

        /// <summary>资源是否落在某个「指定了打包规则」的散图子目录下。</summary>
        public bool IsUnderTextureRule(string assetPath)
        {
            if (textureRules == null || textureRules.Length == 0) return false;

            var root = GetTextureRoot() + "/";
            var p = assetPath.Replace('\\', '/');
            if (!p.StartsWith(root, StringComparison.Ordinal)) return false;

            foreach (var rule in textureRules)
            {
                if (rule == null || string.IsNullOrEmpty(rule.folder)) continue;

                var prefix = root + rule.folder.Trim('/') + "/";
                if (p.StartsWith(prefix, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        /// <summary>扫描 path 下的模块目录，按名字排序。Texture 是散图保留目录，不算模块。</summary>
        public List<string> ScanModules()
        {
            var result = new List<string>();
            if (!Directory.Exists(path)) return result;

            var dirs = Directory.GetDirectories(path);
            Array.Sort(dirs, StringComparer.Ordinal);
            foreach (var dir in dirs)
            {
                var name = Path.GetFileName(dir);
                if (string.IsNullOrEmpty(name) || name.StartsWith(".")) continue;
                if (name == LooseTextureFolderName) continue;
                result.Add(name);
            }

            return result;
        }
    }
}
