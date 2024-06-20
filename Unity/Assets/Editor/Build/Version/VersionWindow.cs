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
        [MenuItem("UxGame/构建/构建打包", false, 550)]
        public static void Build()
        {
            var window = GetWindow<VersionWindow>("VersionWindow", true);
            window.minSize = new Vector2(800, 500);
        }

        [MenuItem("UxGame/构建/本地资源服务器", false, 551)]
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
        public void CreateGUI()
        {
            try
            {
                LoadConfig();
                CreateChildren();
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

        partial void _OnTxtNameChanged(ChangeEvent<string> e)
        {
            SelectItem.Name = e.newValue;
            OnExportListData();
        }

        partial void _OnPlatformTypeChanged(ChangeEvent<Enum> e)
        {
            SelectItem.PlatformType = (PlatformType)e.newValue;
        }
        partial void _OnBuildTypeChanged(ChangeEvent<Enum> e)
        {
            RefreshView();
        }
        partial void _OnInputBundlePathChanged(ChangeEvent<string> e)
        {
            SelectItem.BundlePath = e.newValue;
        }

        partial void _OnTgCopyChanged(ChangeEvent<bool> e)
        {
            SelectItem.IsCopyTo = e.newValue;
            RefreshElement();
        }

        partial void _OnInputCopyPathChanged(ChangeEvent<string> e)
        {
            SelectItem.CopyPath = e.newValue;
        }

        partial void _OnTgClearSandBoxChanged(ChangeEvent<bool> e)
        {
            SelectItem.IsClearSandBox = e.newValue;
        }

        partial void _OnTgCompileDLLChanged(ChangeEvent<bool> e)
        {
            RefreshElement();
        }

        partial void _OnTgExeChanged(ChangeEvent<bool> e)
        {
            SelectItem.IsExportExecutable = e.newValue;
            RefreshElement();
        }


        partial void _OnInputExePathChanged(ChangeEvent<string> e)
        {
            SelectItem.ExePath = e.newValue;
        }

        partial void _OnCompileTypeChanged(ChangeEvent<Enum> e)
        {
            SelectItem.CompileType = (CompileType)e.newValue;
        }
    

        partial void _OnMakeListExportItem(VisualElement e)
        {
            var label = new Label();
            label.name = "Label1";
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.flexGrow = 1f;
            label.style.height = 20f;
            e.Add(label);
        }
        partial void _OnBindListExportItem(VisualElement e, int index)
        {
            var setting = Setting.ExportSettings[index];
            var textField1 = e.Q<Label>("Label1");
            textField1.text = setting.Name;
        }

        partial void _OnListExportItemClick(IEnumerable<object> objs)
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
        partial void _OnBtnAddClick()
        {
            var dt = DateTime.Now;
            int totalSecond = dt.Hour * 3600 + dt.Minute * 60 + dt.Second;
            var item = new BuildExportSetting();
            item.Name = $"Build-{dt.ToString("yyyy-MM-dd")}-{totalSecond}";
            Setting.ExportSettings.Add(item);
            OnExportListData();
        }
        partial void _OnBtnRemoveClick()
        {
            var item = SelectItem;
            if (item == null)
            {
                return;
            }
            Setting.ExportSettings.Remove(item);
            OnExportListData();
        }

        partial void _OnBtnExePathClick()
        {
            BuildHelper.OpenFolderPanel(SelectItem.ExePath, "请选择生成路径", inputExePath);
        }
        partial void _OnBtnBundlePathClick()
        {
            BuildHelper.OpenFolderPanel(SelectItem.BundlePath, "请选择生成路径", inputBundlePath);
        }
        partial void _OnBtnCopyPathClick()
        {
            BuildHelper.OpenFolderPanel(SelectItem.CopyPath, "请选择CDN路径", inputCopyPath);
        }

        partial void _OnBuildClick()
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
                    Build();
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
            Setting = SettingTools.GetSingletonAssets<VersionSettingData>("Assets/Setting/Build/Version");
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

