// 自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ux.UI
{
    public partial class BagWindow : UIWindow
    {
        protected Common1TabFrame mCommonBg;

        protected override void CreateChildren()
        {
            var root = GameObject.transform;
            mCommonBg = new Common1TabFrame(root.GetChild(0).gameObject, this);
        }
    }
}
