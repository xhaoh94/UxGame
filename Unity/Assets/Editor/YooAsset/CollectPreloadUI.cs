using Ux.Editor.Build.UI;
using YooAsset.Editor;
public class CollectPreloadUI : IFilterRule
{
    public bool IsCollectAsset(FilterRuleData data)
    {
        return UIClassifySettingData.IsProload(data.AssetPath);
    }


}