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