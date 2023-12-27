//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Main","Common")]
	public partial class SceneView : UIView
	{
		protected override string PkgName => "Main";
		protected override string ResName => "SceneView";

		protected Btn1 btnMainView;
		protected Btn1 btnBack;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				btnMainView = new Btn1(gCom.GetChildAt(0), this);
				btnBack = new Btn1(gCom.GetChildAt(1), this);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
		protected override void OnAddEvent()
		{
			btnMainView.AddClick(_OnBtnMainViewClick);
			btnBack.AddClick(_OnBtnBackClick);
		}
		void _OnBtnMainViewClick(EventContext e)
		{
			OnBtnMainViewClick(e);
		}
		partial void OnBtnMainViewClick(EventContext e);
		void _OnBtnBackClick(EventContext e)
		{
			OnBtnBackClick(e);
		}
		partial void OnBtnBackClick(EventContext e);
	}
}
