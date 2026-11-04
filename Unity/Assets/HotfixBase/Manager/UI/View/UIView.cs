namespace Ux
{
    public abstract class UIView : UIBase
    {        
        protected override UILayer Layer => UILayer.View;
    }
}
