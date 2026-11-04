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
    public partial class UIClassifyWindow : EditorWindow
    {
        private const string PackageName = "MainPackage";
        private const string GroupName = "UI";
        private const string TextureGroupName = "Texture";

        /// <summary>目录下拉的固定列宽，保证多行之间标签列对齐。</summary>
        private const float DirColumnWidth = 260f;

        [MenuItem("UxGame/工具/UI/UI打包规则", false, 511)]
        public static void ShowExample()
        {
            var window = GetWindow<UIClassifyWindow>("UIClassifyWindow", true);
            window.minSize = new Vector2(800, 600);
        }

        /// <summary>
        /// 重建收集器：UI 组（图集源图 Static + builtin 合并 + preload 合并 + lazyload 每模块一条），
        /// 散图单独一个 UITexture 组（分桶 + 指定规则）。收集器是生成物，不要在 YooAsset 面板里手改。
        /// </summary>
        public static void CreateYooAssetUIGroup()
        {
            var settings = ResClassifySettings;
            if (settings == null)
            {
                Log.Error("找不到 UIClassifySettingData：Assets/Settings/Build/UI");
                return;
            }

            var packages = AssetBundleCollectorSettingData.Setting.Packages;
            var package = packages.Find(x => x.PackageName == PackageName);
            if (package == null)
            {
                package = new AssetBundleCollectorPackage
                {
                    PackageName = PackageName,
                    PackageDesc = "主包",
                };
                packages.Add(package);
            }

            // 旧组名清理：散图组曾叫 UITexture，残留下来会和 Texture 组重复收集（构建直接抛异常）。
            var legacyGroup = package.Groups.Find(x => x.GroupName == "UITexture");
            if (legacyGroup != null) package.Groups.Remove(legacyGroup);

            var uiGroup = GetOrCreateGroup(package, GroupName, "UI资源");
            var textureGroup = GetOrCreateGroup(package, TextureGroupName, "UI散图");

            uiGroup.Collectors.Clear();
            textureGroup.Collectors.Clear();

            // 散图分桶：先补登记新图，保证桶号分配稳定（老桶内容不会因为新增一张图而整体搬动）。
            LooseAssetBucketTable.Refresh();

            // ① 图集源图：整个 UI 一条 Static。不写清单、不产生地址，所以不同模块的同名图不会撞名。
            var atlasSource = NewCollector(settings.path, typeof(CollectAtlasSource).Name, string.Empty,
                nameof(AddressByGroupAndFileName));
            atlasSource.CollectorType = ECollectorType.StaticAssetCollector;
            uiGroup.Collectors.Add(atlasSource);

            // ② builtin 合并成一条，③ preload（含没配置的模块）合并成一条。
            uiGroup.Collectors.Add(NewCollector(settings.path, typeof(CollectBuiltinUI).Name,
                settings.builtinTag, nameof(AddressByGroupOrAtlas)));
            uiGroup.Collectors.Add(NewCollector(settings.path, typeof(CollectPreloadUI).Name,
                settings.preloadTag, nameof(AddressByGroupOrAtlas)));

            // ④ 散图走独立组：UI/Texture 默认分桶；指定了别的打包规则的子目录各占一条。
            var looseTextureDir = settings.GetTextureRoot();
            if (Directory.Exists(looseTextureDir))
            {
                textureGroup.Collectors.Add(NewCollector(looseTextureDir, typeof(CollectLooseTexture).Name,
                    settings.preloadTag, nameof(AddressByGroupOrAtlas), nameof(PackLooseAssetByBucket)));

                if (settings.textureRules != null)
                {
                    foreach (var rule in settings.textureRules)
                    {
                        if (rule == null || string.IsNullOrEmpty(rule.folder) || string.IsNullOrEmpty(rule.packRule))
                            continue;

                        var ruleDir = $"{looseTextureDir}/{rule.folder.Trim('/')}";
                        if (!Directory.Exists(ruleDir))
                        {
                            Log.Warning($"散图打包规则：目录不存在 {ruleDir}");
                            continue;
                        }

                        textureGroup.Collectors.Add(NewCollector(ruleDir, nameof(CollectAll), settings.preloadTag,
                            nameof(AddressByGroupOrAtlas), rule.packRule));
                    }
                }
            }

            // ⑤ 打了 lazyload 标签的模块各占一条：预制 → UI_名字，图集本体 → Atlas_名字（地址规则按扩展名区分）。
            // 图集本体是包里的 Main 资源，负责把模块 tag 传染给整个图集包。
            if (settings.modules != null)
            {
                foreach (var module in settings.modules)
                {
                    if (module == null || string.IsNullOrEmpty(module.name) || string.IsNullOrEmpty(module.tag)) continue;
                    if (module.tag == settings.builtinTag || module.tag == settings.preloadTag) continue;

                    var moduleDir = $"{settings.path}/{module.name}";
                    if (!Directory.Exists(moduleDir))
                    {
                        Log.Warning($"资源分类：模块目录不存在 {moduleDir}");
                        continue;
                    }

                    uiGroup.Collectors.Add(NewCollector(moduleDir, typeof(CollectAssetUI).Name, module.tag,
                        nameof(AddressByGroupOrAtlas)));
                }
            }

            EditorUtility.SetDirty(settings);
            AssetBundleCollectorSettingData.SaveFile();
        }

        private static AssetBundleCollectorGroup GetOrCreateGroup(AssetBundleCollectorPackage package,
            string groupName, string desc)
        {
            var group = package.Groups.Find(x => x.GroupName == groupName);
            if (group != null) return group;

            group = new AssetBundleCollectorGroup
            {
                GroupName = groupName,
                GroupDesc = desc,
                AssetTags = string.Empty,
            };
            package.Groups.Add(group);
            return group;
        }

        private static AssetBundleCollector NewCollector(string collectPath, string filterRule, string tags,
            string addressRule, string packRule = null)
        {
            return new AssetBundleCollector
            {
                CollectPath = collectPath,
                FilterRuleName = filterRule,
                AssetTags = tags,
                AddressRuleName = addressRule,
                PackRuleName = string.IsNullOrEmpty(packRule) ? nameof(PackDirectory) : packRule,
            };
        }

        static UIClassifySettingData _ResClassifySettings;

        public static UIClassifySettingData ResClassifySettings
        {
            get
            {
                if (_ResClassifySettings == null)
                {
                    _ResClassifySettings =
                        SettingTools.GetSingletonAssets<UIClassifySettingData>("Assets/Settings/Build/UI");
                    _ResClassifySettings?.MigrateLegacy();
                }

                return _ResClassifySettings;
            }
        }

        protected VisualElement root;
        public ObjectField pathField;
        public Button btnReload;
        public TextField searchField;
        public Button btnRescan;
        public Button btnAddBuiltin;
        public Button btnAddLazyload;
        public VisualElement builtinList;
        public VisualElement lazyloadList;
        public Button btnApply;

        private List<string> _modules = new List<string>();

        /// <summary>原 UIClassifyWindow.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement();
            root.style.flexGrow = 1f;

            var pathRow = new VisualElement();
            pathRow.style.flexDirection = FlexDirection.Row;
            pathField = new ObjectField("资源目录") { name = "pathField", allowSceneObjects = true };
            pathField.style.flexGrow = 1f;
            pathField.RegisterValueChangedCallback(_OnPathFieldChanged);
            pathRow.Add(pathField);
            btnReload = new Button(_OnBtnReloadClick) { name = "btnReload", text = "重载配置" };
            btnReload.style.width = 80f;
            pathRow.Add(btnReload);
            root.Add(pathRow);

            var toolRow = new VisualElement();
            toolRow.style.flexDirection = FlexDirection.Row;
            searchField = new TextField { name = "searchField" };
            searchField.style.flexGrow = 1f;
            searchField.RegisterValueChangedCallback(_OnSearchChanged);
            toolRow.Add(searchField);
            btnRescan = new Button(_OnBtnRescanClick) { name = "btnRescan", text = "刷新目录" };
            btnRescan.style.width = 80f;
            toolRow.Add(btnRescan);
            root.Add(toolRow);

            root.Add(BuildSection("内置（builtin，进首包）", out btnAddBuiltin, out builtinList,
                _OnBtnAddBuiltinClick));
            root.Add(BuildSection("懒加载（目录 + 下载标签）", out btnAddLazyload, out lazyloadList,
                _OnBtnAddLazyloadClick));

            btnApply = new Button(_OnBtnApplyClick) { name = "btnApply", text = "生成配置" };
            btnApply.style.height = 50f;
            btnApply.style.marginTop = 10f;
            btnApply.style.backgroundColor = new Color(40f / 255f, 106f / 255f, 42f / 255f);
            root.Add(btnApply);
        }

        private VisualElement BuildSection(string title, out Button addButton, out VisualElement list,
            System.Action onAdd)
        {
            var foldout = new Foldout { text = title, value = true };

            var toolRow = new VisualElement();
            toolRow.style.flexDirection = FlexDirection.Row;
            addButton = new Button(onAdd) { text = "[+]" };
            addButton.style.width = 30f;
            addButton.style.height = 20f;
            toolRow.Add(addButton);
            foldout.Add(toolRow);

            list = new VisualElement();
            foldout.Add(list);

            return foldout;
        }

        public void CreateGUI()
        {
            _ResClassifySettings = null;
            BuildUI();
            rootVisualElement.Add(root);

            var helpBox = new HelpBox(
                "没列出的模块默认 preload（启动时下载）。要进首包的加到内置，要按功能包下载的加到懒加载并填标签。" +
                "搜索框过滤下拉选项。",
                HelpBoxMessageType.Info);
            helpBox.style.fontSize = 26;
            root.Insert(0, helpBox);

            var pathObject = AssetDatabase.LoadAssetAtPath<Object>(ResClassifySettings.path);
            pathField.SetValueWithoutNotify(pathObject);

            RefreshModules();
            LoadRows();
        }

        private void _OnPathFieldChanged(ChangeEvent<Object> e)
        {
            if (e.newValue == null) return;
            ResClassifySettings.path = AssetDatabase.GetAssetPath(e.newValue);
            RefreshModules();
            LoadRows();
        }

        private void _OnBtnReloadClick()
        {
            _ResClassifySettings = null;
            RefreshModules();
            LoadRows();
        }

        private void _OnBtnRescanClick()
        {
            RefreshModules();
        }

        private void _OnSearchChanged(ChangeEvent<string> e)
        {
            RefreshChoices();
        }

        private void _OnBtnAddBuiltinClick()
        {
            builtinList.Add(MakeBuiltinRow(NextUnusedModule()));
        }

        private void _OnBtnAddLazyloadClick()
        {
            lazyloadList.Add(MakeLazyloadRow(NextUnusedModule(), string.Empty));
        }

        /// <summary>新行默认选中第一个还没被占用的模块，避免加出来是个空下拉。</summary>
        private string NextUnusedModule()
        {
            var used = new HashSet<string>();
            CollectUsed(builtinList, used);
            CollectUsed(lazyloadList, used);

            foreach (var module in _modules)
            {
                if (!used.Contains(module)) return module;
            }

            return string.Empty;
        }

        private static void CollectUsed(VisualElement list, HashSet<string> used)
        {
            foreach (var row in list.Children())
            {
                var value = row.Q<DropdownField>("Dir")?.value;
                if (!string.IsNullOrEmpty(value)) used.Add(value);
            }
        }

        private void RefreshModules()
        {
            _modules = ResClassifySettings.ScanModules();
            RefreshChoices();
        }

        /// <summary>按搜索词过滤所有下拉选项；已选中的值即使不匹配也保留，避免选中项凭空消失。</summary>
        private void RefreshChoices()
        {
            var keyword = searchField?.value?.Trim() ?? string.Empty;
            var filtered = new List<string>();
            foreach (var module in _modules)
            {
                if (string.IsNullOrEmpty(keyword) ||
                    module.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filtered.Add(module);
                }
            }

            ApplyChoices(builtinList, filtered);
            ApplyChoices(lazyloadList, filtered);
        }

        private void ApplyChoices(VisualElement list, List<string> filtered)
        {
            foreach (var row in list.Children())
            {
                var dropdown = row.Q<DropdownField>("Dir");
                if (dropdown == null) continue;

                var current = dropdown.value;
                var choices = new List<string>(filtered);
                if (!string.IsNullOrEmpty(current) && !choices.Contains(current)) choices.Insert(0, current);

                dropdown.choices = choices;
                dropdown.SetValueWithoutNotify(current);
            }
        }

        private void LoadRows()
        {
            builtinList.Clear();
            lazyloadList.Clear();

            var modules = ResClassifySettings.modules;
            if (modules == null) return;

            foreach (var module in modules)
            {
                if (module == null || string.IsNullOrEmpty(module.name)) continue;

                // 空 tag 就是默认的 preload，不需要单独一行。
                if (string.IsNullOrEmpty(module.tag)) continue;

                if (module.tag == ResClassifySettings.builtinTag)
                {
                    builtinList.Add(MakeBuiltinRow(module.name));
                }
                else
                {
                    lazyloadList.Add(MakeLazyloadRow(module.name, module.tag));
                }
            }
        }

        private VisualElement MakeBuiltinRow(string moduleName)
        {
            var row = MakeRowBase();
            row.Add(MakeModuleDropdown(moduleName));
            return row;
        }

        private VisualElement MakeLazyloadRow(string moduleName, string tag)
        {
            var row = MakeRowBase();
            row.Add(MakeModuleDropdown(moduleName));

            var tagField = new TextField("资源标签") { name = "Tag" };
            tagField.style.flexGrow = 1f;
            tagField.value = tag;
            row.Add(tagField);

            return row;
        }

        private DropdownField MakeModuleDropdown(string moduleName)
        {
            var dropdown = new DropdownField { name = "Dir" };
            // 固定宽度：flexGrow 的基准宽由内容决定，"Bag" 和 "Multiple" 会算出不同宽度，
            // 后面的标签列就跟着错位。这里锁死目录列，右侧标签列自然对齐。
            dropdown.style.width = DirColumnWidth;
            dropdown.style.flexShrink = 0f;
            dropdown.choices = BuildChoices(moduleName);
            dropdown.SetValueWithoutNotify(moduleName);
            return dropdown;
        }

        private List<string> BuildChoices(string current)
        {
            var keyword = searchField?.value?.Trim() ?? string.Empty;
            var choices = new List<string>();
            foreach (var module in _modules)
            {
                if (string.IsNullOrEmpty(keyword) ||
                    module.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    choices.Add(module);
                }
            }

            if (!string.IsNullOrEmpty(current) && !choices.Contains(current)) choices.Insert(0, current);
            return choices;
        }

        private VisualElement MakeRowBase()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var remove = new Button { name = "Sub", text = "[-]" };
            remove.style.width = 30f;
            remove.style.height = 20f;
            remove.clicked += () => row.RemoveFromHierarchy();
            row.Add(remove);

            return row;
        }

        private void _OnBtnApplyClick()
        {
            var modules = new List<UIModule>();
            var used = new HashSet<string>();

            foreach (var row in builtinList.Children())
            {
                var dir = row.Q<DropdownField>("Dir")?.value?.Trim();
                if (string.IsNullOrEmpty(dir)) continue;
                if (!used.Add(dir))
                {
                    Log.Warning($"资源分类：{dir} 重复配置，已跳过");
                    continue;
                }

                CheckModuleDir(dir);
                modules.Add(new UIModule { name = dir, tag = ResClassifySettings.builtinTag });
            }

            foreach (var row in lazyloadList.Children())
            {
                var dir = row.Q<DropdownField>("Dir")?.value?.Trim();
                var tag = row.Q<TextField>("Tag")?.value?.Trim();
                if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(tag)) continue;

                if (tag == ResClassifySettings.preloadTag || tag == ResClassifySettings.builtinTag)
                {
                    Log.Warning($"资源分类：{dir} 的标签 {tag} 是保留值，请改用内置列表或留空");
                    continue;
                }

                if (!used.Add(dir))
                {
                    Log.Warning($"资源分类：{dir} 重复配置，已跳过");
                    continue;
                }

                CheckModuleDir(dir);
                modules.Add(new UIModule { name = dir, tag = tag });
            }

            ResClassifySettings.modules = modules.ToArray();
            CreateYooAssetUIGroup();
            if (EditorUtility.DisplayDialog("提示", "配置成功!", "ok"))
            {
                _OnBtnReloadClick();
            }
        }

        private void CheckModuleDir(string moduleName)
        {
            var dir = $"{ResClassifySettings.path}/{moduleName}";
            if (!Directory.Exists(dir))
            {
                Log.Warning($"资源分类：模块目录不存在 {dir}");
            }
        }
    }
}
