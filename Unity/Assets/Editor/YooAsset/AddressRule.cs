using System;
using System.IO;
using Ux.Editor.Build.UI;
using YooAsset.Editor;
public class AddressTopDirectoryRule : IAddressRule
{
    public string GetAssetAddress(AddressRuleData data)
    {
        string assetPath = data.AssetPath.Replace(data.CollectPath, string.Empty);
        assetPath = assetPath.TrimStart('/');
        string[] splits = assetPath.Split('/');
        if (splits.Length > 0)
        {
            if (Path.HasExtension(splits[0]))
                throw new Exception($"Not found root directory : {assetPath}");
            string fileName = Path.GetFileNameWithoutExtension(data.AssetPath);
            return $"{splits[0]}_{fileName}";
        }
        else
        {
            throw new Exception($"Not found root directory : {assetPath}");
        }
    }
}
public class AddressCollectRule : IAddressRule
{
    public string GetAssetAddress(AddressRuleData data)
    {
        if (Directory.Exists(data.CollectPath))
        {
            var directory = Directory.CreateDirectory(data.CollectPath);
            string fileName = Path.GetFileNameWithoutExtension(data.AssetPath);
            return $"{directory.Name}_{fileName}";
        }
        else
        {
            throw new Exception($"Not found Collect directory : {data.CollectPath}");
        }
    }
}

/// <summary>
/// UI 主资源地址：图集本体用 Atlas_文件名，其余用 组名_文件名。
/// 分开前缀是为了避免同名的图集和界面预制撞地址（Bag.prefab / Bag.spriteatlasv2）。
/// </summary>
public class AddressByGroupOrAtlas : IAddressRule
{
    public string GetAssetAddress(AddressRuleData data)
    {
        string fileName = Path.GetFileNameWithoutExtension(data.AssetPath);
        string ext = Path.GetExtension(data.AssetPath);
        bool isAtlas = string.Equals(ext, ".spriteatlasv2", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(ext, ".spriteatlas", StringComparison.OrdinalIgnoreCase);

        return isAtlas ? $"Atlas_{fileName}" : $"{data.GroupName}_{fileName}";
    }
}