using System.Collections.Generic;

namespace Ux
{
    public static class UIHandlerDebuggerExtensions
    {
        public static Dictionary<string, IUIData> GetAllUIData(this IUIMgrDebuggerAccess access)
        {
            var output = new Dictionary<string, IUIData>();
            access.FillAllUIData(output);
            return output;
        }

        public static List<string> GetShowedUI(this IUIMgrDebuggerAccess access)
        {
            var output = new List<string>();
            access.FillShowedUI(output);
            return output;
        }

        public static List<UIMgr.UIStack> GetUIStacks(this IUIMgrDebuggerAccess access)
        {
            var output = new List<UIMgr.UIStack>();
            access.FillUIStacks(output);
            return output;
        }

        public static List<string> GetShowingUI(this IUIMgrDebuggerAccess access)
        {
            var output = new List<string>();
            access.FillShowingUI(output);
            return output;
        }

        public static List<string> GetCacheUI(this IUIMgrDebuggerAccess access)
        {
            var output = new List<string>();
            access.FillCacheUI(output);
            return output;
        }

        public static List<string> GetWaitDelUI(this IUIMgrDebuggerAccess access)
        {
            var output = new List<string>();
            access.FillWaitDelUI(output);
            return output;
        }
    }
}
