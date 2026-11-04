using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.UI
{
    partial class CommonTabItem
    {
        protected override void OnShow()
        {
            if (!TryGetParam(out int id))
                return;

            var data = UIMgr.Ins.GetUIData(id);
            if (data?.TabData?.Title is string title)
                txtTitle.text = title;                
        }
    }
}
