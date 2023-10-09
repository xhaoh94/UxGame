using Cysharp.Threading.Tasks;
using HybridCLR.Commands;
using HybridCLR.Editor.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using YooAsset;
using YooAsset.Editor;


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
public class VersionWindow : EditorWindow
{
    [MenuItem("UxGame/构建/构建打包", false, 500)]
    public static void Build()
    {
        var window = GetWindow<VersionWindow>("VersionWindow", true);
        window.minSize = new Vector2(800, 500);
    }

    [MenuItem("UxGame/构建/本地资源服务器", false, 501)]
    public static void OpenFileServer()
    {
        //Command.Run("../HFS/hfs.exe");
        Command.Run(Path.GetFullPath("../HFS/dufs.exe").Replace("\\", "/"),
            $"-p 0709 {Path.GetFullPath("../Unity/CDN/").Replace("\\", "/")}", false);
    }




    private List<string> _buildPackageNames;
    private VersionSettingData Setting;
    private int _lastModifyExportIndex = 0;

    ListView _listExport;
    Button _btnAdd;
    Button _btnRemove;

    VisualElement _exportElement;
    TextField _txtName;
    EnumField _platformType;
    EnumField _buildType;
    Toggle _tgExe;

    #region 可执行文件
    VisualElement _exeElement;
    TextField _inputExePath;
    Button _btnExePath;
    EnumField _compileType;
    #endregion

    #region 资源构建    
    Toolbar _toolbar;
    VisualElement _container;
    ToolbarMenu _packageMenu;

    TextField _inputBundlePath;
    Button _btnBundlePath;
    Toggle _tgCopy;
    TextField _inputCopyPath;
    Button _btnCopyPath;
    TextField _txtVersion;
    Toggle _tgCompileDLL;
    Toggle _tgClearSandBox;

    #endregion

    MaskField _buildPackage;

    VersionPackageViewer _versionPackage;

    public void CreateGUI()
    {
        try
        {
            LoadConfig();
            VisualElement root = rootVisualElement;

            var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/Version/VersionWindow.uxml");
            visualAsset.CloneTree(root);


            // 检测构建包裹
            _buildPackageNames = GetBuildPackageNames();

            _listExport = root.Q<ListView>("listExport");
            _listExport.makeItem = MakeExportListViewItem;
            _listExport.bindItem = BindExportListViewItem;
#if UNITY_2022_1_OR_NEWER
            _listExport.selectionChanged += OnExportListSelectionChange;
#else
            _listExport.onSelectionChange += OnExportListSelectionChange;
#endif

            _btnAdd = root.Q<Button>("btnAdd");
            _btnAdd.clicked += OnBtnAddClick;
            _btnRemove = root.Q<Button>("btnRemove");
            _btnRemove.clicked += OnBtnRemoveClick;


            _exportElement = root.Q<VisualElement>("exportElement");
            _txtName = _exportElement.Q<TextField>("txtName");
            _txtName.RegisterValueChangedCallback(evt =>
            {
                SelectItem.Name = evt.newValue;
                OnExportListData();
            });

            _platformType = _exportElement.Q<EnumField>("platformType");
            _platformType.Init(PlatformType.Win64);
            _platformType.style.width = 500;
            _platformType.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformType = (PlatformType)_platformType.value;
            });
            // 构建包裹
            _buildPackage = _exportElement.Q<MaskField>("buildPackage");
            _buildPackage.choices = _buildPackageNames;
            _buildPackage.value = -1;
            _buildPackage.style.width = 350;

            //构建版本
            _txtVersion = _exportElement.Q<TextField>("txtVersion");
            _txtVersion.isReadOnly = true;
            _txtVersion.SetEnabled(false);

            //编译类型
            _buildType = _exportElement.Q<EnumField>("buildType");
            _buildType.Init(BuildType.IncrementalBuild);
            _buildType.style.width = 500;
            _buildType.RegisterValueChangedCallback(evt =>
            {
                //var resVersion = _txtVersion.value;
                RefreshView();
                //_txtVersion.SetValueWithoutNotify(resVersion);
            });

