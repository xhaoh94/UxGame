// 自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ux.UI
{
    public partial class Common1TabFrame : UITabFrame
    {
        protected RectTransform tabContent;
        protected UIList listTab;
        protected GameObject btnClose;

        public Common1TabFrame(GameObject gameObject, UIObject parent) : base(gameObject, parent) { }
        protected override RectTransform __tabContent => tabContent;
        protected override UIList __listTab => listTab;
        protected override GameObject __btnClose => btnClose;

        protected override void CreateChildren()
        {
            var root = GameObject.transform;
            tabContent = root.GetChild(2) as RectTransform;
            listTab = new UIList(root.GetChild(3).gameObject, this);
            btnClose = root.GetChild(4).gameObject;
        }
    }
}
