using System.IO;
using UI.Editor;
using YooAsset.Editor;

public class CollectBuiltinUI : IFilterRule
{
    public bool IsCollectAsset(FilterRuleData data)
    {
        if (UIClassifySettingData.IsProload(data.AssetPath)) return false;
        if (UIClassifySettingData.IsLazyload(data.AssetPath)) return false;
        return true;
    }
}