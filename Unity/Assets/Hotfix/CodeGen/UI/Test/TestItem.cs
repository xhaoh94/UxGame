//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[ItemUrl("ui://7vg0rkntrxzz6")]
	public partial class TestItem : ItemRenderer
	{
		protected GTextField txtNum;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				txtNum = (GTextField)gCom.GetChildAt(1);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
	}
}
