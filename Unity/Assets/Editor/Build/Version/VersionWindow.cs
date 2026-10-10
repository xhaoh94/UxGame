using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using YooAsset.Editor;
namespace Ux.Editor.Build.Version
{
    public enum BuildType
    {
        ForceRebuild,
        IncrementalBuild,
    }
    public enum CompileType
    {
        Development,
        Release,
    }
    public enum PlatformType
    {
        Win32,
        Win64,
        Android,
        IOS,
        //MacOS,
    }
    public partial class VersionWindow : EditorWindow
    {
        [MenuItem("UxGame/工具/构建打包", false, 550)]
        public static void Build()
        {
            var window = GetWindow<VersionWindow>("VersionWindow", true);
            window.minSize = new Vector2(800, 500);
        }

        [MenuItem("UxGame/工具/本地资源服务器", false, 551)]
        public static void OpenFileServer()
        {
            //Command.Run("../HFS/hfs.exe");
            var path = Path.GetFullPath("../Unity/CDN/").Replace("\\", "/");
            if (!Directory.Exists(path))
            {
                Log.Error("本地资源服务器，路径不存在");
                return;
            }
            Command.Run(Path.GetFullPath("../HFS/dufs.exe").Replace("\\", "/"),
                $"-p 0709 {path}", false);
        }


        private List<string> _buildPackageNames;
        private VersionSettingData Setting;
        private int _lastModifyExportIndex = 0;

        VersionPackageViewer _versionPackage;
        ToolbarMenu _packageMenu;

        BuildExportSetting SelectItem
        {
            get
            {
                var selectItem = listExport.selectedItem as BuildExportSetting;
                return selectItem;
            }
        }
        protected VisualElement root;
        public ListView listExport;
        public Button btnRemove;
        public Button btnAdd;
        public VisualElement exportElement;
        public TextField txtName;
        public EnumField platformType;
        public TextField txtVersion;
        public EnumField buildType;
        public TextField inputBundlePath;
        public Button btnBundlePath;
        public Toggle tgUseDb;
        public Toggle tgCopy;
        public TextField inputCopyPath;
        public Button btnCopyPath;
        public Toggle tgClearSandBox;
        public Toggle tgCompileDLL;
        public Toggle tgCompileAot;
        public Toggle tgCompileUI;
        public Toggle tgCompileConfig;
        public Toggle tgCompileProto;
        public MaskField buildPackage;
        public VisualElement encryptionContainer;
        public Toggle tgExe;
        public VisualElement exeElement;
        public TextField inputExePath;
        public Button btnExePath;
        public EnumField compileType;
        public Button build;
        public Button clear;
        public Toolbar Toolbar;
        public VisualElement Container;

        /// <summary>原 VersionWindow.uxml 的手搭等价版本。
        /// GenCode 里注册、主类却没实现的回调（Aot/UI/Config/Proto 的 Toggle 与 clear 按钮）不能注册 —— 原本就被编译器擦除。</summary>
        private void BuildUI()
        {
            var sectionBackground = new Color(89f / 255f, 89f / 255f, 89f / 255f);

            void Border5(VisualElement element)
            {
                element.style.borderLeftWidth = 5f;
                element.style.borderRightWidth = 5f;
                element.style.borderTopWidth = 5f;
                element.style.borderBottomWidth = 5f;
            }

            Label SectionLabel(string text)
            {
                var label = new Label(text);
                label.style.backgroundColor = sectionBackground;
                label.style.unityTextAlign = TextAnchor.UpperCenter;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.fontSize = 12f;
                Border5(label);
                return label;
            }

            (VisualElement row, TextField field, Button button) PathRow(string fieldName, string label, string buttonName, string value, Action onPick)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.flexGrow = 0f;
                var field = new TextField(label) { name = fieldName, value = value, pickingMode = PickingMode.Ignore };
                field.style.flexGrow = 1f;
                row.Add(field);
                var button = new Button(onPick) { name = buttonName, text = "选择" };
                button.style.flexGrow = 0f;
                row.Add(button);
                return (row, field, button);
            }

            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1f;