            //资源导出路径
            _inputBundlePath = _exportElement.Q<TextField>("inputBundlePath");
            _inputBundlePath.RegisterValueChangedCallback(evt =>
            {
                SelectItem.BundlePath = evt.newValue;
            });
            _btnBundlePath = _exportElement.Q<Button>("btnBundlePath");
            _btnBundlePath.clicked += OnBtnBundlePathClick;

            //是否拷贝
            _tgCopy = _exportElement.Q<Toggle>("tgCopy");
            _tgCopy.RegisterValueChangedCallback(evt =>
            {
                SelectItem.IsCopyTo = evt.newValue;
                RefreshElement();
            });
            //拷贝路径
            _inputCopyPath = _exportElement.Q<TextField>("inputCopyPath");
            _inputCopyPath.RegisterValueChangedCallback(evt =>
            {
                SelectItem.CopyPath = evt.newValue;
            });
            _btnCopyPath = _exportElement.Q<Button>("btnCopyPath");
            _btnCopyPath.clicked += OnBtnCopyPathClick;

            // 是否清理沙盒缓存
            _tgClearSandBox = _exportElement.Q<Toggle>("tgClearSandBox");
            _tgClearSandBox.RegisterValueChangedCallback(evt =>
            {
                SelectItem.IsClearSandBox = evt.newValue;
            });

            // 是否编译热更DLL
            _tgCompileDLL = _exportElement.Q<Toggle>("tgCompileDLL");
            _tgCompileDLL.RegisterValueChangedCallback(evt =>
            {
                SelectItem.IsCompileDLL = evt.newValue;
            });

            _tgExe = _exportElement.Q<Toggle>("tgExe");
            _tgExe.RegisterValueChangedCallback(evt =>
            {
                SelectItem.IsExportExecutable = evt.newValue;
                RefreshElement();
            });
            _exeElement = _exportElement.Q<VisualElement>("exeElement");
            //导出路径
            _inputExePath = _exeElement.Q<TextField>("inputExePath");
            _inputExePath.RegisterValueChangedCallback(evt =>
            {
                SelectItem.ExePath = evt.newValue;
            });
            _btnExePath = _exeElement.Q<Button>("btnExePath");
            _btnExePath.clicked += OnBtnExePathClick;

            //编译类型
            _compileType = _exeElement.Q<EnumField>("compileType");
            _compileType.Init(CompileType.Development);
            _compileType.style.width = 500;
            _compileType.RegisterValueChangedCallback(evt =>
            {
                SelectItem.CompileType = (CompileType)evt.newValue;
            });


            // 构建选中按钮
            var btnBuild = _exportElement.Q<Button>("build");
            btnBuild.clicked += OnBuildSelectClick;

            // 清理按钮
            var btnClear = _exportElement.Q<Button>("clear");
            btnClear.clicked += OnClearClick;

            _toolbar = root.Q<Toolbar>("Toolbar");

            if (_buildPackageNames.Count == 0)
            {
                var label = new Label();
                label.text = "没有发现可构建的资源包";
                label.style.width = 100;
                _toolbar.Add(label);
                return;
            }

            _packageMenu = new ToolbarMenu();
            _packageMenu.style.width = 200;
            foreach (var packageName in _buildPackageNames)
            {
                _packageMenu.menu.AppendAction(packageName, PackageMenuAction, PackageMenuFun, packageName);
            }
            _toolbar.Add(_packageMenu);

