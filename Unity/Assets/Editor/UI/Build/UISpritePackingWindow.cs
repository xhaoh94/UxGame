using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using YooAsset.Editor;
using static Ux.Editor.Build.UI.UIClassifySettingData;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 散图打包规则：UI/Texture 下默认按分桶打包，这里可以给个别子目录指定别的 PackRule（比如大背景图一张一 AB）。
    /// </summary>
    public class UISpritePackingWindow : EditorWindow
    {
        [MenuItem("UxGame/工具/UI/散图打包规则", false, 512)]
        public static void ShowWindow()
        {
            var window = GetWindow<UISpritePackingWindow>("散图打包规则", true);
            window.minSize = new Vector2(700, 400);
        }

        private static List<string> _packRules;

        private static List<string> PackRules
        {
            get
            {
                if (_packRules != null) return _packRules;

                _packRules = new List<string>();
                foreach (var type in EditorTools.GetAssignableTypes(typeof(IPackRule)))
                {
                    if (type.IsAbstract || type.IsInterface) continue;
                    _packRules.Add(type.Name);
                }

                _packRules.Sort(StringComparer.Ordinal);
                return _packRules;
            }
        }

        private VisualElement root;
        private Label textureRootLabel;
        private TextField searchField;
        private Button btnRescan;
        private Button btnReload;
        private Button btnAdd;
        private VisualElement ruleList;
        private Button btnApply;

        private List<string> _folders = new List<string>();

        private void BuildUI()
        {
            root = new VisualElement();
            root.style.flexGrow = 1f;

            var pathRow = new VisualElement();
            pathRow.style.flexDirection = FlexDirection.Row;
            pathRow.style.alignItems = Align.Center;
            textureRootLabel = new Label { name = "textureRoot" };
            textureRootLabel.style.flexGrow = 1f;
            pathRow.Add(textureRootLabel);
            btnReload = new Button(Reload) { name = "btnReload", text = "重载配置" };
            btnReload.style.width = 80f;
            pathRow.Add(btnReload);
            root.Add(pathRow);

            var toolRow = new VisualElement();
            toolRow.style.flexDirection = FlexDirection.Row;
            searchField = new TextField { name = "searchField" };
            searchField.style.flexGrow = 1f;
            searchField.RegisterValueChangedCallback(_ => RefreshChoices());
            toolRow.Add(searchField);
            btnRescan = new Button(RefreshFolders) { name = "btnRescan", text = "刷新目录" };
            btnRescan.style.width = 80f;
            toolRow.Add(btnRescan);
            root.Add(toolRow);

            var foldout = new Foldout { text = "子目录打包规则（没列出的走分桶）", value = true };

            var addRow = new VisualElement();
            addRow.style.flexDirection = FlexDirection.Row;
            btnAdd = new Button(AddRow) { name = "btnAdd", text = "[+]" };
            btnAdd.style.width = 30f;
            btnAdd.style.height = 20f;
            addRow.Add(btnAdd);
            foldout.Add(addRow);

            ruleList = new VisualElement { name = "ruleList" };
            foldout.Add(ruleList);
            root.Add(foldout);

            btnApply = new Button(Apply) { name = "btnApply", text = "生成配置" };
            btnApply.style.height = 40f;
            btnApply.style.marginTop = 10f;
            btnApply.style.backgroundColor = new Color(40f / 255f, 106f / 255f, 42f / 255f);
            root.Add(btnApply);
        }

        public void CreateGUI()
        {
            BuildUI();
            rootVisualElement.Add(root);

            var helpBox = new HelpBox(
                "UI/Texture 下默认按分桶打包（每桶固定张数）。需要一张一 AB 的大图之类的，加一条子目录规则，例如 " +
                "Standalone → PackSeparately。搜索框过滤下拉选项。",
                HelpBoxMessageType.Info);
            helpBox.style.fontSize = 24;
            root.Insert(0, helpBox);

            RefreshFolders();
            Reload();
        }

        private void Reload()
        {
            var settings = UIClassifyWindow.ResClassifySettings;
            textureRootLabel.text = settings == null
                ? "找不到 UIClassifySettingData"
                : $"资源目录：{settings.GetTextureRoot()}";

            ruleList.Clear();
            if (settings == null || settings.textureRules == null) return;

            foreach (var rule in settings.textureRules)
            {
                if (rule == null) continue;
                ruleList.Add(MakeRow(rule.folder, rule.packRule));
            }
        }

        private void AddRow()
        {
            ruleList.Add(MakeRow(NextUnusedFolder(), nameof(PackSeparately)));
        }

        /// <summary>新行默认选第一个还没被占用的子目录，避免加出来是个空下拉。</summary>
        private string NextUnusedFolder()
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in ruleList.Children())
            {
                var value = row.Q<DropdownField>("Folder")?.value;
                if (!string.IsNullOrEmpty(value)) used.Add(value);
            }

            foreach (var folder in _folders)
            {
                if (!used.Contains(folder)) return folder;
            }

            return string.Empty;
        }

        private void RefreshFolders()
        {
            var settings = UIClassifyWindow.ResClassifySettings;
            _folders = ScanFolders(settings == null ? null : settings.GetTextureRoot());
            RefreshChoices();
        }

        private static List<string> ScanFolders(string root)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return result;

            foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
            {
                var relative = dir.Replace('\\', '/').Substring(root.Length).TrimStart('/');
                if (!string.IsNullOrEmpty(relative)) result.Add(relative);
            }

            result.Sort(StringComparer.Ordinal);
            return result;
        }

        /// <summary>按搜索词过滤下拉选项；已选中的值即使不匹配也保留，避免选中项凭空消失。</summary>
        private void RefreshChoices()
        {
            foreach (var row in ruleList.Children())
            {
                var dropdown = row.Q<DropdownField>("Folder");
                if (dropdown == null) continue;

                var current = dropdown.value;
                var choices = BuildChoices(current);

                dropdown.choices = choices;
                dropdown.SetValueWithoutNotify(current);
            }
        }

        private List<string> BuildChoices(string current)
        {
            var keyword = searchField?.value?.Trim() ?? string.Empty;
            var choices = new List<string>();

            foreach (var folder in _folders)
            {
                if (string.IsNullOrEmpty(keyword) ||
                    folder.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    choices.Add(folder);
                }
            }

            if (!string.IsNullOrEmpty(current) && !choices.Contains(current)) choices.Insert(0, current);
            return choices;
        }

        private VisualElement MakeRow(string folder, string packRule)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var remove = new Button { name = "Sub", text = "[-]" };
            remove.style.width = 30f;
            remove.style.height = 20f;
            remove.clicked += () => row.RemoveFromHierarchy();
            row.Add(remove);

            var folderField = new DropdownField { name = "Folder" };
            folderField.style.flexGrow = 1f;
            folderField.choices = BuildChoices(folder);
            folderField.SetValueWithoutNotify(folder);
            row.Add(folderField);

            var ruleField = new DropdownField { name = "Rule" };
            ruleField.style.width = 240f;
            ruleField.style.flexShrink = 0f;
            ruleField.choices = PackRules;
            ruleField.SetValueWithoutNotify(string.IsNullOrEmpty(packRule) ? nameof(PackSeparately) : packRule);
            row.Add(ruleField);

            return row;
        }

        private void Apply()
        {
            var settings = UIClassifyWindow.ResClassifySettings;
            if (settings == null)
            {
                Log.Error("找不到 UIClassifySettingData：Assets/Settings/Build/UI");
                return;
            }

            var rules = new List<TextureRule>();
            var used = new HashSet<string>(StringComparer.Ordinal);

            foreach (var row in ruleList.Children())
            {
                var folder = row.Q<DropdownField>("Folder")?.value?.Trim().Trim('/');
                var packRule = row.Q<DropdownField>("Rule")?.value;
                if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(packRule)) continue;

                if (!used.Add(folder))
                {
                    Log.Warning($"散图打包规则：{folder} 重复配置，已跳过");
                    continue;
                }

                if (!Directory.Exists($"{settings.GetTextureRoot()}/{folder}"))
                {
                    Log.Warning($"散图打包规则：目录不存在 {settings.GetTextureRoot()}/{folder}");
                }

                rules.Add(new TextureRule { folder = folder, packRule = packRule });
            }

            settings.textureRules = rules.ToArray();
            EditorUtility.SetDirty(settings);
            UIClassifyWindow.CreateYooAssetUIGroup();

            if (EditorUtility.DisplayDialog("提示", "配置成功!", "ok"))
            {
                Reload();
            }
        }
    }
}
