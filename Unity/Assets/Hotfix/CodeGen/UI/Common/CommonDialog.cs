//自动生成的代码，请勿修改!!!
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Ux.UI
{
    public partial class CommonDialog : UIDialog
    {
        protected TMP_Text txtTitle;
        protected TMP_Text txtContent;
        protected Toggle checkbox;
        protected GameObject btn1;
        protected GameObject btn2;
        protected GameObject btnClose;

        protected override void CreateChildren()
        {
            var root = GameObject.transform;
            txtTitle = root.GetChild(1).GetComponent<TMP_Text>();
            txtContent = root.GetChild(2).GetComponent<TMP_Text>();
            checkbox = root.GetChild(3).GetComponent<Toggle>();
            btn1 = root.GetChild(4).gameObject;
            btn2 = root.GetChild(5).gameObject;
            btnClose = root.GetChild(8).gameObject;
        }
    }
}
