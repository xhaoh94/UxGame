using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    /// <summary>
    /// 关系测量的收敛结构：单 pass 推进 + 外层布局轮，而不是“内层多轮 × 外层多轮”的乘积。
    /// 四层链（A→B→C→D→E）必须在轮数上限内收敛到最终尺寸。
    /// </summary>
    public sealed class UIRelationMeasureExtensionTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Relation Measure Test", typeof(RectTransform));
            var rect = (RectTransform)_root.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(400f, 400f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void FourLevelChainConvergesWithinPassLimit()
        {
            var item = new ChainItem(_root.transform);
            item.Bind(0, null);   // 把最内层 E 的高度改成 80

            var extension = new UIRelationMeasureExtension();
            var context = new UIVirtualListMeasureContext(item, 0);
            Func<int, Vector2> next = _ =>
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(item.RectTransform);
                return item.RectTransform.rect.size;
            };

            var size = extension.Measure(context, next);

            // A→B→C→D→E 逐层跟随，最终五个子项都是 80，根节点按布局组汇总为 400。
            Assert.AreEqual(400f, size.y, 1f, "四层链必须在上限内收敛并被布局组汇总");
        }

        [Test]
        public void ApplyPassReturnsFalseOnceStable()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();
            relation.Add(b, RelationSide.Right, RelationSide.Left);

            b.anchoredPosition = new Vector2(300f, 0f);
            Assert.IsTrue(UIRelationDriver.ApplyPass(new[] { relation }), "目标移动后应产生写入");
            Assert.IsFalse(UIRelationDriver.ApplyPass(new[] { relation }), "已经稳定，第二轮应无变化");
        }

        /// <summary>
        /// Item 根挂 VerticalLayoutGroup + ContentSizeFitter，内部 A→B→C→D→E 是五个兄弟，
        /// 每个都把高度设为下一个的高度（合法：目标不是自己的子节点）。
        /// </summary>
        private sealed class ChainItem : IUIVirtualListItem
        {
            public GameObject GameObject { get; }
            public RectTransform RectTransform => (RectTransform)GameObject.transform;
            private readonly RectTransform _deepest;

            public ChainItem(Transform parent)
            {
                GameObject = new GameObject("Chain Item", typeof(RectTransform));
                GameObject.transform.SetParent(parent, false);
                var root = RectTransform;
                root.sizeDelta = new Vector2(300f, 40f);

                var group = GameObject.AddComponent<VerticalLayoutGroup>();
                // 尺寸交给关系控制，布局组只负责摆放和汇总高度。
                group.childControlHeight = false;
                group.childControlWidth = false;
                group.childForceExpandHeight = false;
                group.childForceExpandWidth = false;
                var fitter = GameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var a = CreateChild(root, "A", 40f);
                var b = CreateChild(root, "B", 40f);
                var c = CreateChild(root, "C", 40f);
                var d = CreateChild(root, "D", 40f);
                _deepest = CreateChild(root, "E", 40f);

                Link(a, b);
                Link(b, c);
                Link(c, d);
                Link(d, _deepest);
            }

            public void Bind(int index, object data)
            {
                // 模拟内容变化：最内层变高，外层要靠关系逐层跟随。
                _deepest.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80f);
            }

            public void OnRecycle() { }
            public void SetClickHandler(Action callback) { }

            public Vector2 Measure()
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
                return RectTransform.rect.size;
            }

            private static void Link(RectTransform self, RectTransform target)
            {
                var relation = self.gameObject.AddComponent<UIRelation>();
                relation.AddTarget(target);
                relation.SetFollowHeight(target, follow: true, usePercent: true, value: 1f);
                relation.Apply();
            }

            private static RectTransform CreateChild(Transform parent, string name, float height)
            {
                var go = new GameObject(name, typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(300f, height);
                rect.anchoredPosition = Vector2.zero;
                return rect;
            }
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }
    }
}
