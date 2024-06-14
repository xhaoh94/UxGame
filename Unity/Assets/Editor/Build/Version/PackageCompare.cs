using System.Collections.Generic;
using YooAsset;

namespace Ux.Editor.Build.Version
{
    public class PackageCompare
    {
        public static void CompareManifest(string path1, string path2, List<string> changeList)
        {
            changeList.Clear();

            // 加载补丁清单1
            byte[] bytesData1 = FileUtility.ReadAllBytes(path1);
            var manifest1 = ManifestTools.DeserializeFromBinary(bytesData1);

            // 加载补丁清单1
            byte[] bytesData2 = FileUtility.ReadAllBytes(path2);
            var manifest2 = ManifestTools.DeserializeFromBinary(bytesData2);

            // 拷贝文件列表
            foreach (var bundle2 in manifest2.BundleList)
            {
                if (manifest1.OutputNameStyle != manifest2.OutputNameStyle)
                {
                    changeList.Add(bundle2.FileName);
                    continue;
                }
                if (!manifest1.TryGetPackageBundleByBundleName(bundle2.BundleName, out var bundle1))
                {
                    changeList.Add(bundle2.FileName);
                    continue;
                }

                if (bundle2.FileHash != bundle1.FileHash)
                {
                    changeList.Add(bundle2.FileName);
                }
            }
        }
    }
}
