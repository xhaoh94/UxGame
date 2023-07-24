//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Patch")]
	public partial class PatchView : UIView
	{
		protected override string PkgName => "Patch";
		protected override string ResName => "PatchView";

		protected GTextField txt;
		protected PatchBar bar;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				txt = (GTextField)gCom.GetChildAt(1);
				bar = new PatchBar(gCom.GetChildAt(2), this);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
	}
}
