// 自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ux.UI
{
    public partial class BagWindow : UIWindow
    {
        protected Common2TabFrame mCommonBg;
        protected GameObject btnTt;
        protected TMP_Text txtTt;

        protected override void CreateChildren()
        {
            var root = GameObject.transform;
            mCommonBg = new Common2TabFrame(root.GetChild(0).gameObject, this);
            btnTt = root.GetChild(3).gameObject;
            var node_3 = root.GetChild(3);
            txtTt = node_3.GetChild(0).GetComponent<TMP_Text>();
        }
    }
}
