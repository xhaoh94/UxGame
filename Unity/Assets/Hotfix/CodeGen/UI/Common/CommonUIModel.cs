//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	public partial class CommonUIModel : UIModel
	{
		protected override GGraph __container => container;
		protected GGraph container;
		public CommonUIModel(GObject gObject,UIObject parent): base(gObject, parent) { }
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				container = (GGraph)gCom.GetChildAt(1);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
	}
}
