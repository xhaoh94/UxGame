namespace Ux
{
    public class StateGameIn : StateNode
    {
        protected override void OnCreate(object args = null)
        {

        }
        protected override async void OnEnter()
        {
            Entity.Init();
            await SceneModule.Ins.EnterScene("Map001");
            await UIMgr.Ins.Show<UI.MainView>().Task();
            UIMgr.Ins.Hide<UI.LoginView>();

            //var item = ConfigMgr.Ins.Tables.TbItem.Get(10000);
        }

        protected override void OnExit()
        {
            Entity.Release();
            UIMgr.Ins.Hide<UI.MainView>();
            SceneModule.Ins.LeaveScene();
        }
    }
}
