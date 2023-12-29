namespace Ux
{
    public abstract class UIView : UIBase
    {
        protected virtual UILayer Layer { get; } = UILayer.Root;        
        protected override void AddToStage()
        {
            if (GObject == null) return;
            UIMgr.Ins.GetLayer(Layer).AddChild(GObject);
        }

        protected override void OnLayout()
        {
            SetLayout(UILayout.Size);
        }
    }
}