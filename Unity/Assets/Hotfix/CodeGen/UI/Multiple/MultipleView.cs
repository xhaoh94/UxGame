//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Multiple","Common")]
	[Lazyload("lazyload_multiple")]
	public partial class MultipleView : UIView
	{
		protected override string PkgName => "Multiple";
		protected override string ResName => "MultipleView";
		protected Common1TabFrame mCommonBg;
		protected Transition t0;
		protected Transition t1;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				mCommonBg = new Common1TabFrame(gCom.GetChildAt(0), this);
				t0 = gCom.GetTransitionAt(0);
				t1 = gCom.GetTransitionAt(1);
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
		protected IUI GetCurrentTab()
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
