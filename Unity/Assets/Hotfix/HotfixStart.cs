using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using UnityEngine;
using Ux.UI;

namespace Ux
{
    public class HotfixStart : MonoBehaviour
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
                return;
            }
#endif            
            Log.Info("启动热更层");            
            HotFixMgr.Ins.Assemblys.ForEach(assembly =>
            {
                assembly.Initialize();
            });
            EventMgr.Ins.SetEvtAttribute<EvtAttribute>();
#if UNITY_EDITOR
            EventMgr.Ins.SetHotfixEvtType(typeof(EventType));
#endif
            UnityPool.Init();            
            ConfigMgr.Ins.Init();            
            UIMgr.MessageBox.SetDefalutType<CommonMessageBox>();
            UIMgr.Tip.SetDefalutType<CommonTip>();


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
