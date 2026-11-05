using System.Collections.Generic;

namespace Ux
{
    public interface IUIMgrDebuggerAccess
    {
        void FillAllUIData(Dictionary<string, IUIData> output);
        void FillShowedUI(List<string> output);
        void FillUIStacks(List<UIMgr.UIStack> output);
        void FillShowingUI(List<string> output);
        void FillCacheUI(List<string> output);
        void FillWaitDelUI(List<string> output);
        void FillWindows(List<string> output);
    }
}
