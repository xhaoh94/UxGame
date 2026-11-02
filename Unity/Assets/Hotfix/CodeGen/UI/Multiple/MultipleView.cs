//自动生成的代码，请勿修改!!!
using UnityEngine;

namespace Ux.UI
{
    public partial class MultipleView : UIView
    {
        protected Common1TabFrame mCommonBg;

        protected override void CreateChildren()
        {
            mCommonBg = new Common1TabFrame(GameObject.transform.GetChild(0).gameObject, this);
        }

        public override void AddChild(UITabView child)
        {
            mCommonBg?.AddChild(child);
        }

        protected void RefreshTab(int selectIndex = 0, bool scrollItToView = true)
        {
            mCommonBg?.Refresh(selectIndex, scrollItToView);
        }

        protected IUI GetCurrentTab()
        {
            return mCommonBg?.SelectItem;
        }

                public void SetItemProvider(System.Func<int, int> templateIdProvider)
        {
            mCommonBg?.SetItemProvider(templateIdProvider);
        }
    }
}
