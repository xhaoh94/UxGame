// 自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ux.UI
{
    public partial class BagWindow : UIWindow
    {
        protected RectTransform rect;
        protected RectTransform body;
        protected CommonWindowFrame mCommonFrame;
        protected GameObject btnTt;
        protected TMP_Text txtTt;
        protected override RectTransform __winBody => body;
        protected override RectTransform __winDragArea => rect;

        protected override void CreateChildren()
        {
            var root = Transform;
            rect = root.GetChild(0) as RectTransform;
            body = root.GetChild(1) as RectTransform;
            var node_1 = root.GetChild(1);
            mCommonFrame = new CommonWindowFrame(node_1.GetChild(0).gameObject, this);
            btnTt = node_1.GetChild(2).gameObject;
            var node_1_2 = node_1.GetChild(2);
            txtTt = node_1_2.GetChild(0).GetComponent<TMP_Text>();
        }
    }
}
