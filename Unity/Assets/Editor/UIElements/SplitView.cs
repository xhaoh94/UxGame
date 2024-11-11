using UnityEngine.UIElements;
namespace Ux.Editor
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class SplitView : TwoPaneSplitView
    {
#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SplitView, UxmlTraits> { }
#endif
        public SplitView() { }
    }

}
