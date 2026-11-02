using Ux;

namespace Ux.UI
{
    [UI]
    partial class BagWindow
    {
        // protected override UILayer Layer => UILayer.View;
        public override UIType Type => UIType.Normal;
        public override UIBlur Blur => UIBlur.Blur;

        protected override void OnShow()
        {
            Log.Info("xxxxxxxxxx333");
            base.OnShow();
        }

        protected override void OnHide()
        {
            base.OnHide();
        }
    }
}
