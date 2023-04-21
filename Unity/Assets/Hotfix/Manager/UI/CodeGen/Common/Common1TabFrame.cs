//自动生成的代码，请勿修改!!!
using FairyGUI;
using Ux;
namespace Ux.UI
{
	public partial class Common1TabFrame : UITabFrame
	{
		protected override GComponent __tabContent => tabContent;
		protected override GList __listTab => listTab;
		protected override UIButton __btnClose => btnClose;

		protected GComponent tabContent;
		protected GList listTab;
		protected BtnClose btnClose;
		public Common1TabFrame(GObject gObject,UIObject parent)
		{
			Init(gObject,parent);
			parent?.Components?.Add(this);
		}
		protected override void CreateChildren()
		{
			var gCom = ObjAs<GComponent>();
			tabContent = (GComponent)gCom.GetChildAt(2);
			listTab = (GList)gCom.GetChildAt(3);
			btnClose = new BtnClose(gCom.GetChildAt(4), this);
		}
	}
}