            _container = root.Q("Container");
            _versionPackage = new VersionPackageViewer(_container);
            OnExportListData();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }

    BuildExportSetting SelectItem
    {
        get
        {
            var selectItem = _listExport.selectedItem as BuildExportSetting;
            return selectItem;
        }
    }

    private VisualElement MakeExportListViewItem()
    {
        VisualElement element = new VisualElement();

        {
            var label = new Label();
            label.name = "Label1";
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.flexGrow = 1f;
            label.style.height = 20f;
            element.Add(label);
        }

        return element;
    }
    private void BindExportListViewItem(VisualElement element, int index)
    {
        var setting = Setting.ExportSettings[index];

        var textField1 = element.Q<Label>("Label1");
        textField1.text = setting.Name;
    }
    private void OnExportListSelectionChange(IEnumerable<object> objs)
    {
        if (_listExport.selectedIndex < 0)
        {
            return;
        }
        _lastModifyExportIndex = _listExport.selectedIndex;
        RefreshView();
    }
    private void OnExportListData()
    {
        _listExport.Clear();
        _listExport.ClearSelection();
        _listExport.itemsSource = Setting.ExportSettings;
        _listExport.Rebuild();
        if (Setting.ExportSettings.Count > 0)
        {
            if (_lastModifyExportIndex >= 0)
            {
                if (_lastModifyExportIndex >= _listExport.itemsSource.Count)
                {
                    _lastModifyExportIndex = 0;
                }
                _listExport.selectedIndex = _lastModifyExportIndex;
            }
        }
        else
        {
            RefreshView();
        }
    }
    string AddVersion(string version)
    {
        if (IsExportExecutable || string.IsNullOrEmpty(version))
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
    }
    void RefreshView()
    {
        if (SelectItem == null)
        {
            _exportElement.style.display = DisplayStyle.None;
            return;
        }
        _exportElement.style.display = DisplayStyle.Flex;        
        _txtName.SetValueWithoutNotify(SelectItem.Name);
        _platformType.SetValueWithoutNotify(SelectItem.PlatformType);
        _inputExePath.SetValueWithoutNotify(SelectItem.ExePath);
        _compileType.SetValueWithoutNotify(SelectItem.CompileType);
        _inputBundlePath.SetValueWithoutNotify(SelectItem.BundlePath);
        _tgCopy.SetValueWithoutNotify(SelectItem.IsCopyTo);
        _inputCopyPath.SetValueWithoutNotify(SelectItem.CopyPath);
        _tgExe.SetValueWithoutNotify(SelectItem.IsExportExecutable);
        _txtVersion.SetValueWithoutNotify(AddVersion(SelectItem.ResVersion));

        _tgCompileDLL.SetValueWithoutNotify(SelectItem.IsCompileDLL);

        _tgClearSandBox.SetValueWithoutNotify(SelectItem.IsClearSandBox);
        RefreshPackageView(_buildPackageNames[0]);
        RefreshElement();
    }

    bool IsForceRebuild => (BuildType)_buildType.value == BuildType.ForceRebuild;
    bool IsExportExecutable => _tgExe.value && IsForceRebuild;
    void RefreshElement()
    {
        _tgExe.style.display = IsForceRebuild ? DisplayStyle.Flex : DisplayStyle.None;
        _exeElement.style.display = IsExportExecutable ? DisplayStyle.Flex : DisplayStyle.None;
        _buildPackage.style.display = IsExportExecutable ? DisplayStyle.None : DisplayStyle.Flex;
        _tgClearSandBox.style.display = IsForceRebuild ? DisplayStyle.Flex : DisplayStyle.None;

        _inputCopyPath.parent.style.display = _tgCopy.value ? DisplayStyle.Flex : DisplayStyle.None;

        _versionPackage.RefreshElement(IsForceRebuild);
    }
    void OnBtnAddClick()
    {
        var dt = DateTime.Now;
        int totalSecond = dt.Hour * 3600 + dt.Minute * 60 + dt.Second;
        var item = new BuildExportSetting();
        item.Name = $"Build-{dt.ToString("yyyy-MM-dd")}-{totalSecond}";
        Setting.ExportSettings.Add(item);
        OnExportListData();
    }
    void OnBtnRemoveClick()
    {
        var item = SelectItem;
        if (item == null)
        {
            return;
        }
        Setting.ExportSettings.Remove(item);
        OnExportListData();
    }

    void OnBtnExePathClick()
    {
        BuildHelper.OpenFolderPanel(SelectItem.ExePath, "请选择生成路径", _inputExePath);
    }
    void OnBtnBundlePathClick()
    {
        BuildHelper.OpenFolderPanel(SelectItem.BundlePath, "请选择生成路径", _inputBundlePath);
    }
    void OnBtnCopyPathClick()
    {
        BuildHelper.OpenFolderPanel(SelectItem.CopyPath, "请选择CDN路径", _inputCopyPath);
    }

    void OnBuildSelectClick()
    {
        var buildType = (BuildType)_buildType.value;
        var resVersion = _txtVersion.value.Trim();
        var buildResVerion = SelectItem.ResVersion.Trim();
        if (string.Compare(resVersion, buildResVerion, true) <= 0)
        {
            if (EditorUtility.DisplayDialog("提示", $"资源版本不可小于当前版本", "确定", "取消"))
            {
                _txtVersion.SetValueWithoutNotify(AddVersion(buildResVerion));
            }
            return;
        }
        void Build()
        {
            string content = string.Empty;
            switch (buildType)
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

    void OnClearClick()
    {
        if (EditorUtility.DisplayDialog("提示", $"是否重置构建！\n重置会删除所有构建相关目录！", "确定", "取消"))
        {
            if (Directory.Exists(SelectItem.ExePath))
            {
                Directory.Delete(SelectItem.ExePath, true);
                Log.Debug($"删除构建目录：{SelectItem.ExePath}");
            }
            if (Directory.Exists(SelectItem.CopyPath))
            {
                Directory.Delete(SelectItem.CopyPath, true);
                Log.Debug($"删除拷贝目录：{SelectItem.CopyPath}");
            }

            if (EditorTools.DeleteDirectory(SandboxRoot))
            {
                BuildLogger.Log($"删除沙盒缓存：{SandboxRoot}");
            }

            // 删除平台总目录                        
            if (EditorTools.DeleteDirectory(BuildOutputRoot))
            {
                Log.Debug($"删除资源目录：{BuildOutputRoot}");
            }

            EditorTools.ClearFolder(StreamingAssetsRoot);

            SelectItem.Clear();
            RefreshView();
            AssetDatabase.Refresh();
            Log.Debug("重置成功");
        }
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
        EditorApplication.LockReloadAssemblies();
        var succ = await _ExecuteBuild(buildTarget);
        EditorApplication.UnlockReloadAssemblies();
        if (succ)
        {
            SaveConfig();
        }
    }
    async UniTask<bool> _ExecuteBuild(BuildTarget buildTarget)
    {
        if (!await CompileDLL(buildTarget))
        {
            return false;
        }
        if (!BuildRes(buildTarget))
        {
            return false;
        }
        if (!BuildExe(buildTarget))
        {
            return false;
        }
        return true;
    }
    private async UniTask<bool> CompileDLL(BuildTarget target)
    {
        if (_tgCompileDLL.value)
        {
            HybridCLRCommand.ClearHOTDll();
            await UxEditor.Export(false);
            var compileType = (CompileType)_compileType.value;
            if (IsExportExecutable)
            {
                if (target != EditorUserBuildSettings.activeBuildTarget &&
                    !EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildPipeline.GetBuildTargetGroup(target), target))
                {
                    Log.Debug("切换编译平台失败");
                    return false;
                }
                HybridCLRCommand.ClearAOTDll();
                Log.Debug("---------------------------------------->执行HybridCLR预编译<---------------------------------------");
                CompileDllCommand.CompileDll(target, compileType == CompileType.Development);
                Il2CppDefGeneratorCommand.GenerateIl2CppDef();

                // 这几个生成依赖HotUpdateDlls
                LinkGeneratorCommand.GenerateLinkXml(target);

                // 生成裁剪后的aot dll
                StripAOTDllCommand.GenerateStripedAOTDlls(target);

                // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
                MethodBridgeGeneratorCommand.GenerateMethodBridge(target);
                ReversePInvokeWrapperGeneratorCommand.GenerateReversePInvokeWrapper(target);
                AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
                //PrebuildCommand.GenerateAll();

                Log.Debug("---------------------------------------->将AOT元数据Dll拷贝到资源打包目录<---------------------------------------");
                HybridCLRCommand.CopyAOTAssembliesToYooAssetPath(target);
            }
            else
            {
                Log.Debug("---------------------------------------->生成热更DLL<---------------------------------------");
                CompileDllCommand.CompileDll(target, compileType == CompileType.Development);
            }

            Log.Debug("---------------------------------------->将热更DLL拷贝到资源打包目录<---------------------------------------");
            HybridCLRCommand.CopyHotUpdateAssembliesToYooAssetPath(target);
            Log.Debug("编译热更DLL完毕！");
        }
        return true;
    }
    private bool BuildRes(BuildTarget buildTarget)
    {
        var packageValue = _buildPackage.value;
        if (packageValue == 0)
        {
            Log.Error("没有选中要构建的资源包");
            return false;
        }

        if (IsForceRebuild)
        {
            EditorTools.ClearFolder(StreamingAssetsRoot);

            if (_tgClearSandBox.value)
            {
                if (EditorTools.DeleteDirectory(SandboxRoot))
                {
                    BuildLogger.Log($"删除沙盒缓存");
                }
            }
        }

        List<string> packages = new List<string>();

        if (IsExportExecutable || packageValue == -1)
        {
            packages.AddRange(_buildPackageNames);
        }
        else
        {
            var array = Convert.ToString(_buildPackage.value, 2).ToCharArray().Reverse();
            var pCnt = array.Count();
            for (int i = 0; i < _buildPackageNames.Count; i++)
            {
                if (i < pCnt)
                {
                    if (array.ElementAt(i) == '1')
                    {
                        packages.Add(_buildPackageNames[i]);
                    }
                }
            }
        }

        if (packages.Count > 0)
        {
            //Log.Debug("---------------------------------------->收集着色器变种<---------------------------------------");

            //UniTask CollectSVC(string path, string package, int processCapacity)
            //{
            //    var index1 = path.LastIndexOf('/');
            //    var index2 = path.LastIndexOf('.');

            //    var name = path.Substring(index1 + 1, index2 - index1 - 1);
            //    path = path.Replace(name, $"{package}SVC");
            //    var task = AutoResetUniTaskCompletionSource.Create();
            //    Action callback = () =>
            //    {
            //        task.TrySetResult();
            //    };
            //    //ShaderVariantCollector.Run(path, package, processCapacity, callback);
            //    return task.Task;
            //}

            //string savePath = ShaderVariantCollectorSettingData.Setting.SavePath;
            //int processCapacity = ShaderVariantCollectorSettingData.Setting.ProcessCapacity;
            //foreach (var package in packages)
            //{
            //    await CollectSVC(savePath, package, processCapacity);
            //}

            Log.Debug("---------------------------------------->开始资源打包<---------------------------------------");
            foreach (var package in packages)
            {
                if (!BuildRes(buildTarget, package)) return false;
            }
            Log.Debug("完成资源打包");
        }
        return true;
    }


    string BuildOutputRoot
    {
        get
        {
            if (!string.IsNullOrEmpty(SelectItem.BundlePath))
            {
                return SelectItem.BundlePath;
            }
            return AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        }
    }

    string StreamingAssetsRoot
    {
        get
        {
            return AssetBundleBuilderHelper.GetStreamingAssetsRoot();
        }
    }

    string SandboxRoot
    {
        get
        {
            string projectPath = Path.GetDirectoryName(UnityEngine.Application.dataPath);
            projectPath = PathUtility.RegularPath(projectPath);
            return PathUtility.Combine(projectPath, YooAssetSettingsData.Setting.DefaultYooFolderName);
        }
    }

    private bool BuildRes(BuildTarget buildTarget, string packageName)
    {
        if (IsForceRebuild)
        {
            // 删除平台总目录            
            string platformDirectory = $"{BuildOutputRoot}/{packageName}/{buildTarget}";
            if (EditorTools.DeleteDirectory(platformDirectory))
            {
                BuildLogger.Log($"删除平台总目录：{platformDirectory}");
            }
        }

        var packageSetting = SelectItem.GetPackageSetting(packageName);
        BuildParameters buildParameters = null;
        IBuildPipeline pipeline = null;
        string buildOutputRoot = BuildOutputRoot;
        switch (packageSetting.PiplineOption)
        {
            case EBuildPipeline.BuiltinBuildPipeline:
                buildParameters = new BuiltinBuildParameters()
                {
                    BuildMode = IsForceRebuild ? EBuildMode.ForceRebuild : EBuildMode.IncrementalBuild,
                    CompressOption = packageSetting.CompressOption,
                    
                };
                pipeline = new BuiltinBuildPipeline();
                break;
            case EBuildPipeline.ScriptableBuildPipeline:
                buildParameters = new ScriptableBuildParameters()
                {
                    BuildMode = EBuildMode.IncrementalBuild,//SBP构建只能用热更模式
                    CompressOption = packageSetting.CompressOption,                    
                };
                pipeline = new ScriptableBuildPipeline();
                break;
            case EBuildPipeline.RawFileBuildPipeline:
                buildParameters = new RawFileBuildParameters()
                {
                    BuildMode = EBuildMode.ForceRebuild,//RawFile构建只能用强更模式                    
                };
                pipeline = new RawFileBuildPipeline();
                break;
            default:
                Log.Error("未知的构建模式");
                return false;                
        }

        buildParameters.BuildOutputRoot = buildOutputRoot;
        buildParameters.BuildinFileRoot = StreamingAssetsRoot;
        buildParameters.BuildPipeline = packageSetting.PiplineOption.ToString();
        buildParameters.BuildTarget = buildTarget;
        buildParameters.PackageName = packageName;
        buildParameters.PackageVersion = _txtVersion.value;
        buildParameters.VerifyBuildingResult = true;
        //buildParameters.SharedPackRule = CreateSharedPackRuleInstance();        
        buildParameters.FileNameStyle = packageSetting.NameStyleOption;
        buildParameters.BuildinFileCopyOption = IsForceRebuild
            ? EBuildinFileCopyOption.OnlyCopyByTags : EBuildinFileCopyOption.None;
        buildParameters.BuildinFileCopyParams = packageSetting.BuildTags;
        buildParameters.EncryptionServices = CreateEncryptionServicesInstance(packageSetting.EncyptionClassName);

        var versionFileName = YooAssetSettingsData.GetPackageVersionFileName(packageName);
        var outputDir = "Output";
        var buildPath = $"{buildOutputRoot}/{buildTarget}/{packageName}";
        var nowVersion = _txtVersion.value;
        var lastVersion = string.Empty;
        var versionFile = $"{buildPath}/{outputDir}/{versionFileName}";
        if (File.Exists(versionFile))
        {
            lastVersion = FileUtility.ReadAllText(versionFile);
        }

        var result = pipeline.Run(buildParameters, true);
        if (!result.Success)
        {
            Log.Error("资源打包失败");
            return false;
        }

        //不拷贝link.xml
        //if (IsExportExecutable)
        //{
        //    var tLinkPath = $"{buildPath}/{nowVersion}/link.xml";
        //    if (File.Exists(tLinkPath))
        //    {
        //        var linkPath = $"{Application.dataPath}/Main/YooAsset/{packageName}";
        //        if (!Directory.Exists(linkPath))
        //            Directory.CreateDirectory(linkPath);
        //        File.Copy(tLinkPath, $"{linkPath}/link.xml", true);
        //    }
        //}



        var sPath = $"{buildPath}/{nowVersion}";

        var nowJsonFileName = YooAssetSettingsData.GetManifestJsonFileName(packageName, nowVersion);
        var nowHashFileName = YooAssetSettingsData.GetPackageHashFileName(packageName, nowVersion);
        var nowBinaryFileName = YooAssetSettingsData.GetManifestBinaryFileName(packageName, nowVersion);
        var lastBinaryFileName = YooAssetSettingsData.GetManifestBinaryFileName(packageName, lastVersion);

        List<string> files = null;
        if (!string.IsNullOrEmpty(lastVersion))
        {
            files = new List<string>();
            var path1 = $"{buildPath}/{lastVersion}/{lastBinaryFileName}";
            var path2 = $"{buildPath}/{nowVersion}/{nowBinaryFileName}";
            List<string> changedList = new List<string>();
            PackageCompare.CompareManifest(path1, path2, changedList);
            var diffPath = $"{buildPath}/Difference/{lastVersion}_{nowVersion}";
            if (Directory.Exists(diffPath))
            {
                Directory.Delete(diffPath, true);
            }
            Directory.CreateDirectory(diffPath);

            foreach (var file in changedList)
            {
                var temFile = $"{sPath}/{file}";
                File.Copy(temFile, $"{diffPath}/{file}", true);
                files.Add(temFile);
            }
            files.Add($"{sPath}/{versionFileName}");
            files.Add($"{sPath}/{nowBinaryFileName}");
            files.Add($"{sPath}/{nowHashFileName}");
            //files.Add($"{sPath}/{nowJsonFileName}");               
        }

        if (files == null)
        {
            files = Directory.GetFiles(sPath).ToList();
        }

        List<string> desPathList = new List<string>();
        var tPath = $"{buildPath}/{outputDir}";
        if (!Directory.Exists(tPath))
        {
            Directory.CreateDirectory(tPath);
        }
        desPathList.Add(tPath);
        if (_tgCopy.value && !string.IsNullOrEmpty(SelectItem.CopyPath))
        {
            tPath = $"{SelectItem.CopyPath}/{buildTarget}";
            if (!Directory.Exists(tPath))
            {
                Directory.CreateDirectory(tPath);
            }
            desPathList.Add(tPath);
        }

        var reportFileName = YooAssetSettingsData.GetReportFileName(packageName, nowVersion);
        foreach (var file in files)
        {
            var temFile = PathUtility.RegularPath(file);
            var index = temFile.LastIndexOf("/");
            var fileName = temFile.Substring(index + 1);
            if (fileName == "link.xml") continue;
            if (fileName == "buildlogtep.json") continue;
            if (fileName == reportFileName) continue;
            if (fileName == nowJsonFileName) continue;
            foreach (var desPath in desPathList)
            {
                File.Copy(file, Path.Combine(desPath, fileName), true);
            }
        }
        return true;
    }


    private bool BuildExe(BuildTarget buildTarget)
    {
        if (IsExportExecutable)
        {
            BuildOptions buildOptions = BuildOptions.None;
            var compileType = (CompileType)_compileType.value;
            switch (compileType)
            {
                case CompileType.Development:
                    buildOptions = BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging;
                    break;
                case CompileType.Release:
                    buildOptions = BuildOptions.None;
                    break;
            }
            var path = Path.Combine(SelectItem.ExePath, buildTarget.ToString());
            BuildPlayerOptions buildPlayerOptions = BuildHelper.GetBuildPlayerOptions(buildTarget, buildOptions, path, "Game");
            Log.Debug("---------------------------------------->开始程序打包<---------------------------------------");
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Log.Error("打包失败");
                return false;
            }
            Log.Debug("完成程序打包");
            EditorUtility.RevealInFinder(Path.GetFullPath(path));
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
            item.ResVersion = _txtVersion.value;
        }
        Setting?.SaveFile();
        EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path, OpenSceneMode.Single);
    }

    #endregion

    //#region 构建包裹相关
    // 构建包裹相关
    private List<string> GetBuildPackageNames()
    {
        List<string> result = new List<string>();
        foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
        {
            result.Add(package.PackageName);
        }
        return result;
    }

    private IEncryptionServices CreateEncryptionServicesInstance(string encyptionClassName)
    {
        var encryptionClassTypes = EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
        var classType = encryptionClassTypes.Find(x => x.FullName.Equals(encyptionClassName));
        if (classType != null)
            return (IEncryptionServices)Activator.CreateInstance(classType);
        else
            return null;
    }
    ////private ISharedPackRule CreateSharedPackRuleInstance()
    ////{
    ////    if (_sharedPackRule.index < 0)
    ////        return null;
    ////    var classType = _sharedPackRuleClassTypes[_sharedPackRule.index];
    ////    return (ISharedPackRule)Activator.CreateInstance(classType);
    ////}

    //private static List<Type> GetEncryptionServicesClassTypes()
    //{
    //    return EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
    //}
    ////private static List<Type> GetSharedPackRuleClassTypes()
    ////{
    ////    return EditorTools.GetAssignableTypes(typeof(ISharedPackRule));
    ////}
    //#endregion

}