            var leftColumn = new VisualElement();
            leftColumn.style.width = 200f;
            leftColumn.style.flexGrow = 0f;
            leftColumn.style.backgroundColor = new Color(67f / 255f, 67f / 255f, 67f / 255f);
            Border5(leftColumn);
            root.Add(leftColumn);

            leftColumn.Add(SectionLabel("构建列表"));

            listExport = new ListView { name = "listExport" };
            listExport.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listExport.fixedItemHeight = 20f;
            listExport.style.flexGrow = 1f;
            listExport.makeItem = () => { var item = new VisualElement(); _OnMakeListExportItem(item); return item; };
            listExport.bindItem = (item, index) => _OnBindListExportItem(item, index);
            listExport.selectionChanged += _OnListExportItemClick;
            leftColumn.Add(listExport);

            var listButtons = new VisualElement();
            listButtons.style.height = 20f;
            listButtons.style.flexDirection = FlexDirection.Row;
            listButtons.style.justifyContent = Justify.Center;
            btnRemove = new Button(_OnBtnRemoveClick) { name = "btnRemove", text = " - " };
            listButtons.Add(btnRemove);
            btnAdd = new Button(_OnBtnAddClick) { name = "btnAdd", text = " + " };
            listButtons.Add(btnAdd);
            leftColumn.Add(listButtons);

            exportElement = new VisualElement { name = "exportElement" };
            exportElement.style.flexGrow = 1f;
            exportElement.style.flexDirection = FlexDirection.Row;
            root.Add(exportElement);

            var mainColumn = new VisualElement();
            mainColumn.style.flexGrow = 1f;
            exportElement.Add(mainColumn);

            mainColumn.Add(SectionLabel("打包"));

            txtName = new TextField("配置名") { name = "txtName", pickingMode = PickingMode.Ignore };
            txtName.RegisterValueChangedCallback(_OnTxtNameChanged);
            mainColumn.Add(txtName);

            platformType = new EnumField { name = "platformType", label = "编译平台" };
            platformType.style.flexShrink = 1f;
            platformType.style.flexGrow = 0f;
            platformType.RegisterValueChangedCallback(_OnPlatformTypeChanged);
            mainColumn.Add(platformType);

            txtVersion = new TextField("构建版本") { name = "txtVersion", isReadOnly = true };
            txtVersion.style.flexShrink = 1f;
            mainColumn.Add(txtVersion);

            buildType = new EnumField { name = "buildType", label = "构建类型" };
            buildType.style.flexGrow = 0f;
            buildType.style.flexShrink = 1f;
            buildType.RegisterValueChangedCallback(_OnBuildTypeChanged);
            mainColumn.Add(buildType);

            var bundleRow = PathRow("inputBundlePath", "资源文件构建目录", "btnBundlePath", "./Bundles", _OnBtnBundlePathClick);
            inputBundlePath = bundleRow.field;
            btnBundlePath = bundleRow.button;
            inputBundlePath.RegisterValueChangedCallback(_OnInputBundlePathChanged);
            mainColumn.Add(bundleRow.row);

            tgUseDb = new Toggle("使用资源依赖关系数据库（可以提高打包速度！）") { name = "tgUseDb", value = true };
            tgUseDb.style.height = StyleKeyword.Auto;
            tgUseDb.RegisterValueChangedCallback(_OnTgUseDbChanged);
            mainColumn.Add(tgUseDb);

            tgCopy = new Toggle("是否拷贝") { name = "tgCopy", value = false };
            tgCopy.style.height = StyleKeyword.Auto;
            tgCopy.RegisterValueChangedCallback(_OnTgCopyChanged);
            mainColumn.Add(tgCopy);

            var copyRow = PathRow("inputCopyPath", "拷贝到目录", "btnCopyPath", "./Release", _OnBtnCopyPathClick);
            inputCopyPath = copyRow.field;
            btnCopyPath = copyRow.button;
            inputCopyPath.RegisterValueChangedCallback(_OnInputCopyPathChanged);
            mainColumn.Add(copyRow.row);

