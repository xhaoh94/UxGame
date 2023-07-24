//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	public partial class PatchBar : UIProgressBar
	{
		protected GGraph bar;
		public PatchBar(GObject gObject,UIObject parent)
		{
			Init(gObject,parent);
			parent?.Components?.Add(this);
		}
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				bar = (GGraph)gCom.GetChildAt(1);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
	}
}
