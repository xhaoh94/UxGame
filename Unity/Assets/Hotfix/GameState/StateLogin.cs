namespace Ux
{
    public class StateLogin : StateNode
    {
        protected override void OnCreate(object args = null)
        {

        }
        protected override async void OnEnter()
        {
            NetMgr.Ins.Release();
            TagMgr.Ins.Release();
            UIMgr.Ins.Release();
            ModuleMgr.Ins.Release();
            ModuleMgr.Ins.Create();
            await UIMgr.Ins.Show<UI.LoginView>().Task();
            PatchMgr.Ins.Done();
        }
    }
}
