#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    public enum UIBindMatch
    {
        /// <summary>完全等于。</summary>
        Exact = 0,
        /// <summary>以该文本开头。</summary>
        Prefix = 1,
        /// <summary>以该文本结尾。</summary>
        Suffix = 2,
        /// <summary>包含该文本。</summary>
        Contains = 3,
    }

    [Serializable]
    public sealed class UIBindRule
    {
        public bool Enabled = true;
        public UIBindMatch Match = UIBindMatch.Prefix;
        public string Pattern;
        public string FieldType;

        public UIBindRule Clone()
        {
            return new UIBindRule
            {
                Enabled = Enabled,
                Match = Match,
                Pattern = Pattern,
                FieldType = FieldType,
            };
        }

        public bool Matches(string nodeName)
        {
            if (!Enabled || string.IsNullOrEmpty(Pattern) || string.IsNullOrEmpty(FieldType))
                return false;

            var name = nodeName ?? string.Empty;
            return Match switch
            {
                UIBindMatch.Exact => string.Equals(name, Pattern, StringComparison.OrdinalIgnoreCase),
                UIBindMatch.Prefix => name.StartsWith(Pattern, StringComparison.OrdinalIgnoreCase),
                UIBindMatch.Suffix => name.EndsWith(Pattern, StringComparison.OrdinalIgnoreCase),
                UIBindMatch.Contains => name.IndexOf(Pattern, StringComparison.OrdinalIgnoreCase) >= 0,
                _ => false,
            };
        }
    }

    /// <summary>
    /// 命名规则配置：节点名如何映射到字段类型。
    /// 列表顺序即优先级，靠前的先命中。
    /// </summary>
    public sealed class UIBindNamingSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/Settings/Build/UI/UIBindNamingSettings.asset";

        [SerializeField] private List<UIBindRule> _rules = new();

        [Tooltip("YooAsset 地址前缀（AddressByGroupAndFileName 的 GroupName），生成地址为 {GroupName}_{PrefabName}。")]
        [SerializeField] private string _addressGroupName = "UI";

        public List<UIBindRule> Rules => _rules;

        /// <summary>资源地址分组前缀，默认 UI，对应 YooAsset 收集器组名。</summary>
        public string AddressGroupName => string.IsNullOrEmpty(_addressGroupName) ? "UI" : _addressGroupName;

        private static UIBindNamingSettings _instance;

        public static UIBindNamingSettings Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = AssetDatabase.LoadAssetAtPath<UIBindNamingSettings>(AssetPath);
                if (_instance != null)
                {
                    if (_instance._rules == null || _instance._rules.Count == 0)
                        _instance.ResetToDefault();
                    MigrateLegacyRules(_instance);
                    return _instance;
                }

                // 资产文件存在但加载失败（YAML 损坏）：绝不能新建资产覆盖，
                // 否则用户已配的命名规则会静默丢失。用内存默认规则兜底并要求修好文件。
                if (System.IO.File.Exists(AssetPath))
                {
                    Debug.LogError($"[UI 命名规则] {AssetPath} 存在但无法解析（YAML 损坏）。" +
                                   "已使用内存默认规则且不会写盘，请先修好该文件的 YAML。");
                    _instance = CreateInstance<UIBindNamingSettings>();
                    _instance._rules = CreateDefaultRules();
                    return _instance;
                }

                var directory = System.IO.Path.GetDirectoryName(AssetPath);
                if (!string.IsNullOrEmpty(directory))
                    System.IO.Directory.CreateDirectory(directory);

                _instance = CreateInstance<UIBindNamingSettings>();
                _instance._rules = CreateDefaultRules();
                AssetDatabase.CreateAsset(_instance, AssetPath);
                AssetDatabase.SaveAssets();
                return _instance;
            }
        }

        /// <summary>旧版本配置迁移：按钮字段从 Button 改为 GameObject。事件由生成面板配置。</summary>
        private static void MigrateLegacyRules(UIBindNamingSettings settings)
        {
            if (settings?._rules == null) return;

            var changed = false;
            for (int i = 0; i < settings._rules.Count; i++)
            {
                var rule = settings._rules[i];
                if (rule == null || rule.FieldType != "Button") continue;
                rule.FieldType = "GameObject";
                changed = true;
            }

            // 窗口框架预制：已有工程的命名规则资产不会自动重置，这里补上新规则，
            // 否则生成器识别不到 CommonWindowFrame，窗口里放框架也不会生成绑定。
            if (!HasExactRule(settings._rules, "CommonWindowFrame"))
            {
                settings._rules.Add(new UIBindRule
                {
                    Enabled = true,
                    Match = UIBindMatch.Exact,
                    Pattern = "CommonWindowFrame",
                    FieldType = "CommonWindowFrame",
                });
                changed = true;
            }

            if (!changed) return;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static bool HasExactRule(List<UIBindRule> rules, string pattern)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule != null && rule.Match == UIBindMatch.Exact && rule.Pattern == pattern)
                    return true;
            }
            return false;
        }

        // 仅编辑器工具内部使用；避免每次访问都走 AssetDatabase。
        internal static void InvalidateCache() => _instance = null;

        public void ResetToDefault()
        {
            _rules = CreateDefaultRules();
            EditorUtility.SetDirty(this);
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>内置默认规则，按优先级排列。</summary>
        public static List<UIBindRule> CreateDefaultRules()
        {
            var list = new List<UIBindRule>();

            void Add(UIBindMatch match, string pattern, string type)
            {
                list.Add(new UIBindRule
                {
                    Enabled = true,
                    Match = match,
                    Pattern = pattern,
                    FieldType = type,
                });
            }

            // 1. Wrapper 类型：由 Prefab 生成器直接以类型名命名，必须精确匹配。
            Add(UIBindMatch.Exact, "CommonUIModel", "CommonUIModel");
            Add(UIBindMatch.Exact, "CommonRTModel", "CommonRTModel");
            Add(UIBindMatch.Exact, "Common1TabFrame", "Common1TabFrame");
            Add(UIBindMatch.Exact, "Common2TabFrame", "Common2TabFrame");
            Add(UIBindMatch.Exact, "CommonTabItem", "CommonTabItem");
            Add(UIBindMatch.Exact, "TestItem", "TestItem");
            Add(UIBindMatch.Exact, "Test22Item", "Test22Item");
            Add(UIBindMatch.Exact, "UIModel", "UIModel");
            Add(UIBindMatch.Exact, "RTModel", "RTModel");
            Add(UIBindMatch.Exact, "UITabFrame", "UITabFrame");
            // 窗口框架预制：由 UIWindowFrame 派生，靠 Exact 规则让生成器识别为字段。
            Add(UIBindMatch.Exact, "CommonWindowFrame", "CommonWindowFrame");

            // 2. 常见前缀。
            // 按钮统一用 GameObject：AddClick/AddMultipleClick/AddLongPress 都接收 GameObject。
            Add(UIBindMatch.Prefix, "btn", "GameObject");
            Add(UIBindMatch.Prefix, "button", "GameObject");
            Add(UIBindMatch.Prefix, "txt", "TMP_Text");
            Add(UIBindMatch.Prefix, "text", "TMP_Text");
            Add(UIBindMatch.Prefix, "label", "TMP_Text");
            Add(UIBindMatch.Prefix, "img", "Image");
            Add(UIBindMatch.Prefix, "image", "Image");
            Add(UIBindMatch.Prefix, "icon", "Image");
            Add(UIBindMatch.Prefix, "tog", "Toggle");
            Add(UIBindMatch.Prefix, "toggle", "Toggle");
            Add(UIBindMatch.Prefix, "check", "Toggle");
            Add(UIBindMatch.Prefix, "ipt", "TMP_InputField");
            Add(UIBindMatch.Prefix, "input", "TMP_InputField");
            Add(UIBindMatch.Prefix, "list", "UIList");
            Add(UIBindMatch.Prefix, "slider", "UIProgressBar");
            Add(UIBindMatch.Prefix, "progress", "UIProgressBar");
            Add(UIBindMatch.Prefix, "model", "UIModel");
            Add(UIBindMatch.Prefix, "rtmodel", "RTModel");
            Add(UIBindMatch.Prefix, "tabframe", "UITabFrame");
            Add(UIBindMatch.Prefix, "tabcontent", "RectTransform");
            Add(UIBindMatch.Prefix, "panel", "RectTransform");
            Add(UIBindMatch.Prefix, "container", "RectTransform");
            Add(UIBindMatch.Prefix, "root", "RectTransform");

            // 3. 常见后缀（美术习惯写法）。
            Add(UIBindMatch.Suffix, "Button", "GameObject");
            Add(UIBindMatch.Suffix, "Btn", "GameObject");
            Add(UIBindMatch.Suffix, "Text", "TMP_Text");
            Add(UIBindMatch.Suffix, "Label", "TMP_Text");
            Add(UIBindMatch.Suffix, "Title", "TMP_Text");
            Add(UIBindMatch.Suffix, "Image", "Image");
            Add(UIBindMatch.Suffix, "Icon", "Image");
            Add(UIBindMatch.Suffix, "Toggle", "Toggle");
            Add(UIBindMatch.Suffix, "List", "UIList");
            Add(UIBindMatch.Suffix, "Slider", "UIProgressBar");
            Add(UIBindMatch.Suffix, "Panel", "RectTransform");

            // 4. 包含匹配。
            Add(UIBindMatch.Contains, "uimodel", "UIModel");
            Add(UIBindMatch.Contains, "rtmodel", "RTModel");
            Add(UIBindMatch.Contains, "tabframe", "UITabFrame");
            Add(UIBindMatch.Contains, "tabcontent", "RectTransform");

            return list;
        }
    }
}
#endif
