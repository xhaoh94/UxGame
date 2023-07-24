//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	public partial class CommonTabBtn : UITabBtn
	{
		protected GImage redPoint;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				redPoint = (GImage)gCom.GetChildAt(3);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
	}
}