            tgClearSandBox = new Toggle("清理沙盒缓存") { name = "tgClearSandBox" };
            tgClearSandBox.style.height = StyleKeyword.Auto;
            tgClearSandBox.RegisterValueChangedCallback(_OnTgClearSandBoxChanged);
            mainColumn.Add(tgClearSandBox);

            tgCompileDLL = new Toggle("编译热更DLL") { name = "tgCompileDLL" };
            tgCompileDLL.style.height = StyleKeyword.Auto;
            tgCompileDLL.RegisterValueChangedCallback(_OnTgCompileDLLChanged);
            mainColumn.Add(tgCompileDLL);

            tgCompileAot = new Toggle("生成AOT") { name = "tgCompileAot" };
            mainColumn.Add(tgCompileAot);

            tgCompileUI = new Toggle("UI代码") { name = "tgCompileUI" };
            mainColumn.Add(tgCompileUI);

            tgCompileConfig = new Toggle("配置文件") { name = "tgCompileConfig" };
            mainColumn.Add(tgCompileConfig);

            tgCompileProto = new Toggle("协议文件") { name = "tgCompileProto" };
            mainColumn.Add(tgCompileProto);

            buildPackage = new MaskField("构建资源包") { name = "buildPackage" };
            buildPackage.style.flexGrow = 0f;
            buildPackage.style.flexShrink = 1f;
            mainColumn.Add(buildPackage);

            encryptionContainer = new VisualElement { name = "encryptionContainer" };
            encryptionContainer.style.height = StyleKeyword.Auto;
            mainColumn.Add(encryptionContainer);

            tgExe = new Toggle("是否生成可执行文件") { name = "tgExe" };
            tgExe.RegisterValueChangedCallback(_OnTgExeChanged);
            mainColumn.Add(tgExe);

            exeElement = new VisualElement { name = "exeElement" };
            exeElement.style.flexGrow = 1f;
            mainColumn.Add(exeElement);

            var exeRow = PathRow("inputExePath", "可执行文件构建目录", "btnExePath", "./Release", _OnBtnExePathClick);
            inputExePath = exeRow.field;
            btnExePath = exeRow.button;
            inputExePath.RegisterValueChangedCallback(_OnInputExePathChanged);
            exeElement.Add(exeRow.row);

            compileType = new EnumField { name = "compileType", label = "编译类型" };
            compileType.style.flexGrow = 0f;
            compileType.style.flexShrink = 1f;
            compileType.RegisterValueChangedCallback(_OnCompileTypeChanged);
            exeElement.Add(compileType);

            build = new Button(_OnBuildClick) { name = "build", text = "开始打包" };
            build.style.height = 50f;
            build.style.marginTop = 10f;
            build.style.backgroundColor = new Color(40f / 255f, 106f / 255f, 42f / 255f);
            mainColumn.Add(build);

            clear = new Button(_OnClearClick) { name = "clear", text = "重置打包" };
            clear.style.height = 50f;
            clear.style.marginTop = 10f;
            clear.style.backgroundColor = new Color(137f / 255f, 40f / 255f, 32f / 255f);
            mainColumn.Add(clear);

            var rightColumn = new VisualElement();
            rightColumn.style.flexGrow = 0f;
            rightColumn.style.width = 300f;
            rightColumn.style.flexShrink = 1f;
            exportElement.Add(rightColumn);

            rightColumn.Add(SectionLabel("资源包设置"));

            Toolbar = new Toolbar { name = "Toolbar" };
            Toolbar.style.flexDirection = FlexDirection.Row;
            rightColumn.Add(Toolbar);

            Container = new VisualElement { name = "Container" };
            rightColumn.Add(Container);
        }

