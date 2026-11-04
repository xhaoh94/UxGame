//自动生成的代码，请勿修改!!!
using UnityEngine;
using TMPro;
namespace Ux.UI
{
    public partial class Test22Item : ItemRenderer
    {
        protected TMP_Text txtNum;
        protected override void CreateChildren()
        {
            txtNum = GameObject.transform.GetChild(1).GetComponent<TMP_Text>();
        }
    }
}
