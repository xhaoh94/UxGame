using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 图集自动维护：Atlas 目录里的 png 导入或移动后，延迟一帧确保该目录有同名图集并把 packables 绑到目录。
    /// 导入设置只在 OnPreprocessTexture（导入前）改，postprocess 里不再 SaveAndReimport，避免导入循环。
    /// </summary>
    public class UISpriteAtlasAutoGenerator : AssetPostprocessor
    {
        private static readonly string AtlasSegment = $"/{UISpriteAtlasGenerator.AtlasFolderName}/";

        private static readonly HashSet<string> DirtyFolders = new HashSet<string>();
        private static bool _scheduled;

        private void OnPreprocessTexture()
        {
            var path = assetImporter.assetPath.Replace('\\', '/');
            if (!IsAtlasSource(path)) return;

            var importer = assetImporter as TextureImporter;
            if (importer == null) return;

            // Multiple 且没有切片不产生任何 Sprite，图集收不到东西，这里统一按单图导入。
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            Collect(importedAssets);
            Collect(movedAssets);

            if (DirtyFolders.Count == 0 || _scheduled) return;

            _scheduled = true;
            EditorApplication.delayCall += Flush;
        }

        private static void Collect(string[] paths)
        {
            foreach (var raw in paths)
            {
                var path = raw.Replace('\\', '/');
                if (!IsAtlasSource(path)) continue;

                var dir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(dir)) continue;

                DirtyFolders.Add(dir.Replace('\\', '/'));
            }
        }

        /// <summary>一批导入只跑一次：这里可能触发图集资产的重导入，但不会命中 .png 过滤条件，不会递归。</summary>
        private static void Flush()
        {
            _scheduled = false;
            if (DirtyFolders.Count == 0) return;

            var folders = new List<string>(DirtyFolders);
            DirtyFolders.Clear();

            var changed = false;
            foreach (var folder in folders)
            {
                // 导入设置已由 OnPreprocessTexture 处理，这里不再扫目录。
                changed |= UISpriteAtlasGenerator.GenerateForFolder(folder, false);
            }

            if (changed) AssetDatabase.SaveAssets();
        }

        private static bool IsAtlasSource(string path)
        {
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return false;
            if (!path.StartsWith(UISpriteAtlasGenerator.Root + "/", StringComparison.Ordinal)) return false;
            return path.Contains(AtlasSegment);
        }
    }
}
