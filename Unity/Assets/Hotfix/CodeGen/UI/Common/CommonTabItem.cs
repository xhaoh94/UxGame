//自动生成的代码，请勿修改!!!
using UnityEngine;

namespace Ux.UI
{
    public partial class CommonTabItem : ItemRenderer
    {
        protected GameObject txtTitle;

        protected override void CreateChildren()
        {
            var root = GameObject.transform;
            txtTitle = root.GetChild(2).gameObject;
        }
    }
}
