//自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;

namespace Ux.UI
{
    public partial class Common2TabFrame : UITabFrame
    {
        protected GameObject btnClose;

        public Common2TabFrame(GameObject gameObject, UIObject parent)
            : base(gameObject, parent)
        {
        }

        protected override void CreateChildren()
        {
            // FGUI: Common2TabFrame.xml displayList = graph n0(0), btnClose(1)
            btnClose = GameObject.transform.GetChild(1).gameObject;
        }

        protected override GameObject __btnClose => btnClose;
    }
}
