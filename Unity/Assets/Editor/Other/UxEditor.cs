using Cysharp.Threading.Tasks;
using UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UxEditor
{
    public static async UniTask<bool> Export(bool _ui = true, bool _config = true, bool _proto = true, bool _isRefresh = true)
    {
        if (_ui)
        {
            //生成YooAsset UI收集器配置
            UIClassifyWindow.CreateYooAssetUIGroup();
            //生成UI代码文件
            if (!UICodeGenWindow.Export())
            {
                return false;
            }
        }

        if (_config)
        {
            //生成配置代码文件
            await ConfigWindow.Export();
        }

        if (_proto)
        {
            //生成协议文件
            await ProtoWindow.Export();
        }

        if (_isRefresh) AssetDatabase.Refresh();

        return true;
    }

    [MenuItem("UxGame/初始化", false, 10)]
    public static void Init()
    {
        Export().Forget();
    }
    [MenuItem("UxGame/切换到/Boot", false, 1000)]
    public static void ChangeBoot()
    {
        EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path, OpenSceneMode.Single);
    }
    [MenuItem("UxGame/切换到/Boot并启动", false, 1001)]
    public static void ChangeBootRun()
    {
        ChangeBoot();
        UnityEditor.EditorApplication.isPlaying = true;
    }
}
