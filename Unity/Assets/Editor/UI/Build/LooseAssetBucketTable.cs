using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 散图分桶表：把 UI/Texture 下的图按「每桶最多 N 张」分配固定桶号，分配结果落盘。
    /// 已分配的桶号永不改变，新增图只会补进未满的桶或开新桶，所以老桶内容稳定，不会因为新增一张图就全量重更。
    /// </summary>
    public static class LooseAssetBucketTable
    {
        [Serializable]
        public class Entry
        {
            public string path;
            public int bucket;
        }

        [Serializable]
        public class Table
        {
            public int maxPerBucket = 200;
            public List<Entry> entries = new List<Entry>();
        }

        private const string TablePath = "Assets/Settings/Build/UI/LooseAssetBucketTable.json";

        public static string TextureRoot =>
            $"{UISpriteAtlasGenerator.Root}/{UIClassifySettingData.LooseTextureFolderName}";

        private static Table _cache;
        private static readonly Dictionary<string, int> Lookup = new Dictionary<string, int>();

        public static int GetBucket(string assetPath)
        {
            EnsureLoaded();
            return Lookup.TryGetValue(assetPath, out var bucket) ? bucket : -1;
        }

        /// <summary>扫描 Texture 目录：补登记新图、清掉已删除的条目。分配只增不改。</summary>
        public static bool Refresh()
        {
            _cache = null;
            Lookup.Clear();

            if (!Directory.Exists(TextureRoot)) return false;

            var table = Load() ?? new Table();
            var settings = UIClassifyWindow.ResClassifySettings;

            var files = new List<string>();
            foreach (var raw in Directory.GetFiles(TextureRoot, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(raw).ToLowerInvariant();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".tga") continue;

                var path = raw.Replace('\\', '/');

                // 指定了其它打包规则的子目录不进桶，由各自的收集器整包处理。
                if (settings != null && settings.IsUnderTextureRule(path)) continue;

                files.Add(path);
            }

            // 按路径排序后再补，保证首次生成的分配结果是确定的
            files.Sort(StringComparer.Ordinal);

            var alive = new HashSet<string>(files, StringComparer.Ordinal);
            var changed = table.entries.RemoveAll(e => e == null || string.IsNullOrEmpty(e.path) || !alive.Contains(e.path)) > 0;

            var assigned = new HashSet<string>(StringComparer.Ordinal);
            var counts = new Dictionary<int, int>();
            foreach (var entry in table.entries)
            {
                assigned.Add(entry.path);
                counts.TryGetValue(entry.bucket, out var count);
                counts[entry.bucket] = count + 1;
            }

            foreach (var file in files)
            {
                if (assigned.Contains(file)) continue;

                var bucket = FindBucket(counts, table.maxPerBucket);
                table.entries.Add(new Entry { path = file, bucket = bucket });
                counts.TryGetValue(bucket, out var count);
                counts[bucket] = count + 1;
                changed = true;
            }

            if (changed) Save(table);

            _cache = table;
            BuildLookup(table);
            return changed;
        }

        private static int FindBucket(Dictionary<int, int> counts, int maxPerBucket)
        {
            for (int i = 0; i < 1000; i++)
            {
                if (!counts.TryGetValue(i, out var count) || count < maxPerBucket) return i;
            }

            return 999;
        }

        private static void EnsureLoaded()
        {
            if (_cache != null) return;

            _cache = Load() ?? new Table();
            BuildLookup(_cache);
        }

        private static void BuildLookup(Table table)
        {
            Lookup.Clear();
            foreach (var entry in table.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.path)) continue;
                Lookup[entry.path] = entry.bucket;
            }
        }

        private static Table Load()
        {
            if (!File.Exists(TablePath)) return null;

            var text = File.ReadAllText(TablePath);
            if (string.IsNullOrEmpty(text)) return null;

            try
            {
                return JsonUtility.FromJson<Table>(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LooseBucket] 分桶表解析失败，将重新分配：{e.Message}");
                return null;
            }
        }

        private static void Save(Table table)
        {
            var dir = Path.GetDirectoryName(TablePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(TablePath, JsonUtility.ToJson(table, true));
            AssetDatabase.ImportAsset(TablePath);
        }
    }

    /// <summary>
    /// 散图打包：按分桶表把同一目录下的图拆进 bucket_XX，桶号来自 LooseAssetBucketTable，稳定不变。
    /// </summary>
    public class PackLooseAssetByBucket : IPackRule
    {
        public PackRuleResult GetPackRuleResult(PackRuleData data)
        {
            var bucket = LooseAssetBucketTable.GetBucket(data.AssetPath);
            if (bucket < 0)
            {
                // 新增的图还没登记：先落到 0 号桶，点一次「生成配置」（或走 Export）会补登记。
                bucket = 0;
                Debug.LogWarning($"[LooseBucket] 散图未登记，暂放 bucket_00：{data.AssetPath}");
            }

            var bundleName = $"{data.CollectPath}/bucket_{bucket:00}";
            return new PackRuleResult(bundleName, DefaultPackRule.AssetBundleFileExtension);
        }
    }
}
