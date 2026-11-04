// 自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ux.UI
{
    public partial class CommonTip : UITip
    {
        protected TMP_Text txtContent;
        protected override TMP_Text __txtContent => txtContent;

        protected override void CreateChildren()
        {
            var root = Transform;
            txtContent = root.GetChild(1).GetComponent<TMP_Text>();
        }
    }
}
