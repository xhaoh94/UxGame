using System.IO;
using UI.Editor;
using YooAsset.Editor;
public class CollectPreloadUI : IFilterRule
{
    public bool IsCollectAsset(FilterRuleData data)
    {
        var dirName = Path.GetDirectoryName(data.AssetPath);
        dirName = dirName.Replace('\\', '/');
        var buildins = UIClassifyWindow.ResClassifySettings.builtins;
        var path = UIClassifyWindow.ResClassifySettings.path;
        foreach (var buildin in buildins)
        {
            var dir = $"{path}/{buildin}";
            if (dirName == dir)
            {
                return false;
            }
        }

        var lazyloads = UIClassifyWindow.ResClassifySettings.lazyloads;
        foreach (var lazyload in lazyloads)
        {
            var dir = $"{path}/{lazyload.key}";
            if (dirName == dir)
            {
                return false;
            }
        }
        return true;
    }


}