//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Common")]
	public partial class CommonTip : UITip
	{
		protected override string PkgName => "Common";
		protected override string ResName => "CommonTip";
		protected override GTextField __txtContent => txtContent;
		protected override Transition __transition => show;
		protected GTextField txtContent;
		protected Transition show;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				txtContent = (GTextField)gCom.GetChildAt(1);
				show = gCom.GetTransitionAt(0);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
	}
}
