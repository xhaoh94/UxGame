using Ux;

namespace Ux.UI
{
    [UI]
    partial class BagWindow
    {        
        public override UIBlurMode Blur => UIBlurMode.RendererFeature;

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
