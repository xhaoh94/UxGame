using Cysharp.Threading.Tasks;
using HybridCLR.Commands;
using HybridCLR.Editor.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UI.Editor;
using UnityEditor;
using UnityEditor.Compilation;
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
public class BuildVersionWindow : EditorWindow
{
    [MenuItem("UxGame/构建/构建打包", false, 400)]
    public static void Build()
    {
        var window = GetWindow<BuildVersionWindow>("BuildVersionWindow", true);
        window.minSize = new Vector2(800, 500);
    }

    [MenuItem("UxGame/构建/本地资源服务器", false, 401)]
    public static void OpenFileServer()
    {
        new Command("../HFS/hfs.exe");
    }



    private List<Type> _encryptionServicesClassTypes;
    private List<string> _encryptionServicesClassNames;
    private List<string> _buildPackageNames;
    private BuildVersionSettingData Setting;
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
    TextField _inputBundlePath;
    Button _btnBundlePath;
    Toggle _tgCopy;
    TextField _inputCopyPath;
    Button _btnCopyPath;

    MaskField _buildPackage;
    TextField _txtVersion;
    EnumField _pipelineType;
    EnumField _nameStyleType;
    EnumField _compressionType;
    TextField _inputBuiltinTags;
    Toggle _tgCompileDLL;
    Toggle _tgClearSandBox;
    PopupField<string> _encryption;

    #endregion

    public void CreateGUI()
    {
        try
        {
            LoadConfig();
            VisualElement root = rootVisualElement;

            var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/BuildVersionWindow.uxml");
            visualAsset.CloneTree(root);
            _encryptionServicesClassTypes = GetEncryptionServicesClassTypes();
            _encryptionServicesClassNames = _encryptionServicesClassTypes.Select(t => t.FullName).ToList();
            _buildPackageNames = GetBuildPackageNames();

            _listExport = root.Q<ListView>("listExport");
            _listExport.makeItem = MakeExportListViewItem;
            _listExport.bindItem = BindExportListViewItem;
            _listExport.onSelectionChange += OnExportListSelectionChange;

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
                SelectItem.CurPlatform = (PlatformType)_platformType.value;
                RefreshWindow();
            });

            //编译类型
            _buildType = _exportElement.Q<EnumField>("buildType");
            _buildType.Init(BuildType.IncrementalBuild);
            _buildType.style.width = 500;
            _buildType.RegisterValueChangedCallback(evt =>
            {
                var resVersion = _txtVersion.value;
                RefreshWindow();
                _txtVersion.SetValueWithoutNotify(resVersion);
            });

