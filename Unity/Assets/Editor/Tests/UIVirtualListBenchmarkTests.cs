using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Ux.Editor.Tests
{
    /// <summary>
    /// 性能基准（手动运行，不进常规测试）：用来决定“增量总长/脏水位”和“已测尺寸复用”的优先级。
    ///
    /// 跑法：Test Runner → EditMode → 选中本类 → Run（本类标了 Explicit，不会被批量跑）。
    /// 输出一条 [Benchmark] 报告，末尾直接给出建议。
    ///
    /// 说明：EditMode 没有 Canvas 渲染，测不到 `Canvas.BuildBatch`，所以这里只测
    /// “列表自身算术 + 强制布局重建”两部分——正好对应待决策的两个改动。
    /// </summary>
    [Explicit("性能基准，手动运行")]
    [Category("Benchmark")]
    public sealed class UIVirtualListBenchmarkTests
    {
        private const int WarmupScrolls = 10;
        private const int MeasuredScrolls = 100;
        private const int MeasureIterations = 300;

        [Test]
        public void ReportScrollAndMeasureCost()
        {
            var report = new StringBuilder();
            report.AppendLine("===== [Benchmark] 虚拟列表成本 =====");

            // 1) 布局算术 + 尺寸复用：固定 vs 可变，随数据量变化
            var sizes = new[] { 1000, 10000, 100000 };
            foreach (int count in sizes)
            {
                var withReuse = MeasureScroll(count, reuseMeasuredSize: true);
                var withoutReuse = MeasureScroll(count, reuseMeasuredSize: false);
                report.AppendLine(
                    $"数据 {count,7} | 复用开 滚动 {withReuse.PerScrollMs,7:F3} ms/次 Measure {withReuse.Measures,6} 次" +
                    $" | 复用关 滚动 {withoutReuse.PerScrollMs,7:F3} ms/次 Measure {withoutReuse.Measures,6} 次");
            }

            // 2) 测量成本：item 内部有/无 LayoutGroup
            var lazy = MeasureItemCost(withLayoutGroup: true);
            var plain = MeasureItemCost(withLayoutGroup: false);
            report.AppendLine(
                $"Measure：有 LayoutGroup {lazy:F4} ms/次 | 无布局组件 {plain:F4} ms/次 | 倍率 {lazy / Math.Max(plain, 1e-6):F2}x");

            // 3) 决策
            report.AppendLine("----- 结论 -----");
            var small = MeasureScroll(1000, reuseMeasuredSize: true);
            var large = MeasureScroll(100000, reuseMeasuredSize: true);
            double growth = large.PerScrollMs / Math.Max(small.PerScrollMs, 1e-6);
            report.AppendLine($"滚动成本 1k → 100k 增长 {growth:F2}x（线性增长说明位置重算是主成本）");
            if (growth >= 5.0)
                report.AppendLine("→ 先做【增量总长 + 脏水位】：滚动成本随数据量线性上升。");
            else if (growth >= 2.0)
                report.AppendLine("→ 增量总长仍建议做（长期上限），但已不是决定性瓶颈。");
            else
                report.AppendLine("→ 列表算术不是瓶颈（脏水位/增量总长已生效）。");

            var reuseOff = MeasureScroll(100000, reuseMeasuredSize: false);
            double measureRatio = reuseOff.Measures / Math.Max((double)large.Measures, 1.0);
            report.AppendLine($"100k 滚动：复用开 Measure {large.Measures} 次，复用关 {reuseOff.Measures} 次（{measureRatio:F1}x）");
            if (measureRatio >= 5.0)
                report.AppendLine("→ 【已测尺寸复用】生效：滑动时不重复测量。");
            else
                report.AppendLine("→ 注意：复用未明显减少测量，检查是否每次都在重新绑定。");

            if (lazy / Math.Max(plain, 1e-6) >= 2.0)
                report.AppendLine("→ 【已测尺寸复用】收益大：item 内部有布局组件，每次 bind 的重测成本明显，值得做 key 化缓存。");
            else
                report.AppendLine("→ 【已测尺寸复用】收益有限：item 内部基本是手工布局，重测本身很便宜。");

            Debug.Log(report.ToString());
            // 也写一份到文件：batchmode 跑测试时 Debug.Log 不进结果 XML，只能从编辑器日志里找。
            try
            {
                var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Temp", "benchmark-report.txt"));
                System.IO.File.WriteAllText(path, report.ToString());
                Debug.Log($"[Benchmark] 报告已写入 {path}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Benchmark] 报告写入失败：{e.Message}");
            }
        }

        private readonly struct ScrollResult
        {
            public readonly double SetDatasMs;
            public readonly double PerScrollMs;
            public readonly int CreatedFirstFrame;
            public readonly int Measures;
            public ScrollResult(double setDatasMs, double perScrollMs, int createdFirstFrame, int measures)
            {
                SetDatasMs = setDatasMs;
                PerScrollMs = perScrollMs;
                CreatedFirstFrame = createdFirstFrame;
                Measures = measures;
            }
        }

        private static ScrollResult MeasureScroll(int count, bool reuseMeasuredSize)
        {
            var root = new GameObject($"Bench {count}", typeof(RectTransform), typeof(ScrollRect));
            try
            {
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(root.transform, false);
                ((RectTransform)viewport.transform).sizeDelta = new Vector2(400f, 800f);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);

                var scroll = root.GetComponent<ScrollRect>();
                scroll.viewport = (RectTransform)viewport.transform;
                scroll.content = (RectTransform)content.transform;

                // 可变主轴尺寸：每次 SetItemSize 都会让位置缓存变脏，才能真实测到 EnsurePositions。
                var layout = new UIFlowLayout(
                    UIVirtualListAxis.Vertical,
                    itemSize: new Vector2(400f, 80f),
                    variableSize: true);
                var factory = new BenchFactory();
                var list = new UIVirtualList(scroll, layout, factory) { ReuseMeasuredSize = reuseMeasuredSize };
                try
                {
                    var datas = new object[count];
                    for (int i = 0; i < count; i++) datas[i] = i;

                    var watch = Stopwatch.StartNew();
                    list.SetDatas(datas);
                    watch.Stop();
                    double setDatasMs = watch.Elapsed.TotalMilliseconds;
                    int created = factory.Created;

                    // 预热：把对象池填满，排除首次创建成本。
                    for (int i = 0; i < WarmupScrolls; i++)
                        list.ScrollToView((int)(count * 0.5f) + i * 3, UIVirtualListAlignment.Start);

                    watch.Restart();
                    int measuresBefore = factory.Measures;
                    for (int i = 0; i < MeasuredScrolls; i++)
                    {
                        int index = (int)(count * 0.5f) + (i % 20) * 7;
                        list.ScrollToView(Mathf.Clamp(index, 0, count - 1), UIVirtualListAlignment.Start);
                    }
                    watch.Stop();
                    return new ScrollResult(setDatasMs, watch.Elapsed.TotalMilliseconds / MeasuredScrolls,
                        created, factory.Measures - measuresBefore);
                }
                finally { list.Dispose(); }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static double MeasureItemCost(bool withLayoutGroup)
        {
            var root = new GameObject("Measure Bench", typeof(RectTransform));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = new Vector2(400f, 100f);

                LayoutElement probe = null;
                if (withLayoutGroup)
                {
                    var group = root.AddComponent<VerticalLayoutGroup>();
                    group.childControlHeight = true;
                    group.childForceExpandHeight = false;
                    var fitter = root.AddComponent<ContentSizeFitter>();
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    for (int i = 0; i < 4; i++)
                    {
                        var child = new GameObject($"Row{i}", typeof(RectTransform));
                        child.transform.SetParent(root.transform, false);
                        var element = child.AddComponent<LayoutElement>();
                        element.preferredHeight = 20f + i;
                        if (i == 0) probe = element;
                    }
                }

                // 不测首次（含组件初始化），直接进入循环。
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

                var watch = Stopwatch.StartNew();
                for (int i = 0; i < MeasureIterations; i++)
                {
                    // 改一次内容，让布局真的变脏；否则 LayoutRebuilder 会提前返回，测不到重建成本。
                    if (probe != null) probe.preferredHeight = 20f + (i % 7);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    // 读一次尺寸，模拟 Measure 的收尾。
                    var unused = rect.rect.size;
                    if (unused.x < 0f) throw new InvalidOperationException();
                }
                watch.Stop();
                return watch.Elapsed.TotalMilliseconds / MeasureIterations;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private sealed class BenchFactory : IUIVirtualListItemFactory
        {
            public int Created { get; private set; }
            public int Measures { get; private set; }
            public int GetItemType(int index, object data) => 0;

            public IUIVirtualListItem Create(int itemType, Transform parent)
            {
                Created++;
                var go = new GameObject("Bench Item", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return new BenchItem(go, this);
            }

            public void Destroy(IUIVirtualListItem item)
            {
                if (item.GameObject != null) UnityEngine.Object.DestroyImmediate(item.GameObject);
            }

            public void ReportMeasure() => Measures++;
        }

        private sealed class BenchItem : IUIVirtualListItem
        {
            private readonly BenchFactory _owner;
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;

            public BenchItem(GameObject gameObject, BenchFactory owner)
            {
                GameObject = gameObject;
                _owner = owner;
            }

            public void Bind(int index, object data) { }
            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }

            public Vector2 Measure()
            {
                _owner.ReportMeasure();
                LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
                return RectTransform.rect.size;
            }
        }
    }
}
