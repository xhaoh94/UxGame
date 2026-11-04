// 自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ux.UI
{
    public partial class Common2TabFrame : UITabFrame
    {
        protected GameObject btnClose;

        public Common2TabFrame(GameObject gameObject, UIObject parent) : base(gameObject, parent) { }
        protected override GameObject __btnClose => btnClose;

        protected override void CreateChildren()
        {
            var root = Transform;
            btnClose = root.GetChild(1).gameObject;
        }
    }
}
