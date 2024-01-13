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
public partial class VersionWindow : EditorWindow
{
    [MenuItem("UxGame/构建/构建打包", false, 540)]
    public static void Build()
    {
        var window = GetWindow<VersionWindow>("VersionWindow", true);
        window.minSize = new Vector2(800, 500);
    }

    [MenuItem("UxGame/构建/本地资源服务器", false, 541)]
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
    Toggle _tgClearSandBox;
    Toggle _tgCompileDLL;
    Toggle _tgCompileAot;
    Toggle _tgCompileUI;
    Toggle _tgCompileConfig;
    Toggle _tgCompileProto;

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
            _platformType.RegisterValueChangedCallback(evt =>
            {
                SelectItem.PlatformType = (PlatformType)_platformType.value;
            });
            // 构建包裹
            _buildPackage = _exportElement.Q<MaskField>("buildPackage");
            _buildPackage.choices = _buildPackageNames;
            _buildPackage.value = -1;            

            //构建版本
            _txtVersion = _exportElement.Q<TextField>("txtVersion");
            _txtVersion.isReadOnly = true;
            _txtVersion.SetEnabled(false);

            //编译类型
            _buildType = _exportElement.Q<EnumField>("buildType");
            _buildType.Init(BuildType.IncrementalBuild);            
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
            _tgCompileDLL.SetValueWithoutNotify(true);
            _tgCompileDLL.RegisterValueChangedCallback(evt =>
            {
                RefreshElement();
            });
            _tgCompileAot = _exportElement.Q<Toggle>("tgCompileAot");
            _tgCompileAot.SetValueWithoutNotify(true);
            _tgCompileUI = _exportElement.Q<Toggle>("tgCompileUI");
            _tgCompileUI.SetValueWithoutNotify(true);
            _tgCompileConfig = _exportElement.Q<Toggle>("tgCompileConfig");
            _tgCompileConfig.SetValueWithoutNotify(true);
            _tgCompileProto = _exportElement.Q<Toggle>("tgCompileProto");
            _tgCompileProto.SetValueWithoutNotify(true);

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
            //_packageMenu.style.width = 200;
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
        _tgCompileAot.style.display = _tgCompileDLL.value && IsExportExecutable ? DisplayStyle.Flex : DisplayStyle.None;
        _tgCompileUI.style.display = _tgCompileDLL.value ? DisplayStyle.Flex : DisplayStyle.None;
        _tgCompileConfig.style.display = _tgCompileDLL.value ? DisplayStyle.Flex : DisplayStyle.None;
        _tgCompileProto.style.display = _tgCompileDLL.value ? DisplayStyle.Flex : DisplayStyle.None;
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
        if (EditorApplication.isCompiling)
        {
            Log.Error("请等待编译完成后再导出");
            return;
        }
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
            item.ResVersion = _txtVersion.value;
        }
        Setting?.SaveFile();
        EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path, OpenSceneMode.Single);
    }

    #endregion



}
