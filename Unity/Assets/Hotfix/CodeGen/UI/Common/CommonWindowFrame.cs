// 自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ux.UI
{
    public partial class CommonWindowFrame : UIWindowFrame
    {
        protected RectTransform panelTitleBar;
        protected GameObject btnClose;
        protected GameObject btnMax;
        protected GameObject btnMin;

        public CommonWindowFrame(GameObject gameObject, UIObject parent) : base(gameObject, parent) { }
        protected override RectTransform __frameTitleBar => panelTitleBar;
        protected override GameObject __frameBtnMin => btnMin;
        protected override GameObject __frameBtnMax => btnMax;
        protected override GameObject __frameBtnClose => btnClose;

        protected override void CreateChildren()
        {
            var root = Transform;
            panelTitleBar = root.GetChild(1) as RectTransform;
            var node_1 = root.GetChild(1);
            btnClose = node_1.GetChild(0).gameObject;
            btnMax = node_1.GetChild(1).gameObject;
            btnMin = node_1.GetChild(2).gameObject;
        }
    }
}
