using System.Collections.Generic;
using static Ux.UIMgr;

namespace Ux
{
    public interface IUIStackHandlerCallback
    {
        void ShowByStack(int id, IUIParam param);
        bool IsShowing(int id);
        IUI GetShowed(int id);
        bool IsInCreatedDels(int id);
        void AddToCreatedDels(int id);
    }

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
        List<UIStack> GetUIStacks();
        List<string> GetShowingUI();
        List<string> GetCacheUI();
        List<string> GetWaitDelUI();        
    }
}