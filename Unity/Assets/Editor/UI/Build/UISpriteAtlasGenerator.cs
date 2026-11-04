using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#if !UNITY_6000_0_OR_NEWER
using UnityEditor.U2D;
using UnityEngine.U2D;
#endif

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 把 UI 下每个包里的 Atlas 文件夹收进同名图集：UI/Bag/Atlas → Bag.spriteatlasv2。
    /// 图集和源 png 同目录是硬要求：Unity 只在同一个 bundle 内才会把 sprite 重映射到图集。
    /// </summary>
    public static class UISpriteAtlasGenerator
    {
        internal const string Root = "Assets/GameRes/Res/UI";
        internal const string AtlasFolderName = "Atlas";

        // Unity 6 起图集是 SpriteAtlasImporter 资产（.spriteatlasv2），更早版本是 .spriteatlas。
#if UNITY_6000_0_OR_NEWER
        private const string AtlasExtension = ".spriteatlasv2";
#else
        private const string AtlasExtension = ".spriteatlas";
#endif

        // [MenuItem("UxGame/工具/UI/生成 UI 图集", false, 512)]
        public static void GenerateAll()
        {
            if (!AssetDatabase.IsValidFolder(Root))
            {
                Debug.LogError($"[UISpriteAtlas] 找不到 UI 根目录：{Root}");
                return;
            }

            var dirs = Directory.GetDirectories(Root, AtlasFolderName, SearchOption.AllDirectories);
            int done = 0, skipped = 0;
            foreach (var rawDir in dirs)
            {
                if (GenerateForFolder(rawDir)) done++;
                else skipped++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UISpriteAtlas] 完成：处理 {done}，跳过 {skipped}。");
        }

        /// <summary>
        /// 处理单个 Atlas 目录：建/改名同名图集、绑定文件夹，可选修源图导入设置。
        /// 菜单全量生成和导入后的自动维护共用；不调 SaveAssets/Refresh，交给调用方。
        /// 导入后的调用要传 fixImportSettings:false —— OnPreprocessTexture 已经在导入前设好了，
        /// 这里再扫一遍等于每导一张图都 LoadAllAssetsAtPath 整个目录。
        /// </summary>
        public static bool GenerateForFolder(string rawDir, bool fixImportSettings = true)
        {
            var dir = rawDir.Replace('\\', '/');
            var packName = Path.GetFileName(Path.GetDirectoryName(dir));
            if (string.IsNullOrEmpty(packName)) return false;

            if (Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly).Length == 0)
            {
                return false;
            }

            if (fixImportSettings) EnsureSpriteImport(dir);

            var target = $"{dir}/{packName}{AtlasExtension}";
            if (!File.Exists(target))
            {
                var existing = FindExistingAtlas(dir);
                if (existing != null)
                {
                    // 已有图集但名字不规范：改名而不是重建，保住它的 GUID 和引用。
                    var error = AssetDatabase.MoveAsset(existing, target);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[UISpriteAtlas] 重命名失败 {existing} → {target}：{error}");
                        return false;
                    }

                    Debug.Log($"[UISpriteAtlas] 重命名 {existing} → {target}");
                }
                else if (!CreateAtlas(target))
                {
                    return false;
                }
            }

            var bound = BindFolder(target, dir);

            if (Directory.GetFiles(dir, "*" + AtlasExtension, SearchOption.TopDirectoryOnly).Length > 1)
            {
                Debug.LogWarning($"[UISpriteAtlas] 同一目录存在多个图集，只会绑定第一个：{dir}");
            }

            return bound;
        }

        private static string FindExistingAtlas(string dir)
        {
            var files = Directory.GetFiles(dir, "*" + AtlasExtension, SearchOption.TopDirectoryOnly);
            return files.Length > 0 ? files[0].Replace('\\', '/') : null;
        }

        /// <summary>
        /// 保证图集目录里的 png 都导入为 Sprite 单图：Multiple 模式且没有切片时不产生任何 Sprite，
        /// 图集收不到东西（Inspector 预览全空），而且不报错。
        /// 用子资产里有没有 Sprite 判断，避开已废弃的 TextureImporter.spritesheet。
        /// </summary>
        private static int EnsureSpriteImport(string dir)
        {
            int changed = 0;
            foreach (var file in Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly))
            {
                var assetPath = $"{dir}/{Path.GetFileName(file)}";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                bool hasSprite = false;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    if (asset is Sprite)
                    {
                        hasSprite = true;
                        break;
                    }
                }

                if (importer.textureType == TextureImporterType.Sprite && hasSprite) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                changed++;
            }

            return changed;
        }

        private static bool CreateAtlas(string path)
        {
#if UNITY_6000_0_OR_NEWER
            // v2 是导入器资产，没有公开的创建 API，只能写最小的 SpriteAtlasAsset 结构再让 Unity 导入。
            // packables 留空，随后由 BindFolder 直接改 YAML 绑定（SerializedObject 取不到那个字段）。
            File.WriteAllText(path, V2EmptyAsset);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
            {
                // 解析失败就删掉，避免留下一个 Unity 不认识的坏资产。
                File.Delete(path);
                AssetDatabase.Refresh();
                Debug.LogError($"[UISpriteAtlas] 创建失败（Unity 未能解析生成的 spriteatlasv2）：{path}");
                return false;
            }

            Debug.Log($"[UISpriteAtlas] 新建 {path}");
            return true;
#else
            var atlas = new SpriteAtlas();
            atlas.SetIncludeInBuild(true);
            AssetDatabase.CreateAsset(atlas, path);
            Debug.Log($"[UISpriteAtlas] 新建 {path}");
            return true;
#endif
        }

        private static bool BindFolder(string atlasPath, string folderPath)
        {
#if UNITY_6000_0_OR_NEWER
            // v2 的 packables 是 SpriteAtlasAsset 的原生字段，SerializedObject 拿不到属性路径
            // （FindProperty("m_ImporterData.packables") 恒为 null），只能直接改资产 YAML。
            // 绑文件夹而不是逐个 png：后续新增的 png 会自动进图集，不用重跑工具。
            var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
            if (string.IsNullOrEmpty(folderGuid))
            {
                Debug.LogError($"[UISpriteAtlas] 取不到文件夹 GUID：{folderPath}");
                return false;
            }

            var text = File.ReadAllText(atlasPath);
            var replaced = SetPackable(text, folderGuid);
            if (replaced == null)
            {
                Debug.LogError($"[UISpriteAtlas] 资产里找不到 packables 字段：{atlasPath}");
                return false;
            }

            if (replaced != text)
            {
                File.WriteAllText(atlasPath, replaced);
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);
            }

            // 导入会按导入结果重写文件，必须回读确认绑定没被丢掉。
            if (!File.ReadAllText(atlasPath).Contains(folderGuid))
            {
                Debug.LogError($"[UISpriteAtlas] 绑定未生效（导入后 packables 被重置）：{atlasPath}");
                return false;
            }

            return true;
