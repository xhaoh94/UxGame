//自动生成的代码，请勿修改!!!
using TMPro;
using UnityEngine;
namespace Ux.UI
{
    public partial class CommonTip : UITip
    {
        protected TMP_Text txtContent;
        protected override void CreateChildren()
        {
            txtContent = GameObject.transform.GetChild(1).GetComponent<TMP_Text>();
        }
        protected override TMP_Text __txtContent => txtContent;
    }
}
