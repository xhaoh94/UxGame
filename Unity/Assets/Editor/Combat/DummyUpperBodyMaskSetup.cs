using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Combat
{
    /// <summary>
    /// 给角色所有动作 Timeline 的动画轨挂一个上半身 AvatarMask，配成"边走边打"：
    /// Action 层只写上半身骨骼，下半身继续由 Base 层（Idle/Run）驱动。播放代码不用动 ——
    /// TLAnimationTrack 会把 mask 交给 AnimationLayerMixerPlayable，且 Action 层排序在 Base 之后
    /// （TLAnimationOutput.CompareTracks 按 PlaybackLayer 排），所以上半身能压住走动画。
    ///
    /// 逻辑侧前提：动作 movementPolicy = Allow。锁移动的动作起手时 Locomotion 被压成 Idle，
    /// Base 层放的是站立而不是走，遮罩没有意义 —— 这种动作工具只警告不改。
    /// </summary>
    internal static class DummyUpperBodyMaskSetup
    {
        private const string ProfilePath = "Assets/Data/Res/Combat/Role/Role_DummyCombatProfile.asset";
        private const string PrefabPath = "Assets/Data/Res/Prefab/Unit/Role/Role_Dummy.prefab";
        private const string MaskPath = "Assets/Data/Res/Timeline/Role/Role_DummyUpperBodyMask.asset";

        /// <summary>上半身起点：腰以上的全部骨骼都归遮罩管，腰以下留给 Locomotion。</summary>
        private const string UpperBodyRootName = "B-spine";

        [MenuItem("UxGame/工具/战斗/配置边走边攻击(上半身遮罩)", false, 525)]
        private static void Configure()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(ProfilePath);
            var animator = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)?.GetComponentInChildren<Animator>(true);
            if (profile == null || animator == null)
            {
                Debug.LogError($"[上半身遮罩] 读不到角色配置或预制：{ProfilePath} / {PrefabPath}");
                return;
            }

            var upperRoot = FindUpperBodyRoot(animator.transform);
            if (upperRoot == null)
            {
                Debug.LogError($"[上半身遮罩] 骨架里找不到上半身根骨骼 {UpperBodyRootName}，换名字后重跑本菜单。");
                return;
            }

            var mask = LoadOrCreateMask();
            // AvatarMask 没有 ClearTransformPaths，清空只能走 transformCount 的 setter。
            mask.transformCount = 0;
            // AddTransformPath 只有 Transform 重载；includeChildren 会连同全部子骨骼一起写入，
            // 路径由 Unity 自己算，等价于在 Inspector 里手勾这些骨骼。
            mask.AddTransformPath(upperRoot, true);
            var boneCount = mask.transformCount;

            var trackCount = 0;
            foreach (var presentation in profile.ActionPresentations)
            {
                var timeline = presentation?.Timeline;
                if (timeline == null)
                {
                    continue;
                }
                foreach (var track in timeline.tracks)
                {
                    if (track is not AnimationTrackAsset animation)
                    {
                        continue;
                    }
                    animation.avatarMask = mask;
                    animation.isAdditive = false;
                    trackCount++;
                }
                EditorUtility.SetDirty(timeline);
            }

            EditorUtility.SetDirty(mask);
            AssetDatabase.SaveAssets();
            Debug.Log($"[上半身遮罩] {upperRoot.name} 起共 {boneCount} 根骨骼，已挂到 {trackCount} 条动作动画轨（{MaskPath}）。");
            WarnLockedActions(profile);
        }

        private static AvatarMask LoadOrCreateMask()
        {
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (mask != null)
            {
                return mask;
            }
            mask = new AvatarMask();
            AssetDatabase.CreateAsset(mask, MaskPath);
            return mask;
        }

        private static Transform FindUpperBodyRoot(Transform animatorRoot)
        {
            foreach (var transform in animatorRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform != animatorRoot && transform.name == UpperBodyRootName)
                {
                    return transform;
                }
            }
            return null;
        }

        /// <summary>锁移动的动作改了也没用：起手期间下半身本来就是站立，只提示不改配置。</summary>
        private static void WarnLockedActions(CharacterCombatProfile profile)
        {
            foreach (var action in profile.Actions)
            {
                if (action != null && action.MovementPolicy == ActionMovementPolicy.Block)
                {
                    Debug.Log($"[上半身遮罩] 提示：动作 {action.name} 是 Block 移动，边走边打时它下半身会是站立。", action);
                }
            }
        }
    }
}