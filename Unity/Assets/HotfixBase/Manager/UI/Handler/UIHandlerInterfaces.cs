using System.Collections.Generic;

namespace Ux
{
    public interface IUIBlurHandlerCallback
    {
        Dictionary<int, IUI> GetShowedDict();
    }

    public interface IUICacheHandlerCallback
    {
        void DisposeUI(IUI ui);
    }

    public interface IUIMgrDebuggerAccess
    {
        Dictionary<string, IUIData> GetAllUIData();
        List<string> GetShowedUI();
        List<UIMgr.UIStack> GetUIStacks();
        List<string> GetShowingUI();
        List<string> GetCacheUI();
        List<string> GetWaitDelUI();
    }
}
