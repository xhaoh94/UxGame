using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.UI
{
    public partial class CommonDialog
    {
        protected override TMP_Text __txtTitle => txtTitle;
        protected override TMP_Text __txtContent => txtContent;
        protected override Toggle __checkbox => checkbox;
        protected override GameObject __btn1 => btn1;
        protected override GameObject __btn2 => btn2;
        protected override GameObject __btnClose => btnClose;
    }
}

