#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 命名规则配置：节点名 -> 组件类型。
    /// 列表顺序即优先级，靠前的规则先命中。
    /// </summary>
    public sealed class UIBindNamingWindow : EditorWindow
    {
        private static readonly string[] CommonTypes =
        {
            "GameObject",
            "TMP_Text",
            "Text",
            "Button",
            "Toggle",
            "Image",
            "RawImage",
            "InputField",
            "TMP_InputField",
            "RectTransform",
            "UIList",
            "UIProgressBar",
            "UIModel",
            "RTModel",
            "CommonUIModel",
            "CommonRTModel",
            "UITabFrame",
            "Common1TabFrame",
            "Common2TabFrame",
            "CommonTabItem",
            "TestItem",
            "Test22Item",
        };

        private static readonly string[] MatchNames = { "完全等于", "前缀", "后缀", "包含" };

        private Vector2 _scroll;
        private string _previewName = "btnBack";
        private bool _dirty;

        [MenuItem("UxGame/工具/UI/代码生成规则", false, 513)]
        public static void ShowWindow()
        {
            var window = GetWindow<UIBindNamingWindow>("UI 命名规则", true);
            window.minSize = new Vector2(760f, 560f);
        }

        private void OnEnable()
        {
            // 触发一次加载，确保配置文件存在。
            _ = UIBindNamingSettings.Instance;
        }

        private void OnGUI()
        {
            var settings = UIBindNamingSettings.Instance;

            DrawToolbar(settings);
            DrawPreview();
            EditorGUILayout.Space(2f);
            DrawRules(settings);
            DrawFooter(settings);
        }

        private void DrawToolbar(UIBindNamingSettings settings)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("新增规则", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    settings.Rules.Add(new UIBindRule
                    {
                        Enabled = true,
                        Match = UIBindMatch.Prefix,
                        Pattern = "new",
                        FieldType = "GameObject",
                    });
                    _dirty = true;
                }

                if (GUILayout.Button("重置为默认", EditorStyles.toolbarButton, GUILayout.Width(85f)))
                {
                    if (EditorUtility.DisplayDialog("命名规则", "确定恢复默认规则？当前修改会丢失。", "恢复", "取消"))
                    {
                        settings.ResetToDefault();
                        settings.Save();
                        _dirty = false;
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label($"规则 {settings.Rules.Count} 条，自上而下匹配", EditorStyles.miniLabel);
            }
        }

        private void DrawPreview()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("规则预览", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("节点名", GUILayout.Width(50f));
                    _previewName = EditorGUILayout.TextField(_previewName, GUILayout.Width(220f));

                    var type = UIBindNaming.ResolveType(_previewName, null, out _, out _);
                    var hit = UIBindNaming.FindRule(_previewName);

                    if (string.IsNullOrEmpty(type))
                    {
                        EditorGUILayout.LabelField("-> 不生成（未命中）", EditorStyles.miniBoldLabel);
                    }
                    else
                    {
                        var ruleText = hit == null
                            ? "组件兜底"
                            : $"{MatchNames[(int)hit.Match]} \"{hit.Pattern}\"";
                        EditorGUILayout.LabelField($"-> {type}    命中：{ruleText}", EditorStyles.miniBoldLabel);
                        EditorGUILayout.LabelField(
                            $"字段：{UIBindNaming.ToFieldName(_previewName, type)}",
                            EditorStyles.miniLabel);
                    }
                }
            }
        }

        private void DrawRules(UIBindNamingSettings settings)
        {
            using (var scope = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scope.scrollPosition;

                var rules = settings.Rules;
                for (var i = 0; i < rules.Count; i++)
                {
                    var rule = rules[i];
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label($"{i + 1}.", EditorStyles.miniLabel, GUILayout.Width(24f));

                        var enabled = EditorGUILayout.Toggle(rule.Enabled, GUILayout.Width(18f));
                        if (enabled != rule.Enabled)
                        {
                            rule.Enabled = enabled;
                            _dirty = true;
                        }

                        var match = (UIBindMatch)EditorGUILayout.Popup(
                            (int)rule.Match, MatchNames, GUILayout.Width(90f));
                        if (match != rule.Match)
                        {
                            rule.Match = match;
                            _dirty = true;
                        }

                        if (rule.Match != UIBindMatch.Exact)
                            GUILayout.Label("节点名", EditorStyles.miniLabel, GUILayout.Width(36f));

                        var pattern = EditorGUILayout.TextField(rule.Pattern ?? string.Empty, GUILayout.Width(150f));
                        if (pattern != rule.Pattern)
                        {
                            rule.Pattern = pattern;
                            _dirty = true;
                        }

                        GUILayout.Label("->", GUILayout.Width(20f));

                        var typeIndex = Array.IndexOf(CommonTypes, rule.FieldType);
                        var nextIndex = EditorGUILayout.Popup(
                            typeIndex < 0 ? 0 : typeIndex, CommonTypes, GUILayout.Width(140f));
                        if (nextIndex != typeIndex)
                        {
                            rule.FieldType = CommonTypes[nextIndex];
                            _dirty = true;
                        }

                        var customType = EditorGUILayout.TextField(
                            rule.FieldType ?? string.Empty, GUILayout.Width(150f));
                        if (customType != rule.FieldType)
                        {
                            rule.FieldType = customType;
                            _dirty = true;
                        }

                        GUI.enabled = i > 0;
                        if (GUILayout.Button("↑", GUILayout.Width(24f)))
                        {
                            (rules[i - 1], rules[i]) = (rules[i], rules[i - 1]);
                            _dirty = true;
                        }
                        GUI.enabled = true;

                        GUI.enabled = i < rules.Count - 1;
                        if (GUILayout.Button("↓", GUILayout.Width(24f)))
                        {
                            (rules[i + 1], rules[i]) = (rules[i], rules[i + 1]);
                            _dirty = true;
                        }
                        GUI.enabled = true;

                        if (GUILayout.Button("删除", GUILayout.Width(44f)))
                        {
                            rules.RemoveAt(i);
                            _dirty = true;
                            break;
                        }
                    }
                }
            }
        }

        private void DrawFooter(UIBindNamingSettings settings)
        {
            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _dirty;
                if (GUILayout.Button("保存", GUILayout.Height(28f)))
                {
                    settings.Save();
                    _dirty = false;
                }
                GUI.enabled = true;

                if (GUILayout.Button("打开配置文件", GUILayout.Height(28f)))
                    Selection.activeObject = settings;
            }

            EditorGUILayout.LabelField(
                $"配置文件：{UIBindNamingSettings.AssetPath}",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "说明：_ 开头的节点及其子树不参与绑定；未命中任何规则时不生成字段。",
                EditorStyles.miniLabel);
        }
    }
}
#endif
