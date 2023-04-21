using System.IO;
using UnityEngine;
using UnityEditor;

namespace YooAsset.Editor
{
    public class PackageImportWindow : EditorWindow
    {
        static PackageImportWindow _thisInstance;

        [MenuItem("YooAsset/补丁包导入工具", false, 301)]
        static void ShowWindow()
        {
            if (_thisInstance == null)
            {
                _thisInstance = EditorWindow.GetWindow(typeof(PackageImportWindow), false, "补丁包导入工具", true) as PackageImportWindow;
                _thisInstance.minSize = new Vector2(800, 600);
            }
            _thisInstance.Show();
        }

        private string _packageManifestPath = string.Empty;

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("选择补丁包", GUILayout.MaxWidth(150)))
            {
                string resultPath = EditorUtility.OpenFilePanel("Find", "Assets/", "bytes");
                if (string.IsNullOrEmpty(resultPath))
                    return;
                _packageManifestPath = resultPath;
            }
            EditorGUILayout.LabelField(_packageManifestPath);
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(_packageManifestPath) == false)
            {
                if (GUILayout.Button("导入补丁包（全部文件）", GUILayout.MaxWidth(150)))
                {
                    AssetBundleBuilderHelper.ClearStreamingAssetsFolder();
                    CopyPackageManifestFiles(_packageManifestPath);
                }
            }
        }

        private void CopyPackageManifestFiles(string packageManifestFilePath)
        {
            string manifestFileName = Path.GetFileNameWithoutExtension(packageManifestFilePath);
            string outputDirectory = Path.GetDirectoryName(packageManifestFilePath);

            // 加载补丁清单
            byte[] bytesData = FileUtility.ReadAllBytes(packageManifestFilePath);
            var manifest = ManifestTools.DeserializeFromBinary(bytesData);

            // 拷贝核心文件
            {
                string sourcePath = $"{outputDirectory}/{manifestFileName}.bytes";
                string destPath = $"{AssetBundleBuilderHelper.GetStreamingAssetsFolderPath()}/{manifestFileName}.bytes";
                EditorTools.CopyFile(sourcePath, destPath, true);
            }
            {
                string sourcePath = $"{outputDirectory}/{manifestFileName}.hash";
                string destPath = $"{AssetBundleBuilderHelper.GetStreamingAssetsFolderPath()}/{manifestFileName}.hash";
                EditorTools.CopyFile(sourcePath, destPath, true);
            }
            {
                string fileName = YooAssetSettingsData.GetPackageVersionFileName(manifest.PackageName);
                string sourcePath = $"{outputDirectory}/{fileName}";
                string destPath = $"{AssetBundleBuilderHelper.GetStreamingAssetsFolderPath()}/{fileName}";
                EditorTools.CopyFile(sourcePath, destPath, true);
            }

            // 拷贝文件列表
            int fileCount = 0;
            foreach (var bundle in manifest.BundleList)
            {
                fileCount++;
                string sourcePath = $"{outputDirectory}/{bundle.FileName}";
                string destPath = $"{AssetBundleBuilderHelper.GetStreamingAssetsFolderPath()}/{bundle.FileName}";
                EditorTools.CopyFile(sourcePath, destPath, true);
            }

            Debug.Log($"补丁包拷贝完成，一共拷贝了{fileCount}个资源文件");
            AssetDatabase.Refresh();
        }
    }
}