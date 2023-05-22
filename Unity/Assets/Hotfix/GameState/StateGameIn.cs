using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Ux
{
    public class StateGameIn : StateNode
    {
        protected override void OnCreate(object args = null)
        {

        }
        protected override async void OnEnter(object args = null)
        {
            var ui = await UIMgr.Ins.Show<UI.MainView>().Task();
            //MapModule.Ins.EnterMap("Map001");
            UIMgr.Ins.Hide<UI.LoginView>();
        }

        protected override void OnExit()
        {
            UIMgr.Ins.Hide<UI.MainView>();
        }

        protected override void OnUpdate()
        {

        }
    }
}
