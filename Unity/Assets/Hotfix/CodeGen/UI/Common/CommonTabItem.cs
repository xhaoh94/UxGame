//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[ItemUrl("ui://ex3p2il3ftpt7")]
	public partial class CommonTabItem : ItemRenderer
	{
		protected GTextField txtTitle;
		protected GImage redPoint;
		protected Transition showanim;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				txtTitle = (GTextField)gCom.GetChildAt(2);
				redPoint = (GImage)gCom.GetChildAt(3);
				showanim = gCom.GetTransitionAt(0);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
	}
}
