namespace Ux
{
    public abstract class UIView : UIBase
    {
        protected virtual UILayer Layer { get; } = UILayer.Normal;

        protected override void AddToStage()
        {
            if (GObject == null) return;
            UIMgr.Instance.GetLayer(Layer).AddChild(GObject);
        }

        protected override void OnLayout()
        {
            SetLayout(UILayout.Size);
        }
    }
}