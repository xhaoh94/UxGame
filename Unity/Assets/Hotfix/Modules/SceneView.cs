using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux.UI
{
    [UI]
    public partial class SceneView
    {
        protected override UILayer Layer => UILayer.Normal;
        public override UIType Type => UIType.Fixed;
        partial void OnBtnBackClick(EventContext e)
        {
            GameMain.Machine.Enter<StateLogin>();
        }

        partial void OnBtnMainViewClick(EventContext e)
        {
            UIMgr.Ins.Show<UI.MainView>();
        }
    }
}
