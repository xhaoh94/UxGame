using UnityEngine;
using Ux.UI;

namespace Ux
{
    public class Hotfix_Init : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_EDITOR
            if (GameMain.Ins == null)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                throw new System.Exception("请在Boot场景启动");
            }
#endif            
            Log.Info("启动热更层");
            EventMgr.Ins.___SetEvtAttribute<EvtAttribute>();
            HotFixMgr.Ins.Assembly.Initialize();

            PatchMgr.Ins.Done();
            UIMgr.Dialog.SetDefalutType<CommonDialog>();
            ConfigMgr.Ins.Init();
            GameMain.Machine.AddNode<StateLogin>();
            GameMain.Machine.AddNode<StateGameIn>();
            GameMain.Machine.Enter<StateLogin>();

            Destroy(gameObject);
        }
    }

}
