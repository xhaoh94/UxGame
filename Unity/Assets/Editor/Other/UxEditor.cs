using Cysharp.Threading.Tasks;
using UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UxEditor
{
    public static async UniTask Export(bool isRefresh = true)
    {
        //生成YooAsset UI收集器配置
        UIClassifyWindow.CreateYooAssetUIGroup();
        //生成UI代码文件
        UICodeGenWindow.Export();

        //TODO 协议文件

        //生成配置代码文件
        await ConfigWindow.Export();

        if (isRefresh) AssetDatabase.Refresh();
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
