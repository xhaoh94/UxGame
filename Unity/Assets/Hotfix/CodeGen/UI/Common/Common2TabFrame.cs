//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	public partial class Common2TabFrame : UITabFrame
	{
		protected override UIButton __btnClose => btnClose;
		protected UIButton btnClose;
		public Common2TabFrame(GObject gObject,UIObject parent): base(gObject, parent) { }
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				btnClose = new UIButton(gCom.GetChildAt(1), this);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
	}
}
