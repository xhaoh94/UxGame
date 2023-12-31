namespace Ux
{
    public class StateGameIn : StateNode
    {
        protected override void OnCreate(object args = null)
        {

        }
        protected override async void OnEnter(object args = null)
        {
            await MapModule.Ins.EnterMap("Map001");
            await UIMgr.Ins.Show<UI.MainView>().Task();
            UIMgr.Ins.Hide<UI.LoginView>();

            //var item = ConfigMgr.Ins.Tables.TbItem.Get(10000);
        }

        protected override void OnExit()
        {
            UIMgr.Ins.Hide<UI.MainView>();
            MapModule.Ins.ExitMap();
        }

        protected override void OnUpdate()
        {

        }
    }
}
