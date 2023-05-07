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
        protected override void OnEnter(object args = null)
        {
            //var ui = await UIMgr.Instance.Show<UI.MainView>().Task();
            MapModule.Instance.EnterMap("Map001");
            UIMgr.Instance.Hide<UI.LoginView>();
        }

        protected override void OnExit()
        {
            //UIMgr.Instance.Hide<UI.MainView>();
        }

        protected override void OnUpdate()
        {

        }
    }
}
