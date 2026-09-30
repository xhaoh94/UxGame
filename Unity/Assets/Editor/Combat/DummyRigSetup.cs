using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Combat
{
    /// <summary>
    /// 把 Dummy 目录下的 Kevin Iglesias 模型/动画 FBX 批量配置成 **Generic** rig。
    ///
    /// 为什么必须是 Generic：本项目用 AnimationClipPlayable + AnimationMixerPlayable 直接驱动 Animator
    /// （没有 AnimatorController）。Generic 剪辑的曲线按骨骼路径直接写进层级，这条路可用；
    /// Humanoid 剪辑是 muscle 空间数据，需要 Mecanim 的 retargeting 管线才会落到骨骼上，
    /// 裸 Playables 不做这一步 —— 全部动画都会错乱（这就是把 Kevin 资源导成 Humanoid 时踩的坑）。
    /// 旧的 Hero_ZS 资源（能正常播）就是 animationType: 2 (Generic) + 模型 avatarSetup: 0。
    ///
    /// 只处理角色骨骼：道具/武器/法术 mesh（Human_ 开头、无 @ 的那些）会被跳过。
    /// </summary>
    internal static class DummyRigSetup
    {
        private const string DummyRoot = "Assets/Data/Art/Model/Unit/Dummy";

        /// <summary>循环动作的判定：站立/位移/社交类都是循环，攻击/受击/死亡/施法必须一次性播完。</summary>
        private static readonly string[] LoopKeywords = { "Idle", "Run", "Walk", "Sprint", "Strafe", "Dance" };

        [MenuItem("UxGame/工具/战斗/配置 Dummy 动画导入(Rig=Generic)", false, 524)]
        private static void Configure()
        {
            var paths = AssetDatabase.FindAssets("t:Model", new[] { DummyRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .Where(IsCharacterAsset)
                .ToArray();

            var configured = 0;
            try
            {
                for (var i = 0; i < paths.Length; i++)
                {
                    var path = paths[i];
                    EditorUtility.DisplayProgressBar("配置 Rig", path, i / (float)Math.Max(1, paths.Length));

                    if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                    {
                        continue;
                    }

                    var changed = importer.animationType != ModelImporterAnimationType.Generic;
                    importer.animationType = ModelImporterAnimationType.Generic;

                    var stem = Path.GetFileNameWithoutExtension(path);
                    // 动画 FBX 不需要 Avatar；模型 FBX 必须保留自动生成的 Generic Avatar，
                    // 预制 Animator 引用的是它（fileID 9000000），置 NoAvatar 会让该引用悬空。
                    if (stem.Contains("@") && importer.avatarSetup != ModelImporterAvatarSetup.NoAvatar)
                    {
                        importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                        changed = true;
                    }

                    if (ApplyLoop(importer, LoopKeywords.Any(keyword => stem.Contains(keyword))))
                    {
                        changed = true;
                    }

                    if (!changed)
                    {
                        continue;
                    }

                    importer.SaveAndReimport();
                    configured++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[Rig] 已配置 {configured} / {paths.Length} 个 FBX 为 Generic（循环关键字: {string.Join("/", LoopKeywords)}）");
        }

        /// <summary>按动作类型设置循环标记，返回是否发生了变化。</summary>
        private static bool ApplyLoop(ModelImporter importer, bool loop)
        {
            var settings = importer.defaultClipAnimations;
            if (settings == null || settings.Length == 0 || settings[0].loopTime == loop)
            {
                return false;
            }

            for (var i = 0; i < settings.Length; i++)
            {
                settings[i].loopTime = loop;
            }
            importer.clipAnimations = settings;
            return true;
        }

        /// <summary>
        /// 角色骨骼资源的命名特征：Kevin 的动画一律 `HumanX@动作`，模型是 `*_Model` 或 `*Dummy*`。
        /// 武器/道具/法术 mesh 叫 `Human_XXX`，不含 @，会被排除。
        /// </summary>
        private static bool IsCharacterAsset(string path)
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            return stem.Contains("@") ||
                   stem.Contains("_Model") ||
                   stem.Contains("Dummy", StringComparison.OrdinalIgnoreCase);
        }
    }
}
