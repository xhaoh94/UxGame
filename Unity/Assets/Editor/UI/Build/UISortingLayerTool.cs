using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 向 ProjectSettings/TagManager.asset 插入 UI 各层级专用的 SortingLayer。
    /// 幂等：已存在的跳过。插在 Default 之后，保证场景 Sprite（Default 层）画在所有 UI 之下。
    /// </summary>
    public static class UISortingLayerTool
    {
        // 与 UIStageHandler.SortingLayerNames 一一对应，顺序即跨层渲染顺序
        private static readonly string[] LayerNames =
        {
            "sl1",
            "sl2",
            "sl3",
            "sl4",
            "sl5",
            "sl6",
        };

        private const string TagManagerPath = "ProjectSettings/TagManager.asset";

        [MenuItem("UxGame/工具/UI/初始化 UI SortingLayer")]
        public static void Initialize()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"[UISortingLayer] 找不到 {TagManagerPath}");
                return;
            }

            var so = new SerializedObject(assets[0]);
            var layers = so.FindProperty("m_SortingLayers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogError("[UISortingLayer] TagManager 里找不到 m_SortingLayers，Unity 版本格式可能变了");
                return;
            }

            var existing = new HashSet<string>();
            var usedIds = new HashSet<int>();
            var defaultIndex = -1;
            for (int i = 0; i < layers.arraySize; i++)
            {
                var elem = layers.GetArrayElementAtIndex(i);
                var name = elem.FindPropertyRelative("name").stringValue;
                existing.Add(name);
                var id = elem.FindPropertyRelative("uniqueID").intValue;
                if (id != 0) usedIds.Add(id);
                if (name == "Default") defaultIndex = i;
            }

            if (defaultIndex < 0)
            {
                Debug.LogError("[UISortingLayer] 找不到 Default 层，放弃插入");
                return;
            }

            // uniqueID 为 0 的条目和 Default 撞 ID，Unity 会把它当成 Default，整个层名解析失效。
            var repaired = 0;
            for (int i = 0; i < layers.arraySize; i++)
            {
                var elem = layers.GetArrayElementAtIndex(i);
                var name = elem.FindPropertyRelative("name").stringValue;
                if (string.IsNullOrEmpty(name) || name == "Default") continue;

                var idProp = elem.FindPropertyRelative("uniqueID");
                if (idProp.intValue != 0) continue;

                idProp.intValue = NextId(name, usedIds);
                repaired++;
            }

            var insertAt = defaultIndex + 1;
            var added = 0;
            foreach (var name in LayerNames)
            {
                if (existing.Contains(name))
                {
                    // 已存在的层也要保证相对顺序，跳过但继续占位
                    for (int i = 0; i < layers.arraySize; i++)
                    {
                        if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                        {
                            insertAt = i + 1;
                            break;
                        }
                    }

                    continue;
                }

                layers.InsertArrayElementAtIndex(insertAt);
                var elem = layers.GetArrayElementAtIndex(insertAt);
                elem.FindPropertyRelative("name").stringValue = name;
                // 不能用 string.GetHashCode()：.NET Core 下每进程随机，每次重启 ID 都变。
                elem.FindPropertyRelative("uniqueID").intValue = NextId(name, usedIds);
                existing.Add(name);
                insertAt++;
                added++;
            }

            if (added == 0 && repaired == 0)
            {
                Debug.Log("[UISortingLayer] 六个 UI 层都已存在，无需修改");
            }
            else
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(assets[0]);
                AssetDatabase.SaveAssets();
                Debug.Log($"[UISortingLayer] 已插入 {added} 个 SortingLayer，修复 {repaired} 个无效 ID，可到 Project Settings → Tags and Layers 核对顺序");
            }

            VerifySortingLayers(layers);
            LogEffectiveLayers();
            EnsureObjectLayer(assets[0], so);
        }

        /// <summary>打印 Unity 当前真正生效的层列表。改完 TagManager 可能需要重载编辑器才会刷新。</summary>
        private static void LogEffectiveLayers()
        {
            var sb = new System.Text.StringBuilder();
            var all = SortingLayer.layers;
            for (int i = 0; i < all.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(all[i].name).Append('#').Append(all[i].id);
            }

            Debug.Log($"[UISortingLayer] Unity 当前生效 {all.Length} 层：{sb}");
        }

        /// <summary>稳定且不重复的 uniqueID。ID 只要非 0 且唯一即可，Unity 不校验它与名字的对应关系。</summary>
        private static int NextId(string name, HashSet<int> used)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < name.Length; i++)
                {
                    hash ^= name[i];
                    hash *= 16777619;
                }

                var id = (int)(hash & 0x7FFFFFFF);
                if (id == 0) id = 1;
                while (!used.Add(id)) id++;
                return id;
            }
        }

        /// <summary>保存后回读一遍：有重名或 0 ID 就说明层表已经坏了，Unity 会把层名解析成 Default。</summary>
        private static void VerifySortingLayers(SerializedProperty layers)
        {
            var names = new HashSet<string>();
            var ids = new HashSet<int>();
            for (int i = 0; i < layers.arraySize; i++)
            {
                var elem = layers.GetArrayElementAtIndex(i);
                var name = elem.FindPropertyRelative("name").stringValue;
                var id = elem.FindPropertyRelative("uniqueID").intValue;

                if (!names.Add(name))
                    Debug.LogError($"[UISortingLayer] Sorting Layer 重名：{name}");
                if (name != "Default" && id == 0)
                    Debug.LogError($"[UISortingLayer] Sorting Layer [{name}] 的 uniqueID 仍是 0，层名会解析成 Default");
                else if (!ids.Add(id))
                    Debug.LogError($"[UISortingLayer] Sorting Layer [{name}] 的 uniqueID 与其它层重复：{id}");
            }
        }

        /// <summary>给直挂模型的专用 layer（UI 相机渲染、场景相机默认不渲染，避免双渲染）。</summary>
        private static void EnsureObjectLayer(Object tagManager, SerializedObject so)
        {
            var layers = so.FindProperty("layers");
            if (layers == null || !layers.isArray || layers.arraySize < 32)
            {
                Debug.LogError("[UISortingLayer] TagManager layers 数组异常，跳过 UIModel object layer");
                return;
            }

            const string layerName = "UIModel";
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    Debug.Log($"[UISortingLayer] object layer 已存在：{layerName}(索引 {i})");
                    return;
                }
            }

            // 8 之后第一个空位（11 起，避开 RTModel=10）
            for (int i = 8; i < 31; i++)
            {
                if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;

                layers.GetArrayElementAtIndex(i).stringValue = layerName;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tagManager);
                AssetDatabase.SaveAssets();
                Debug.Log($"[UISortingLayer] 已添加 object layer：{layerName}(索引 {i})");
                return;
            }

            Debug.LogError("[UISortingLayer] 没有空闲 layer 槽位，无法添加 UIModel");
        }
    }
}
