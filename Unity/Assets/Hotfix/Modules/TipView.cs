using Ux;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Ux.UI
{
    [UI]
    partial class TipView
    {
        protected override UILayer Layer => UILayer.Top;
        protected override void OnLayout()
        {
            SetLayout(UILayout.Center_Top, true);
        }
    }
}