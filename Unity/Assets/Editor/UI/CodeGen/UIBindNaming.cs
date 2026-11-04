#if UNITY_EDITOR
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 命名约定绑定规则：节点名 -> 组件类型。
    ///
    /// 规则完全来自 UGUIBindNamingSettings（可在 UxGame/工具/UI/命名规则 中编辑），
    /// 这里只负责读取与兜底：
    /// 1. "_" 开头        -> 忽略该节点与整棵子树
    /// 2. 配置规则按顺序匹配（完全等于 / 前缀 / 后缀 / 包含）
    /// 3. 都没命中        -> 再看组件做兜底
    /// 4. 仍无结果        -> 不生成
    /// </summary>
    public static class UIBindNaming
    {
        public const string IgnorePrefix = "_";

        /// <summary>节点是否应被忽略（_ 前缀）。</summary>
        public static bool ShouldIgnore(string nodeName)
        {
            return !string.IsNullOrEmpty(nodeName) &&
                   nodeName.StartsWith(IgnorePrefix, System.StringComparison.Ordinal);
        }

        /// <summary>返回命中的规则；未命中返回 null。</summary>
        public static UIBindRule FindRule(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName) || ShouldIgnore(nodeName))
                return null;

            var settings = UIBindNamingSettings.Instance;
            var rules = settings.Rules;
            for (var i = 0; i < rules.Count; i++)
            {
                if (rules[i] != null && rules[i].Matches(nodeName))
                    return rules[i];
            }
            return null;
        }

        /// <summary>
        /// 按命名规则推断字段类型；返回 null 表示不生成。
        /// </summary>
        /// <summary>
        /// 只按命名规则解析字段类型；没命中规则就不生成字段。
        /// clickable / listItemClick 仅用于编辑器预览，不参与生成决策。
        /// </summary>
        public static string ResolveType(string nodeName, GameObject node,
            out bool clickable, out bool listItemClick)
        {
            clickable = node != null && node.GetComponent<UnityEngine.UI.Button>() != null;
            listItemClick = node != null && node.GetComponent<UIVirtualListComponent>() != null;

            if (ShouldIgnore(nodeName))
                return null;

            return FindRule(nodeName)?.FieldType;
        }


        /// <summary>
        /// 字段名 = 节点名首字母小写（保留前缀）。
        /// Wrapper 节点名本身就是类型名，转成对应字段名。
        /// </summary>
        public static string ToFieldName(string nodeName, string type)
        {
            if (string.IsNullOrEmpty(nodeName))
                return "node";

            var name = nodeName;

            if (!string.IsNullOrEmpty(type) &&
                string.Equals(name, type, System.StringComparison.OrdinalIgnoreCase))
            {
                name = type;
            }

            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}
#endif
