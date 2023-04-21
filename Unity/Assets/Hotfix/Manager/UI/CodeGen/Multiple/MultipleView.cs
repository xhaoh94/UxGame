//自动生成的代码，请勿修改!!!
using FairyGUI;
using Ux;
namespace Ux.UI
{
	[Package("Multiple","Common")]
	[Lazyload("lazyload_multiple")]
	public partial class MultipleView : UIView
	{
		protected override string PkgName => "Multiple";
		protected override string ResName => "MultipleView";

		protected Common1TabFrame mCommonBg;
		protected override void CreateChildren()
		{
			var gCom = ObjAs<GComponent>();
			mCommonBg = new Common1TabFrame(gCom.GetChildAt(0), this);
		}
		public override void AddChild(UITabView child)
		{
			mCommonBg?.AddChild(child);
		}
		protected void RefreshTab(int selectIndex = 0, bool scrollItToView = true)
		{
			mCommonBg?.Refresh(selectIndex,scrollItToView);
		}
		protected UITabView GetCurrentTab()
		{
			return mCommonBg?.SelectItem;
		}
		protected void SetTabRenderer<T>() where T : UITabBtn
		{
			mCommonBg?.SetTabRenderer<T>();
		}
	}
}