#else
            var folder = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (folder == null)
            {
                Debug.LogError($"[UISpriteAtlas] 加载文件夹失败：{folderPath}");
                return false;
            }
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                return false;
            }

            var packables = atlas.GetPackables();
            for (int i = 0; i < packables.Length; i++)
            {
                atlas.Remove(packables[i]);
            }

            atlas.Add(folder);
            EditorUtility.SetDirty(atlas);
            return true;
#endif
        }

#if UNITY_6000_0_OR_NEWER
        /// <summary>
        /// 把 packables 换成「引用文件夹」单项。Unity 里文件夹资产的 fileID 固定为 102900000。
        /// 列表项与 packables 同缩进（Unity 的 YAML 风格），更深的行也算它的续行。
        /// </summary>
        private static string SetPackable(string text, string folderGuid)
        {
            var lines = text.Split('\n');
            int keyIndex = -1;
            int indent = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("packables:", System.StringComparison.Ordinal))
                {
                    keyIndex = i;
                    indent = line.Length - trimmed.Length;
                    break;
                }
            }

            if (keyIndex < 0) return null;

            int end = keyIndex + 1;
            while (end < lines.Length)
            {
                var line = lines[end].TrimEnd('\r');
                var trimmed = line.TrimStart();
                if (trimmed.Length == 0) break;

                int lineIndent = line.Length - trimmed.Length;
                bool isListItem = trimmed.StartsWith("- ", System.StringComparison.Ordinal) || trimmed == "-";
                if (lineIndent > indent || (lineIndent == indent && isListItem))
                {
                    end++;
                    continue;
                }

                break;
            }

            var pad = new string(' ', indent);
            var result = new List<string>(lines.Length + 1);
            for (int i = 0; i < keyIndex; i++) result.Add(lines[i]);
            result.Add($"{pad}packables:");
            result.Add($"{pad}- {{fileID: 102900000, guid: {folderGuid}, type: 3}}");
            for (int i = end; i < lines.Length; i++) result.Add(lines[i]);
            return string.Join("\n", result);
        }

        private const string V2EmptyAsset =
@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!612988286 &1
SpriteAtlasAsset:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: 
  serializedVersion: 2
  m_MasterAtlas: {fileID: 0}
  m_ImporterData:
    packables: []
  m_IsVariant: 0
";
#endif
    }
}
