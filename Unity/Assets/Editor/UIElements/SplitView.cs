using UnityEngine.UIElements;
namespace Ux.Editor
{
    public class SplitView : TwoPaneSplitView
    {
        public new class UxmlFactory : UxmlFactory<SplitView, UxmlTraits> { }
        public SplitView() { }
    }

}
