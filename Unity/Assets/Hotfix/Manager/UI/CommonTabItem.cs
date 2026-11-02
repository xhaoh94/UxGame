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
                UITextUtil.SetText(txtTitle, title);
        }
    }

    internal static class UITextUtil
    {
        /// <summary>
        /// 统一写文本：优先 TMP，其次传统 UI Text。
        /// </summary>
        public static void SetText(GameObject target, string value)
        {
            if (target == null)
                return;

            var tmp = target.GetComponent<TMP_Text>();
            if (tmp == null)
                tmp = target.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = value ?? string.Empty;
                return;
            }

            var text = target.GetComponent<Text>();
            if (text == null)
                text = target.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = value ?? string.Empty;
        }
    }
}
