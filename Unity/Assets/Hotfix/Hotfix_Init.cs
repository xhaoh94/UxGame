using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using Ux.UI;

namespace Ux
{
    public class Hotfix_Init : MonoBehaviour
    {
#if UNITY_EDITOR
        bool __CHANGED_BOOT;
#endif
        private void Awake()
        {
#if UNITY_EDITOR            
            if (GameMain.Ins == null)
            {
                __CHANGED_BOOT = true;
                UnityEditor.EditorApplication.isPlaying = false;
                //throw new System.Exception("切换Boot场景启动");                
                return;
            }
#endif            
            Log.Info("启动热更层");

            PatchMgr.Ins.Done();

            ResMgr.Ins.UnloadUnusedAssets();
            
            EventMgr.Ins.___SetEvtAttribute<EvtAttribute>();
            HotFixMgr.Ins.Assemblys.ForEach(assembly =>
            {
                assembly.Initialize();
            });

            
            ConfigMgr.Ins.Init();
            UIMgr.Ins.OnLowMemory();
            UIMgr.Dialog.SetDefalutType<CommonDialog>();

            GameMain.Machine.AddNode<StateLogin>();
            GameMain.Machine.AddNode<StateGameIn>();
            GameMain.Machine.Enter<StateLogin>();

            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private async void OnApplicationQuit()
        {
            if (__CHANGED_BOOT)
            {
                await Task.Delay(100);
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(UnityEditor.EditorBuildSettings.scenes[0].path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                UnityEditor.EditorApplication.isPlaying = true;
            }
        }
#endif
    }

}
