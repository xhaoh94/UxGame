using Assets.Editor.Timeline;

namespace Ux.Editor.Timeline
{
    public partial class TimelineWindow
    {
        static UxUndo Undo { get; set; }
        static TimelineAsset Asset { get; set; }
        static System.Action SaveAssets { get; set; }

        public static TimelineEditorDocument Document { get; private set; }
        public static TimelineInspectorView InspectorContent { get; set; }
        public static UnityEngine.UIElements.VisualElement ClipContent { get; set; }
        public static System.Func<int, float> GetPositionByFrame { get; set; }
        public static System.Func<int> GetFrameByMousePosition { get; set; }
        public static System.Action<int> MarkerMove { get; set; }
        public static TimelineComponent Timeline { get; set; }
        public static System.Action<TimelineTrackAsset, UnityEngine.Object> RefreshBinds { get; set; }
        public static System.Action RefreshEntity { get; set; }
        public static System.Action RefreshView { get; set; }
        public static System.Action RefreshClip { get; set; }
        public static bool IsPlaying { get; set; }

        public static bool IsValid()
        {
            return Document?.CanEdit == true;
        }
    }
}
