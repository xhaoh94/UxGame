namespace Ux
{
    public class StateLogin : StateNode
    {
        protected override void OnCreate(object args = null)
        {

        }
        protected override void OnEnter(object args)
        {
            NetMgr.Ins.Release();
            TagMgr.Ins.Release();
            UIMgr.Ins.Release();
            ModuleMgr.Ins.Release();
            ModuleMgr.Ins.Create();
            UIMgr.Ins.Show<UI.LoginView>();            
        }
        protected override void OnUpdate()
        {
        }
        protected override void OnExit()
        {            
        }
    }
}
