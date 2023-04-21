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
			var gCom = ObjAs<Window>().contentPane;
			mCommonBg = new Common2TabFrame(gCom.GetChildAt(0), this);
		}
	}
}
