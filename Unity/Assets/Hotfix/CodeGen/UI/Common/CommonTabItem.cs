// 自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ux.UI
{
    public partial class CommonTabItem : UIView
    {
        protected TMP_Text txtTitle;

        protected override void CreateChildren()
        {
            var root = Transform;
            txtTitle = root.GetChild(2).GetComponent<TMP_Text>();
        }
    }
}
