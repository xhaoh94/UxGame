using System.Collections.Generic;

namespace Ux
{
    /// <summary>
    /// UIMgr 对界面/处理器开放的回调面。
    /// 接口本身 public 是因为 IUI.InitData 是 public 签名；但 UIMgr 用显式实现，
    /// 这些方法不出现在 UIMgr 的公开 API 上，业务想调必须显式转型。
    /// </summary>
    public interface IUICallback
    {
        void NotifyShown(IUI ui, IUIParam param, bool checkStack);
        void NotifyHidden(IUI ui);
        void RegisterStage(int instanceId, UILayer layer);
        void UnregisterStage(int instanceId, UILayer layer);
        bool TryGetStageSorting(int instanceId, out string sortingLayer, out int order);
        Dictionary<int, IUI> GetShowedDict();
    }

    public interface IUICacheHandlerCallback
    {
        void DisposeUI(IUI ui);
    }

    public interface IUIMgrDebuggerAccess
    {
        void FillAllUIData(Dictionary<string, IUIData> output);
        void FillShowedUI(List<string> output);
        void FillUIStacks(List<UIMgr.UIStack> output);
        void FillShowingUI(List<string> output);
        void FillCacheUI(List<string> output);
        void FillWaitDelUI(List<string> output);
    }

    public interface IUIFocusHandlerCallback
    {
        Dictionary<int, IUI> GetShowedDict();
        IUI GetShownUI(int id);
        bool IsVisible(int id);
    }
}
