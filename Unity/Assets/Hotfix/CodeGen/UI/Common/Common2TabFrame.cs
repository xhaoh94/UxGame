//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	public partial class Common2TabFrame : UITabFrame
	{
		protected override UIButton __btnClose => btnClose;

		protected BtnClose btnClose;
		public Common2TabFrame(GObject gObject,UIObject parent)
		{
			Init(gObject,parent);
			parent?.Components?.Add(this);
		}
		protected override void CreateChildren()
		{
			var gCom = ObjAs<GComponent>();
			btnClose = new BtnClose(gCom.GetChildAt(1), this);
		}
	}
}
