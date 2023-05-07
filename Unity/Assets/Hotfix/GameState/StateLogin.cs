using Ux;

namespace Ux
{
    public class StateLogin : StateNode
    {
        protected override void OnCreate(object args = null)
        {

        }
        protected override void OnEnter(object args)
        {
            NetMgr.Instance.Release();
            TagMgr.Instance.Release();
            UIMgr.Instance.Release();
            ModuleMgr.Instance.Release();
            ModuleMgr.Instance.Create();
            UIMgr.Instance.Show<UI.LoginView>();            
        }
        protected override void OnUpdate()
        {
        }
        protected override void OnExit()
        {            
        }
    }
}