            //资源导出路径
            _inputBundlePath = _exportElement.Q<TextField>("inputBundlePath");
            _inputBundlePath.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.BundlePath = evt.newValue;
            });
            _btnBundlePath = _exportElement.Q<Button>("btnBundlePath");
            _btnBundlePath.clicked += OnBtnBundlePathClick;

            //是否拷贝
            _tgCopy = _exportElement.Q<Toggle>("tgCopy");
            _tgCopy.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.IsCopyTo = evt.newValue;
                RefreshElement();
            });
            //拷贝路径
            _inputCopyPath = _exportElement.Q<TextField>("inputCopyPath");
            _inputCopyPath.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.CopyPath = evt.newValue;
            });
            _btnCopyPath = _exportElement.Q<Button>("btnCopyPath");
            _btnCopyPath.clicked += OnBtnCopyPathClick;

            // 是否清理沙盒缓存
            _tgClearSandBox = _exportElement.Q<Toggle>("tgClearSandBox");
            _tgClearSandBox.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.IsClearSandBox = evt.newValue;
            });

            // 是否编译热更DLL
            _tgCompileDLL = _exportElement.Q<Toggle>("tgCompileDLL");
            _tgCompileDLL.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.IsCompileDLL = evt.newValue;
            });

            // 构建包裹
            _buildPackage = _exportElement.Q<MaskField>("buildPackage");
            _buildPackage.choices = _buildPackageNames;
            _buildPackage.value = -1;
            _buildPackage.style.width = 350;

            //资源版本
            _txtVersion = _exportElement.Q<TextField>("txtVersion");
            //_txtVersion.RegisterValueChangedCallback(evt =>
            //{
            //    SelectItem.PlatformConfig.ResVersion = evt.newValue;
            //});        

            // 构建管线
            _pipelineType = _exportElement.Q<EnumField>("pipelineType");
            _pipelineType.Init(EBuildPipeline.ScriptableBuildPipeline);
            _pipelineType.style.width = 500;
            _pipelineType.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.PiplineOption = (EBuildPipeline)evt.newValue;
            });

            // 资源命名格式
            _nameStyleType = _exportElement.Q<EnumField>("nameStyleType");
            _nameStyleType.Init(EOutputNameStyle.HashName);
            _nameStyleType.style.width = 500;
            _nameStyleType.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.NameStyleOption = (EOutputNameStyle)evt.newValue;
            });

            // 压缩方式
            _compressionType = _exportElement.Q<EnumField>("compressionType");
            _compressionType.Init(ECompressOption.LZ4);
            _compressionType.style.width = 500;
            _compressionType.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.CompressOption = (ECompressOption)evt.newValue;
            });

            // 首包资源标签
            _inputBuiltinTags = _exportElement.Q<TextField>("inputBuiltinTags");
            _inputBuiltinTags.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.BuildTags = evt.newValue;
            });


            // 加密方法
            var encryptionContainer = _exportElement.Q("encryptionContainer");
            if (_encryptionServicesClassNames.Count > 0)
            {
                _encryption = new PopupField<string>(_encryptionServicesClassNames, 0);
                _encryption.label = "加密方法";
                _encryption.style.width = 500;
                _encryption.RegisterValueChangedCallback(evt =>
                {
                    SelectItem.PlatformConfig.EncyptionClassName = evt.newValue;
                });
                encryptionContainer.Add(_encryption);
            }
            else
            {
                _encryption = new PopupField<string>();
                _encryption.label = "加密方法";
                _encryption.style.width = 500;
                encryptionContainer.Add(_encryption);
            }

            _tgExe = _exportElement.Q<Toggle>("tgExe");
            _tgExe.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.IsExportExecutable = evt.newValue;
                RefreshElement();
            });
            _exeElement = _exportElement.Q<VisualElement>("exeElement");
            //导出路径
            _inputExePath = _exeElement.Q<TextField>("inputExePath");
            _inputExePath.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.ExePath = evt.newValue;
            });
            _btnExePath = _exeElement.Q<Button>("btnExePath");
            _btnExePath.clicked += OnBtnExePathClick;

            //编译类型
            _compileType = _exeElement.Q<EnumField>("compileType");
            _compileType.Init(CompileType.Development);
            _compileType.style.width = 500;
            _compileType.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformConfig.CompileType = (CompileType)evt.newValue;
            });

            // 构建按钮
            var btnBuild = _exportElement.Q<Button>("build");
            btnBuild.clicked += OnBuildClick;

            // 清理按钮
            var btnClear = _exportElement.Q<Button>("clear");
            btnClear.clicked += OnClearClick;

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
        RefreshWindow();
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
            RefreshWindow();
        }
    }
    string AddVersion(string version)
    {
        try
        {
            var newVersion = string.Empty;
            var sz = version.Split('.');
            for (int i = 0; i < sz.Length - 1; i++)
            {
                newVersion += sz[i] + ".";
            }
            newVersion += (int.Parse(sz[sz.Length - 1]) + 1).ToString();
            return newVersion;
        }
        catch
        {
            System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
            byte[] bs = asciiEncoding.GetBytes(version);
            bs[bs.Length - 1]++;
            return asciiEncoding.GetString(bs);
        }
    }
    void RefreshWindow()
    {
        if (SelectItem == null)
        {
            _exportElement.visible = false;
            return;
        }
        _exportElement.visible = true;
        _txtName.SetValueWithoutNotify(SelectItem.Name);
        _platformType.SetValueWithoutNotify(SelectItem.PlatformConfig.PlatformType);
        _inputExePath.SetValueWithoutNotify(SelectItem.PlatformConfig.ExePath);
        _compileType.SetValueWithoutNotify(SelectItem.PlatformConfig.CompileType);
        _inputBundlePath.SetValueWithoutNotify(SelectItem.PlatformConfig.BundlePath);
        _tgCopy.SetValueWithoutNotify(SelectItem.PlatformConfig.IsCopyTo);
        _inputCopyPath.SetValueWithoutNotify(SelectItem.PlatformConfig.CopyPath);
        _tgExe.SetValueWithoutNotify(SelectItem.PlatformConfig.IsExportExecutable);
        _txtVersion.SetValueWithoutNotify(AddVersion(SelectItem.PlatformConfig.ResVersion));
        _pipelineType.SetValueWithoutNotify(SelectItem.PlatformConfig.PiplineOption);
        _nameStyleType.SetValueWithoutNotify(SelectItem.PlatformConfig.NameStyleOption);
        _compressionType.SetValueWithoutNotify(SelectItem.PlatformConfig.CompressOption);
        _inputBuiltinTags.SetValueWithoutNotify(SelectItem.PlatformConfig.BuildTags);
        _tgCompileDLL.SetValueWithoutNotify(SelectItem.PlatformConfig.IsCompileDLL);
        _encryption.SetValueWithoutNotify(SelectItem.PlatformConfig.EncyptionClassName);
        _tgClearSandBox.SetValueWithoutNotify(SelectItem.PlatformConfig.IsClearSandBox);
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
        _pipelineType.SetEnabled(IsForceRebuild);
        _nameStyleType.SetEnabled(IsForceRebuild);
        _compressionType.SetEnabled(IsForceRebuild);
        _encryption.SetEnabled(IsForceRebuild);
        _inputBuiltinTags.SetEnabled(IsForceRebuild);
        _inputCopyPath.parent.style.display = _tgCopy.value ? DisplayStyle.Flex : DisplayStyle.None;
    }
    void OnBtnAddClick()
    {
        var dt = DateTime.Now;
        int totalSecond = dt.Hour * 3600 + dt.Minute * 60 + dt.Second;
        var item = new BuildExportSetting();
        item.Name = $"Config-{dt.ToString("yyyy-MM-dd")}-{totalSecond}";
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
        BuildHelper.OpenFolderPanel(SelectItem.PlatformConfig.ExePath, "请选择生成路径", _inputExePath);
    }
    void OnBtnBundlePathClick()
    {
        BuildHelper.OpenFolderPanel(SelectItem.PlatformConfig.BundlePath, "请选择生成路径", _inputBundlePath);
    }
    void OnBtnCopyPathClick()
    {
        BuildHelper.OpenFolderPanel(SelectItem.PlatformConfig.CopyPath, "请选择CDN路径", _inputCopyPath);
    }

    void OnBuildClick()
    {
        var buildType = (BuildType)_buildType.value;
        var resVersion = _txtVersion.value.Trim();
        var buildResVerion = SelectItem.PlatformConfig.ResVersion.Trim();
        if (string.Compare(resVersion, buildResVerion, true) <= 0)
        {
            if (EditorUtility.DisplayDialog("提示", $"资源版本不可小于当前版本", "确定", "取消"))
            {
                _txtVersion.SetValueWithoutNotify(AddVersion(buildResVerion));
            }
            return;
        }
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

    void OnClearClick()
    {
        if (EditorUtility.DisplayDialog("提示", $"是否重置构建！\n重置会删除所有构建相关目录！", "确定", "取消"))
        {
            if (Directory.Exists(SelectItem.PlatformConfig.ExePath))
            {
                Directory.Delete(SelectItem.PlatformConfig.ExePath, true);
                Log.Debug($"删除构建目录：{SelectItem.PlatformConfig.ExePath}");
            }
            if (Directory.Exists(SelectItem.PlatformConfig.CopyPath))
            {
                Directory.Delete(SelectItem.PlatformConfig.CopyPath, true);
                Log.Debug($"删除拷贝目录：{SelectItem.PlatformConfig.CopyPath}");
            }
            var sandBoxPath = "./Sandbox";
            if (EditorTools.DeleteDirectory(sandBoxPath))
            {
                BuildLogger.Log($"删除沙盒缓存");
            }

            // 删除平台总目录                        
            if (EditorTools.DeleteDirectory(OutputRoot))
            {
                Log.Debug($"删除资源目录：{OutputRoot}");
            }
            AssetBundleBuilderHelper.ClearStreamingAssetsFolder();

            SelectItem.Clear();
            RefreshWindow();
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
        var platformType = SelectItem.CurPlatform;
        BuildTarget buildTarget = GetBuildTarget(platformType);
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
        Console.Clear();
        if (!await CompileDLL(buildTarget, buildOptions)) return;
        if (!await BuildRes(buildTarget)) return;
        if (!BuildExe(buildTarget, buildOptions)) return;
        SaveConfig();
    }

    private async UniTask<bool> CompileDLL(BuildTarget buildTarget, BuildOptions buildOptions)
    {
        if (_tgCompileDLL.value)
        {
            Log.Debug("---------------------------------------->生成配置文件Code<---------------------------------------");
            UniTask ExportConfig()
            {
                var configTask = AutoResetUniTaskCompletionSource.Create();
                BuildConfigSettingData.Export(() =>
                {
                    configTask?.TrySetResult();
                });
                return configTask.Task;
            }
            await ExportConfig();

            Log.Debug("------------------------------------>生成YooAsset UI收集器配置<------------------------------");
            UIClassifyWindow.CreateYooAssetUIGroup();

            if (IsExportExecutable)
            {
                if (buildTarget != EditorUserBuildSettings.activeBuildTarget &&
                    !EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget))
                {
                    Log.Debug("切换编译平台失败");
                    return false;
                }

                Log.Debug("---------------------------------------->执行HybridCLR预编译<---------------------------------------");
                PrebuildCommand.GenerateAll();

                Log.Debug("---------------------------------------->将AOT元数据Dll拷贝到资源打包目录<---------------------------------------");
                HybridCLRCommand.CopyAOTAssembliesToYooAssetPath(buildTarget);
            }
            else
            {
                Log.Debug("---------------------------------------->生成热更DLL<---------------------------------------");
                CompileDllCommand.CompileDll(buildTarget);
            }

            Log.Debug("---------------------------------------->将热更DLL拷贝到资源打包目录<---------------------------------------");
            HybridCLRCommand.CopyHotUpdateAssembliesToYooAssetPath(buildTarget);
            Log.Debug("编译热更DLL完毕！");
        }
        return true;
    }
    private async UniTask<bool> BuildRes(BuildTarget buildTarget)
    {
        var packageValue = _buildPackage.value;
        if (packageValue == 0)
        {
            Log.Error("没有选中要构建的资源包");
            return false;
        }

        if (IsForceRebuild)
        {
            AssetBundleBuilderHelper.ClearStreamingAssetsFolder();

            if (_tgClearSandBox.value)
            {
                var sandBoxPath = "./Sandbox";
                if (EditorTools.DeleteDirectory(sandBoxPath))
                {
                    BuildLogger.Log($"删除沙盒缓存：{sandBoxPath}");
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
            Log.Debug("---------------------------------------->收集着色器变种<---------------------------------------");

            UniTask CollectSVC(string path, string package, int processCapacity)
            {
                var index1 = path.LastIndexOf('/');
                var index2 = path.LastIndexOf('.');

                var name = path.Substring(index1 + 1, index2 - index1 - 1);
                path = path.Replace(name, $"{package}SVC");
                var task = AutoResetUniTaskCompletionSource.Create();
                Action callback = () =>
                {
                    task.TrySetResult();
                };
                ShaderVariantCollector.Run(path, package, processCapacity, callback);
                return task.Task;
            }

            string savePath = ShaderVariantCollectorSettingData.Setting.SavePath;
            int processCapacity = ShaderVariantCollectorSettingData.Setting.ProcessCapacity;
            foreach (var package in packages)
            {
                await CollectSVC(savePath, package, processCapacity);
            }           

            Log.Debug("---------------------------------------->开始资源打包<---------------------------------------");
            foreach (var package in packages)
            {
                if (!BuildRes(buildTarget, package)) return false;
            }
            Log.Debug("完成资源打包");
        }
        return true;
    }




    string OutputRoot
    {
        get
        {
            if (!string.IsNullOrEmpty(SelectItem.PlatformConfig.BundlePath))
            {
                return SelectItem.PlatformConfig.BundlePath;
            }
            return AssetBundleBuilderHelper.GetDefaultOutputRoot();
        }
    }
    private bool BuildRes(BuildTarget buildTarget, string packageName)
    {
        if (IsForceRebuild)
        {
            // 删除平台总目录            
            string platformDirectory = $"{OutputRoot}/{packageName}/{buildTarget}";
            if (EditorTools.DeleteDirectory(platformDirectory))
            {
                BuildLogger.Log($"删除平台总目录：{platformDirectory}");
            }
        }

        string outputRoot = OutputRoot;
        BuildParameters buildParameters = new BuildParameters();
        buildParameters.OutputRoot = outputRoot;
        buildParameters.BuildTarget = buildTarget;
        buildParameters.BuildPipeline = (EBuildPipeline)_pipelineType.value;
        if (buildParameters.BuildPipeline == EBuildPipeline.ScriptableBuildPipeline)
        {
            buildParameters.SBPParameters = new BuildParameters.SBPBuildParameters();
            buildParameters.SBPParameters.WriteLinkXML = false;
            buildParameters.BuildMode = EBuildMode.IncrementalBuild;//SBP构建只能用热更模式
        }
        else
        {
            buildParameters.BuildMode = IsForceRebuild ? EBuildMode.ForceRebuild : EBuildMode.IncrementalBuild;
        }
        buildParameters.PackageName = packageName;
        buildParameters.PackageVersion = _txtVersion.value;
        buildParameters.VerifyBuildingResult = true;
        buildParameters.ShareAssetPackRule = new DefaultShareAssetPackRule();
        buildParameters.EncryptionServices = CreateEncryptionServicesInstance();
        buildParameters.OutputNameStyle = (EOutputNameStyle)_nameStyleType.value;
        buildParameters.CompressOption = (ECompressOption)_compressionType.value;
        buildParameters.CopyBuildinFileOption = IsForceRebuild
            ? ECopyBuildinFileOption.OnlyCopyByTags : ECopyBuildinFileOption.None;
        buildParameters.CopyBuildinFileTags = _inputBuiltinTags.value;


        string outputDir = "Output";
        string buildPath = $"{outputRoot}/{buildTarget}/{packageName}";
        string nowVersion = _txtVersion.value;
        string lastVersion = string.Empty;
        var versionFile = $"{buildPath}/{outputDir}/PatchManifest_{packageName}.version";
        if (File.Exists(versionFile))
        {
            lastVersion = FileUtility.ReadAllText(versionFile);
        }

        AssetBundleBuilder builder = new AssetBundleBuilder();
        var result = builder.Run(buildParameters);
        if (!result.Success)
        {
            Log.Error("资源打包失败");
            return false;
        }

        //不拷贝link.xml，因为会导致无法打包
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
        string diffPath = string.Empty;
        if (!string.IsNullOrEmpty(lastVersion))
        {
            var path1 = $"{buildPath}/{lastVersion}/PatchManifest_{packageName}_{lastVersion}.bytes";
            var path2 = $"{buildPath}/{nowVersion}/PatchManifest_{packageName}_{nowVersion}.bytes";
            List<string> changedList = new List<string>();
            PackageCompare.CompareManifest(path1, path2, changedList);
            diffPath = $"{buildPath}/Difference/{lastVersion}_{nowVersion}";
            if (Directory.Exists(diffPath))
            {
                Directory.Delete(diffPath, true);
            }
            Directory.CreateDirectory(diffPath);

            foreach (var file in changedList)
            {
                File.Copy($"{buildPath}/{nowVersion}/{file}", $"{diffPath}/{file}", true);
            }

            File.Copy($"{buildPath}/{nowVersion}/PatchManifest_{packageName}.version", $"{diffPath}/PatchManifest_{packageName}.version", true);
            File.Copy($"{buildPath}/{nowVersion}/PatchManifest_{packageName}_{nowVersion}.bytes",
                $"{diffPath}/PatchManifest_{packageName}_{nowVersion}.bytes", true);
            File.Copy($"{buildPath}/{nowVersion}/PatchManifest_{packageName}_{nowVersion}.hash",
              $"{diffPath}/PatchManifest_{packageName}_{nowVersion}.hash", true);
        }
        var sPath = diffPath;
        if (string.IsNullOrEmpty(diffPath))
        {
            sPath = $"{buildPath}/{nowVersion}";
        }
        var tPath = $"{buildPath}/{outputDir}";
        if (!Directory.Exists(tPath))
        {
            Directory.CreateDirectory(tPath);
        }
        var files = Directory.GetFiles(sPath);
        foreach (var file in files)
        {
            if (file == "link.xml") { continue; }
            if (file == "buildlogtep") { continue; }
            var desFile = file.Replace(sPath, tPath);
            File.Copy(file, desFile, true);
        }
        if (_tgCopy.value)
        {
            CopyTo(tPath, buildTarget);
        }

        return true;
    }
    private void CopyTo(string sPath, BuildTarget buildTarget)
    {
        if (string.IsNullOrEmpty(SelectItem.PlatformConfig.CopyPath))
        {
            return;
        }
        var tPath = $"{SelectItem.PlatformConfig.CopyPath}/{buildTarget}";
        if (!Directory.Exists(tPath))
        {
            Directory.CreateDirectory(tPath);
        }
        var files = Directory.GetFiles(sPath);
        foreach (var file in files)
        {
            var desFile = file.Replace(sPath, tPath);
            File.Copy(file, desFile, true);
        }
    }
    private bool BuildExe(BuildTarget buildTarget, BuildOptions buildOptions)
    {
        if (IsExportExecutable)
        {
            BuildPlayerOptions buildPlayerOptions = BuildHelper.GetBuildPlayerOptions(buildTarget, buildOptions,
                Path.Combine(SelectItem.PlatformConfig.ExePath, buildTarget.ToString()), "Game");
            Log.Debug("---------------------------------------->开始程序打包<---------------------------------------");
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Log.Error("打包失败");
                return false;
            }
            Log.Debug("完成程序打包");
            EditorUtility.RevealInFinder($"{SelectItem.PlatformConfig.ExePath}/{buildTarget}/");
        }
        return true;
    }

    #region 初始化   
    void LoadConfig()
    {
        Setting = SettingTools.GetSingletonAssets<BuildVersionSettingData>("Assets/Editor/Build");
    }
    void SaveConfig()
    {
        var item = SelectItem;
        if (item != null)
        {
            item.PlatformConfig.ResVersion = _txtVersion.value;
        }
        Setting?.SaveFile();
        EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path, OpenSceneMode.Single);
        // 聚焦到游戏窗口
        EditorTools.FocusUnitySceneWindow();        
    }

    #endregion

    #region 构建包裹相关
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

    #endregion

    #region  加密类相关    
    private int GetEncryptionDefaultIndex(string className)
    {
        for (int index = 0; index < _encryptionServicesClassNames.Count; index++)
        {
            if (_encryptionServicesClassNames[index] == className)
            {
                return index;
            }
        }
        return 0;
    }
    private IEncryptionServices CreateEncryptionServicesInstance()
    {
        if (_encryption.index < 0)
            return null;
        var classType = _encryptionServicesClassTypes[_encryption.index];
        return (IEncryptionServices)Activator.CreateInstance(classType);
    }

    private static List<Type> GetEncryptionServicesClassTypes()
    {
        return EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
    }
    #endregion

}
