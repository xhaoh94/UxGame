using Ux;
using UnityEngine;

namespace Ux
{
    public class Hotfix_Init : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_EDITOR
            if (GameMain.Instance == null)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                throw new System.Exception("请在Boot场景启动");
            }
#endif
            Log.Info("启动热更层");
            //DontDestroyOnLoad(gameObject);
            EventMgr.Instance.___SetEvtAttribute<EvtAttribute>();
            HotFixMgr.Instance.Assembly.Initialize();

            PatchMgr.Instance.Done();
            GameMain.Machine.AddNode<StateLogin>();
            GameMain.Machine.AddNode<StateGameIn>();
            GameMain.Machine.Enter<StateLogin>();
        }
    }

}
