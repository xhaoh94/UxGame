//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[ItemUrl("ui://7vg0rkntrxzz7")]
	public partial class Test22Item : ItemRenderer
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