        public void CreateGUI()
        {
            try
            {
                LoadConfig();
                BuildUI();
                rootVisualElement.Add(root);

                // 检测构建包裹
                _buildPackageNames = GetBuildPackageNames();

                platformType.Init(PlatformType.Win64);
                // 构建包裹            
                buildPackage.choices = _buildPackageNames;
                buildPackage.value = -1;

                //构建版本            
                txtVersion.isReadOnly = true;
                txtVersion.SetEnabled(false);

                //编译类型            
                buildType.Init(BuildType.IncrementalBuild);   

                // 是否编译热更DLL            
                tgCompileDLL.SetValueWithoutNotify(true);                
                tgCompileAot.SetValueWithoutNotify(true);
                tgCompileUI.SetValueWithoutNotify(true);
                tgCompileConfig.SetValueWithoutNotify(true);
                tgCompileProto.SetValueWithoutNotify(true);

                //编译类型            
                compileType.Init(CompileType.Development);


                if (_buildPackageNames.Count == 0)
                {
                    var label = new Label();
                    label.text = "没有发现可构建的资源包";
                    label.style.width = 100;
                    Toolbar.Add(label);
                    return;
                }

                _packageMenu = new ToolbarMenu();
                //_packageMenu.style.width = 200;
                foreach (var packageName in _buildPackageNames)
                {
                    _packageMenu.menu.AppendAction(packageName, PackageMenuAction, PackageMenuFun, packageName);
                }
                Toolbar.Add(_packageMenu);

                _versionPackage = new VersionPackageViewer(Container);
                OnExportListData();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void _OnTxtNameChanged(ChangeEvent<string> e)
        {
            SelectItem.Name = e.newValue;
            OnExportListData();
        }

        private void _OnPlatformTypeChanged(ChangeEvent<Enum> e)
        {
            SelectItem.PlatformType = (PlatformType)e.newValue;
        }
        private void _OnBuildTypeChanged(ChangeEvent<Enum> e)
        {
            RefreshView();
        }
        private void _OnInputBundlePathChanged(ChangeEvent<string> e)
        {
            SelectItem.BundlePath = e.newValue;
        }

        private void _OnTgUseDbChanged(ChangeEvent<bool> e)
        {
            SelectItem.IsUseDb = e.newValue;
        }
        private void _OnTgCopyChanged(ChangeEvent<bool> e)
        {
            SelectItem.IsCopyTo = e.newValue;
            RefreshElement();
        }

        private void _OnInputCopyPathChanged(ChangeEvent<string> e)
        {
            SelectItem.CopyPath = e.newValue;
        }

        private void _OnTgClearSandBoxChanged(ChangeEvent<bool> e)
        {
            SelectItem.IsClearSandBox = e.newValue;
        }

        private void _OnTgCompileDLLChanged(ChangeEvent<bool> e)
        {
            RefreshElement();
        }

        private void _OnTgExeChanged(ChangeEvent<bool> e)
        {
            SelectItem.IsExportExecutable = e.newValue;
            RefreshElement();
        }


        private void _OnInputExePathChanged(ChangeEvent<string> e)
        {
            SelectItem.ExePath = e.newValue;
        }

        private void _OnCompileTypeChanged(ChangeEvent<Enum> e)
        {
            SelectItem.CompileType = (CompileType)e.newValue;
        }
    

        private void _OnMakeListExportItem(VisualElement e)
        {
            var label = new Label();
            label.name = "Label1";
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.flexGrow = 1f;
            label.style.height = 20f;
            e.Add(label);
        }
        private void _OnBindListExportItem(VisualElement e, int index)
        {
            var setting = Setting.ExportSettings[index];
            var textField1 = e.Q<Label>("Label1");
            textField1.text = setting.Name;
        }

        private void _OnListExportItemClick(IEnumerable<object> objs)
        {
            if (listExport.selectedIndex < 0)
            {
                return;
            }
            _lastModifyExportIndex = listExport.selectedIndex;
            RefreshView();
        }
        private void OnExportListData()
        {            
            listExport.Clear();
            listExport.ClearSelection();
            listExport.itemsSource = Setting.ExportSettings;
            listExport.Rebuild();
            if (Setting.ExportSettings.Count > 0)
            {
                if (_lastModifyExportIndex >= 0)
                {
                    if (_lastModifyExportIndex >= listExport.itemsSource.Count)
                    {
                        _lastModifyExportIndex = 0;
                    }
                    listExport.selectedIndex = _lastModifyExportIndex;
                }
            }
            else
            {
                RefreshView();
            }
        }
        string AddVersion(string version)
        {
            if (IsForceRebuild || string.IsNullOrEmpty(version))
            {
                var dt = DateTime.Now;
                int totalSecond = dt.Hour * 3600 + dt.Minute * 60 + dt.Second;
                return $"{dt.ToString("yyMMdd")}{totalSecond}x1";
            }

            try
            {
                var sz = version.Split('x');
                return $"{sz[0]}x{int.Parse(sz[1]) + 1}";
            }
            catch
            {
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                byte[] bs = asciiEncoding.GetBytes(version);
                bs[bs.Length - 1]++;
                return asciiEncoding.GetString(bs);
            }
        }
        private void PackageMenuAction(DropdownMenuAction action)
        {
            var packageName = (string)action.userData;
            if (_packageMenu.text != packageName)
            {
                RefreshPackageView(packageName);
            }
        }
        private DropdownMenuAction.Status PackageMenuFun(DropdownMenuAction action)
        {
            var packageName = (string)action.userData;
            if (_packageMenu.text == packageName)
                return DropdownMenuAction.Status.Checked;
            else
                return DropdownMenuAction.Status.Normal;
        }

        private void RefreshPackageView(string packageName)
        {
            if (SelectItem == null)
            {
                return;
            }
            _packageMenu.text = packageName;
            _versionPackage.RefreshView(SelectItem.GetPackageSetting(packageName));
            _versionPackage.RefreshElement(IsForceRebuild);
        }
        void RefreshView()
        {
            if (SelectItem == null)
            {
                exportElement.style.display = DisplayStyle.None;
                return;
            }
            exportElement.style.display = DisplayStyle.Flex;
            txtName.SetValueWithoutNotify(SelectItem.Name);
            platformType.SetValueWithoutNotify(SelectItem.PlatformType);
            inputExePath.SetValueWithoutNotify(SelectItem.ExePath);
            compileType.SetValueWithoutNotify(SelectItem.CompileType);
            inputBundlePath.SetValueWithoutNotify(SelectItem.BundlePath);
            tgUseDb.SetValueWithoutNotify(SelectItem.IsUseDb);
            tgCopy.SetValueWithoutNotify(SelectItem.IsCopyTo);
            inputCopyPath.SetValueWithoutNotify(SelectItem.CopyPath);
            tgExe.SetValueWithoutNotify(SelectItem.IsExportExecutable);
            txtVersion.SetValueWithoutNotify(AddVersion(SelectItem.ResVersion));

            tgClearSandBox.SetValueWithoutNotify(SelectItem.IsClearSandBox);
            RefreshPackageView(_buildPackageNames[0]);
            RefreshElement();
        }

        bool IsForceRebuild => (BuildType)buildType.value == BuildType.ForceRebuild;
        bool IsExportExecutable => tgExe.value && IsForceRebuild;
        void RefreshElement()
        {
            tgExe.style.display = IsForceRebuild ? DisplayStyle.Flex : DisplayStyle.None;
            exeElement.style.display = IsExportExecutable ? DisplayStyle.Flex : DisplayStyle.None;
            buildPackage.style.display = IsExportExecutable ? DisplayStyle.None : DisplayStyle.Flex;
            tgClearSandBox.style.display = IsForceRebuild ? DisplayStyle.Flex : DisplayStyle.None;

            inputCopyPath.parent.style.display = tgCopy.value ? DisplayStyle.Flex : DisplayStyle.None;
            tgCompileAot.style.display = tgCompileDLL.value && IsExportExecutable ? DisplayStyle.Flex : DisplayStyle.None;
            tgCompileUI.style.display = tgCompileDLL.value ? DisplayStyle.Flex : DisplayStyle.None;
            tgCompileConfig.style.display = tgCompileDLL.value ? DisplayStyle.Flex : DisplayStyle.None;
            tgCompileProto.style.display = tgCompileDLL.value ? DisplayStyle.Flex : DisplayStyle.None;
            _versionPackage.RefreshElement(IsForceRebuild);
        }
        private void _OnBtnAddClick()
        {
            var dt = DateTime.Now;
            int totalSecond = dt.Hour * 3600 + dt.Minute * 60 + dt.Second;
            var item = new BuildExportSetting();
            item.Name = $"Build-{dt.ToString("yyyy-MM-dd")}-{totalSecond}";
            Setting.ExportSettings.Add(item);
            OnExportListData();
        }
        private void _OnBtnRemoveClick()
        {
            var item = SelectItem;
            if (item == null)
            {
                return;
            }
            Setting.ExportSettings.Remove(item);
            OnExportListData();
        }

        private void _OnBtnExePathClick()
        {
            BuildHelper.OpenFolderPanel(SelectItem.ExePath, "请选择生成路径", inputExePath);
        }
        private void _OnBtnBundlePathClick()
        {
            BuildHelper.OpenFolderPanel(SelectItem.BundlePath, "请选择生成路径", inputBundlePath);
        }
        private void _OnBtnCopyPathClick()
        {
            BuildHelper.OpenFolderPanel(SelectItem.CopyPath, "请选择CDN路径", inputCopyPath);
        }

        private void _OnBuildClick()
        {
            var bType = (BuildType)buildType.value;
            var resVersion = txtVersion.value.Trim();
            var buildResVerion = SelectItem.ResVersion.Trim();
            if (string.Compare(resVersion, buildResVerion, true) <= 0)
            {
                if (EditorUtility.DisplayDialog("提示", $"资源版本不可小于当前版本", "确定", "取消"))
                {
                    txtVersion.SetValueWithoutNotify(AddVersion(buildResVerion));
                }
                return;
            }
            void Build()
            {
                string content = string.Empty;
                switch (bType)
                {
                    case BuildType.ForceRebuild:
                        content = "是否构建【整包】！";
                        break;
                    case BuildType.IncrementalBuild:
                        content = "是否构建【补丁包】！";
                        break;
                }
                if (EditorUtility.DisplayDialog("提示", content, "确定", "取消"))
                {
                    EditorTools.ClearUnityConsole();
                    EditorApplication.delayCall += ExecuteBuild;
                }
                else
                {
                    Log.Debug("打包已经取消");
                }
            }

            // 检测是否有未保存场景
            if (EditorTools.HasDirtyScenes())
            {
                if (EditorUtility.DisplayDialog("提示", $"检测到未保存的场景文件,是否切到Boot场景继续打包？", "确定", "取消"))
                {
                    UxEditor.ChangeBoot();                    
                }
                return;
            }
            Build();
        }

        public static BuildTarget GetBuildTarget(PlatformType platformType)
        {
            BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
            switch (platformType)
            {
                case PlatformType.Win32:
                    buildTarget = BuildTarget.StandaloneWindows;
                    break;
                case PlatformType.Android:
                    buildTarget = BuildTarget.Android;
                    break;
                case PlatformType.IOS:
                    buildTarget = BuildTarget.iOS;
                    break;
            }
            return buildTarget;
        }
        private async void ExecuteBuild()
        {
            var platformType = SelectItem.PlatformType;
            BuildTarget buildTarget = GetBuildTarget(platformType);

            EditorTools.FocusUnityConsoleWindow();
            Console.Clear();
            if (EditorApplication.isCompiling)
            {
                Log.Error("请等待编译完成后再导出");
                return;
            }

            EditorApplication.LockReloadAssemblies();
            try
            {
                var succ = await _ExecuteBuild(buildTarget);
                if (succ)
                {
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            EditorApplication.UnlockReloadAssemblies();
        }
        async UniTask<bool> _ExecuteBuild(BuildTarget buildTarget)
        {
            if (!await BuildDLL(buildTarget))
            {
                return false;
            }
            if (!await BuildRes(buildTarget))
            {
                return false;
            }
            if (!BuildExe(buildTarget))
            {
                return false;
            }
            return true;
        }

        #region 初始化   
        void LoadConfig()
        {
            Setting = SettingTools.GetSingletonAssets<VersionSettingData>("Assets/Settings/Build/Version");
        }
        void SaveConfig()
        {
            var item = SelectItem;
            if (item != null)
            {
                item.ResVersion = txtVersion.value;
            }
            Setting?.SaveFile();
            EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path, OpenSceneMode.Single);
        }

        #endregion



    }

}

