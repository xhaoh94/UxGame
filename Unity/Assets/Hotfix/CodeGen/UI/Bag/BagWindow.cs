//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Bag","Common")]
	[Lazyload("lazyload_bag")]
	public partial class BagWindow : UIWindow
	{
		protected override string PkgName => "Bag";
		protected override string ResName => "BagWindow";
		protected Common2TabFrame mCommonBg;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<Window>().contentPane;
				mCommonBg = new Common2TabFrame(gCom.GetChildAt(0), this);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
		public override void AddChild(UITabView child)
		{
			mCommonBg?.AddChild(child);
		}
		protected void RefreshTab(int selectIndex = 0, bool scrollItToView = true)
		{
			mCommonBg?.Refresh(selectIndex,scrollItToView);
		}
		protected ITabView GetCurrentTab()
		{
			return mCommonBg?.SelectItem;
		}
		public void SetItemRenderer<T>() where T : ItemRenderer
		{
			mCommonBg?.SetItemRenderer<T>();
		}
		public void SetItemProvider(System.Func<int, System.Type> itemTypeFunc)
		{
			mCommonBg?.SetItemProvider(itemTypeFunc);
		}
	}
}
