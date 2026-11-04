// 自动生成的代码，请勿修改!!!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ux.UI
{
    public partial class CommonDialog : UIDialog
    {
        protected TMP_Text txtTitle;
        protected TMP_Text txtContent;
        protected Toggle checkbox;
        protected TMP_Text title;
        protected GameObject btn1;
        protected TMP_Text title2;
        protected GameObject btn2;
        protected TMP_Text title3;
        protected GameObject btnClose;

        protected override void CreateChildren()
        {
            var root = Transform;
            txtTitle = root.GetChild(1).GetComponent<TMP_Text>();
            txtContent = root.GetChild(2).GetComponent<TMP_Text>();
            checkbox = root.GetChild(3).GetComponent<Toggle>();
            var node_3 = root.GetChild(3);
            title = node_3.GetChild(3).GetComponent<TMP_Text>();
            btn1 = root.GetChild(4).gameObject;
            var node_4 = root.GetChild(4);
            title2 = node_4.GetChild(3).GetComponent<TMP_Text>();
            btn2 = root.GetChild(5).gameObject;
            var node_5 = root.GetChild(5);
            title3 = node_5.GetChild(3).GetComponent<TMP_Text>();
            btnClose = root.GetChild(8).gameObject;
        }
    }
}
