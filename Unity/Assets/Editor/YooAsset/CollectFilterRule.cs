using System;
using System.IO;
using Ux.Editor.Build.UI;
using YooAsset.Editor;

/// <summary>
/// 模块收集器：收模块下所有主资源（界面预制、散图、图集本体）。
/// 图集源图排除掉，交给全局的 CollectAtlasSource + StaticAssetCollector。
/// 用在「单独一条收集器」的模块上（lazyload）。
/// </summary>
public class CollectAssetUI : IFilterRule
{
    public string FindAssetType
    {
        get { return EAssetSearchType.All.ToString(); }
    }

    public bool IsCollectAsset(FilterRuleData data)
    {
        return CollectRuleUtil.IsUIAsset(data.AssetPath);
    }
}

/// <summary>归类为 builtin 的模块合并成一条收集器。</summary>
public class CollectBuiltinUI : IFilterRule
{
    public string FindAssetType
    {
        get { return EAssetSearchType.All.ToString(); }
    }

    public bool IsCollectAsset(FilterRuleData data)
    {
        var settings = UIClassifyWindow.ResClassifySettings;
        if (settings == null) return false;
        if (CollectRuleUtil.IsUIAsset(data.AssetPath) == false) return false;
        return settings.GetAssetTag(data.AssetPath) == settings.builtinTag;
    }
}

/// <summary>归类为 preload（含没配置的模块）合并成一条收集器。</summary>
public class CollectPreloadUI : IFilterRule
{
    public string FindAssetType
    {
        get { return EAssetSearchType.All.ToString(); }
    }

    public bool IsCollectAsset(FilterRuleData data)
    {
        var settings = UIClassifyWindow.ResClassifySettings;
        if (settings == null) return false;
        if (CollectRuleUtil.IsUIAsset(data.AssetPath) == false) return false;
        return settings.GetAssetTag(data.AssetPath) == settings.preloadTag;
    }
}

/// <summary>散图默认分桶：指定了其它打包规则的子目录排除掉，由各自的收集器接管。</summary>
public class CollectLooseTexture : IFilterRule
{
    public string FindAssetType
    {
        get { return EAssetSearchType.All.ToString(); }
    }

    public bool IsCollectAsset(FilterRuleData data)
    {
        var settings = UIClassifyWindow.ResClassifySettings;
        return settings == null || settings.IsUnderTextureRule(data.AssetPath) == false;
    }
}

/// <summary>图集源图：只收 Atlas 目录下的 png，配合 StaticAssetCollector 使用（不写清单、不产生地址）。</summary>
public class CollectAtlasSource : IFilterRule
{
    public string FindAssetType
    {
        get { return EAssetSearchType.All.ToString(); }
    }

    public bool IsCollectAsset(FilterRuleData data)
    {
        if (CollectRuleUtil.IsUnderAtlas(data.AssetPath) == false) return false;
        return string.Equals(Path.GetExtension(data.AssetPath), ".png", StringComparison.OrdinalIgnoreCase);
    }
}

internal static class CollectRuleUtil
{
    /// <summary>路径里是否包含 /Atlas/ 这一段目录。</summary>
    public static bool IsUnderAtlas(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return false;
        return assetPath.Replace('\\', '/').Contains("/Atlas/");
    }

    /// <summary>图片集本体文档（.spriteatlas / .spriteatlasv2）。</summary>
    public static bool IsAtlasAsset(string assetPath)
    {
        var ext = Path.GetExtension(assetPath);
        return string.Equals(ext, ".spriteatlasv2", StringComparison.OrdinalIgnoreCase)
               || string.Equals(ext, ".spriteatlas", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>UI 主资源：Atlas 目录里只要图集本体；散图目录（UI/Texture）有独立收集器。</summary>
    public static bool IsUIAsset(string assetPath)
    {
        if (IsUnderLooseTexture(assetPath)) return false;
        if (IsUnderAtlas(assetPath) == false) return true;
        return IsAtlasAsset(assetPath);
    }

    /// <summary>是否在 UI 根下的散图目录（UI/Texture）里。</summary>
    public static bool IsUnderLooseTexture(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return false;
        var root = $"{UISpriteAtlasGenerator.Root}/{UIClassifySettingData.LooseTextureFolderName}/";
        return assetPath.Replace('\\', '/').StartsWith(root, StringComparison.Ordinal);
    }
}
