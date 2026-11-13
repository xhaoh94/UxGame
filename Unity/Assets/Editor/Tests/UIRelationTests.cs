using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    /// <summary>
    /// 关联是“保持距离”而不是“对齐”：勾选时记录当前位置，之后目标变化时距离不变。
    /// </summary>
    public sealed class UIRelationTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("RelationTestRoot", typeof(RectTransform));
            var rect = (RectTransform)_root.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(2000f, 2000f);
            rect.anchoredPosition = Vector2.zero;
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void TickingLinkRecordsCurrentDistanceAndKeepsObjectStill()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Right, RelationSide.Left);
            relation.Apply();

            // 勾选不改变当前位置，只是记录“右边距目标左边 -100”。
            Assert.AreEqual(0f, a.position.x, 0.01f);
            Assert.IsTrue(relation.Has(b, RelationSide.Right, RelationSide.Left));
            var item = relation.Items[0];
            Assert.AreEqual(-100f, item.Links[0].Offset, 0.01f);
        }

        [Test]
        public void TargetMoveKeepsRecordedDistance()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Right, RelationSide.Left);
            relation.Apply();

            b.anchoredPosition = new Vector2(300f, 0f);
            Assert.IsTrue(relation.ApplyIfNeeded(), "目标移动后应重算");
            Assert.AreEqual(200f, a.position.x + a.rect.width, 0.01f);
        }

        [Test]
        public void LeftToLeftKeepsLeftGap()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Left, RelationSide.Left);
            relation.Apply();

            b.anchoredPosition = new Vector2(400f, 0f);
            relation.Apply();

            // 左边距保持 -200：目标左 400 → 自身左 200。
            Assert.AreEqual(200f, a.position.x, 0.01f);
        }

        [Test]
        public void BothHorizontalSidesStretchWithTarget()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Left, RelationSide.Left);
            relation.Add(b, RelationSide.Right, RelationSide.Right);
            relation.Apply();
            Assert.AreEqual(100f, a.rect.width, 0.01f);

            b.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
            relation.Apply();

            Assert.AreEqual(300f, a.rect.width, 0.01f);
            Assert.AreEqual(0f, a.position.x, 0.01f);
        }

        [Test]
        public void CenterToCenterKeepsOffset()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Center, RelationSide.Center);
            relation.Apply();

            b.anchoredPosition = new Vector2(400f, 0f);
            relation.Apply();

            Assert.AreEqual(250f, a.position.x + a.rect.width * 0.5f, 0.01f);
        }

        [Test]
        public void CrossHierarchyKeepsWorldDistance()
        {
            var c1 = CreateRect(_root.transform, "C1", new Vector2(400f, 400f), new Vector2(50f, -20f));
            var c2 = CreateRect(_root.transform, "C2", new Vector2(400f, 400f), new Vector2(500f, -20f));
            var a = CreateRect(c1, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(c2, "B", new Vector2(100f, 50f), Vector2.zero);
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Right, RelationSide.Left);
            relation.Apply();

            b.anchoredPosition = new Vector2(100f, 0f);
            relation.Apply();

            var aRight = a.TransformPoint(new Vector3(a.rect.xMax, 0f, 0f)).x;
            var bLeft = b.TransformPoint(new Vector3(b.rect.xMin, 0f, 0f)).x;
            Assert.AreEqual(bLeft - 350f, aRight, 0.01f);
        }

        [Test]
        public void ParentScaleIsRespected()
        {
            var c1 = CreateRect(_root.transform, "C1", new Vector2(400f, 400f), new Vector2(50f, -20f));
            c1.localScale = new Vector3(2f, 2f, 1f);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(600f, 0f));
            var a = CreateRect(c1, "A", new Vector2(100f, 50f), Vector2.zero);
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Right, RelationSide.Left);
            relation.Apply();

            b.anchoredPosition = new Vector2(700f, 0f);
            relation.Apply();

            var aRight = a.TransformPoint(new Vector3(a.rect.xMax, 0f, 0f)).x;
            Assert.AreEqual(700f - 350f, aRight, 0.01f);
        }

        [Test]
        public void FollowWidthKeepsRecordedRatio()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 80f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Left, RelationSide.Left);
            relation.SetFollowWidth(b, true, true);
            relation.Apply();

            // 勾选“宽 -> 宽”只记录比例 100 / 300，不改当前尺寸。
            Assert.AreEqual(100f, a.rect.width, 0.01f);
            Assert.AreEqual(1f / 3f, relation.Items[0].WidthRatio, 0.001f);

            b.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
            relation.Apply();

            Assert.AreEqual(200f, a.rect.width, 0.01f);
            Assert.AreEqual(50f, a.rect.height, 0.01f);
        }

        [Test]
        public void FollowWidthAcceptsManualRatio()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 80f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Left, RelationSide.Left);
            relation.SetFollowWidth(b, true, true, 0.5f);
            relation.Apply();

            Assert.AreEqual(150f, a.rect.width, 0.01f);
        }

        [Test]
        public void FollowWidthAbsoluteModeKeepsDelta()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 80f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Left, RelationSide.Left);
            relation.SetFollowWidth(b, true, false);
            relation.Apply();

            Assert.AreEqual(100f, a.rect.width, 0.01f);
            Assert.AreEqual(-200f, relation.Items[0].WidthDelta, 0.01f);

            b.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
            relation.Apply();

            // 绝对值模式保持差值 -200：600 - 200 = 400。
            Assert.AreEqual(400f, a.rect.width, 0.01f);
        }

        [Test]
        public void SwitchingFollowModeRecordsCurrentValue()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 80f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Left, RelationSide.Left);
            relation.SetFollowWidth(b, true, true);
            relation.SetSizeFollowMode(b, false, null);

            Assert.IsFalse(relation.Items[0].WidthUsePercent);
            Assert.AreEqual(-200f, relation.Items[0].WidthDelta, 0.01f);
        }

        [Test]
        public void FollowHeightKeepsRecordedRatio()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 40f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 80f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Top, RelationSide.Top);
            relation.SetFollowHeight(b, true, true);
            relation.Apply();
            Assert.AreEqual(0.5f, relation.Items[0].HeightRatio, 0.001f);

            b.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 200f);
            relation.Apply();

            Assert.AreEqual(100f, a.rect.height, 0.01f);
        }

        [Test]
        public void SecondApplyWithoutChangeDoesNothing()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Right, RelationSide.Left);
            relation.Apply();
            Assert.IsFalse(relation.ApplyIfNeeded(), "无变化时不应重复写入");

            b.anchoredPosition = new Vector2(300f, 0f);
            Assert.IsTrue(relation.ApplyIfNeeded());
            Assert.IsFalse(relation.ApplyIfNeeded());
        }

        [Test]
        public void ManualWidthChangeUpdatesRatio()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 80f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Left, RelationSide.Left);
            relation.SetFollowWidth(b, true, true);
            relation.Apply();
            Assert.AreEqual(1f / 3f, relation.Items[0].WidthRatio, 0.001f);

            // 手动把宽度改成 150，下一次调度应把比例更新成 0.5，而不是拉回 100。
            a.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 150f);
            relation.ApplyIfNeeded();

            Assert.AreEqual(150f, a.rect.width, 0.01f);
            Assert.AreEqual(0.5f, relation.Items[0].WidthRatio, 0.001f);

            b.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
            relation.Apply();

            Assert.AreEqual(300f, a.rect.width, 0.01f);
        }

        [Test]
        public void ManualMoveUpdatesLinkDistance()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Right, RelationSide.Left);
            relation.Apply();

            // 手动移动自身，距离应重新记录为 150 - 200 = -50。
            a.anchoredPosition = new Vector2(50f, 0f);
            relation.ApplyIfNeeded();

            b.anchoredPosition = new Vector2(300f, 0f);
            relation.Apply();

            Assert.AreEqual(250f, a.position.x + a.rect.width, 0.01f);
        }

        [Test]
        public void SwitchingModeUsesCurrentSizesAfterTargetChange()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 80f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            // 绝对值模式：delta = 100 - 300 = -200。
            relation.Add(b, RelationSide.Left, RelationSide.Left);
            relation.SetFollowWidth(b, true, false);
            relation.Apply();
            Assert.AreEqual(-200f, relation.Items[0].WidthDelta, 0.01f);

            // 改目标宽度到 500，绝对值跟随：A = 500 - 200 = 300。
            b.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
            relation.Apply();
            Assert.AreEqual(300f, a.rect.width, 0.01f);

            // 此时切百分比，应记录 300 / 500 = 60%，而不是 100 / 300 的旧值。
            relation.SetSizeFollowMode(b, true, null);
            Assert.AreEqual(0.6f, relation.Items[0].WidthRatio, 0.001f);
            Assert.AreEqual(300f, a.rect.width, 0.01f);

            // 之后目标变 1000，按 60% 跟随：A = 600。
            b.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000f);
            relation.Apply();
            Assert.AreEqual(600f, a.rect.width, 0.01f);
        }

        [Test]
        public void SwitchingModeNeverChangesAnySize()
        {
            // B 关联 A：B 是被关联方，A 是目标。
            var a = CreateRect(_root.transform, "A", new Vector2(300f, 80f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(200f, 50f), new Vector2(0f, -120f));
            var relation = b.gameObject.AddComponent<UIRelation>();

            relation.Add(a, RelationSide.Left, RelationSide.Left);
            relation.SetFollowWidth(a, true, false);
            relation.Apply();
            Assert.AreEqual(-100f, relation.Items[0].WidthDelta, 0.01f);

            // 改目标 A 到 200，B 跟随到 100。
            a.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
            relation.Apply();
            Assert.AreEqual(100f, b.rect.width, 0.01f);

            // 切百分比：不改任何尺寸，只记录当前 100 / 200 = 50%。
            relation.SetSizeFollowMode(a, true, null);
            Assert.AreEqual(0.5f, relation.Items[0].WidthRatio, 0.001f);
            Assert.AreEqual(0f, relation.Items[0].WidthDelta, 0.01f, "切换后旧差值必须清零");
            Assert.AreEqual(100f, b.rect.width, 0.01f);
            Assert.AreEqual(200f, a.rect.width, 0.01f);

            // 之后目标 A 变 100，B 按 50% 跟随 = 50。
            a.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100f);
            relation.Apply();
            Assert.AreEqual(50f, b.rect.width, 0.01f);
        }

        [Test]
        public void SwitchingModeUsesCurrentSizesEvenBeforeApply()
        {
            // 目标已经改了但自身还没跟随，切换模式也不能改动任何组件。
            var a = CreateRect(_root.transform, "A", new Vector2(300f, 80f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(200f, 50f), new Vector2(0f, -120f));
            var relation = b.gameObject.AddComponent<UIRelation>();

            relation.Add(a, RelationSide.Left, RelationSide.Left);
            relation.SetFollowWidth(a, true, false);
            relation.Apply();

            a.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
            // 故意不调用 Apply，直接切换模式。
            relation.SetSizeFollowMode(a, true, null);

            Assert.AreEqual(200f, b.rect.width, 0.01f, "切换模式不允许改动自身尺寸");
            Assert.AreEqual(500f, a.rect.width, 0.01f, "切换模式不允许改动目标尺寸");
            Assert.AreEqual(0.4f, relation.Items[0].WidthRatio, 0.001f);
        }

        [Test]
        public void RelinkRecordsNewDistance()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Right, RelationSide.Left);
            a.anchoredPosition = new Vector2(50f, 0f);
            relation.Relink(b);
            relation.Apply();

            b.anchoredPosition = new Vector2(300f, 0f);
            relation.Apply();

            Assert.AreEqual(250f, a.position.x + a.rect.width, 0.01f);
        }

        [Test]
        public void RemoveLinkStopsFollowing()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Right, RelationSide.Left);
            relation.Apply();
            var aligned = a.position.x;

            relation.Remove(b, RelationSide.Right, RelationSide.Left);
            b.anchoredPosition = new Vector2(500f, 0f);
            relation.ApplyIfNeeded();

            Assert.AreEqual(aligned, a.position.x, 0.01f);
            Assert.IsFalse(relation.Has(b, RelationSide.Right, RelationSide.Left));
        }

        [Test]
        public void HorizontalLinkSwitchToPercentKeepsPosition()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 80f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Left, RelationSide.Left);
            relation.Apply();
            Assert.AreEqual(-200f, relation.Items[0].Links[0].Offset, 0.01f);

            // 像素 -> 百分比：-200 / 300 = -0.6667，位置不变。
            relation.SetLinkPercent(b, RelationSide.Left, RelationSide.Left, true);
            Assert.IsTrue(relation.Items[0].Links[0].Percent);
            Assert.AreEqual(-2f / 3f, relation.Items[0].Links[0].Offset, 0.001f);
            Assert.AreEqual(0f, a.position.x, 0.01f);

            // 目标变宽到 600：距离 = -0.6667 × 600 = -400，左边 = 200 - 400 = -200。
            b.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
            relation.Apply();
            Assert.AreEqual(-200f, a.position.x, 0.01f);
        }

        [Test]
        public void VerticalLinkPercentUsesTargetHeight()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 80f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Bottom, RelationSide.Bottom);
            relation.Apply();
            // a.Bottom = -50，b.Bottom = -80，距离 = 30。
            Assert.AreEqual(30f, relation.Items[0].Links[0].Offset, 0.01f);

            relation.SetLinkPercent(b, RelationSide.Bottom, RelationSide.Bottom, true);
            Assert.AreEqual(0.375f, relation.Items[0].Links[0].Offset, 0.001f);
            Assert.AreEqual(0f, a.position.y, 0.01f);

            // 目标高 200：b.Bottom = -200，距离 = 0.375 × 200 = 75，
            // a.Bottom = -200 + 75 = -125 → a.y = -75。
            b.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 200f);
            relation.Apply();
            Assert.AreEqual(-75f, a.position.y, 0.01f);
        }

        [Test]
        public void ChildTargetIsRejectedAsCycle()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var child = CreateRect(a, "Child", new Vector2(50f, 50f), Vector2.zero);
            var item = new UIRelationItem
            {
                Target = child,
                Links = new[]
                {
                    new UIRelationLink
                    {
                        Self = RelationSide.Right,
                        Target = RelationSide.Left,
                    },
                },
            };

            var warning = UIRelationCore.Describe(a, item);
            Assert.IsNotNull(warning);
            Assert.IsTrue(warning.Contains("子节点"));
        }

        [Test]
        public void MismatchedAxisIsRejected()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.Add(b, RelationSide.Left, RelationSide.Top);

            Assert.AreEqual(0, relation.Items.Length);
        }

        /// <summary>
        /// 运行时（模拟播放模式）自身被外部改尺寸时，以关联配置为准拉回，
        /// 不把外部写入重记成新基线；编辑器里手改才会更新配置值。
        /// </summary>
        [Test]
        public void ManualSizeChangeIsPulledBackWhenRunning()
        {
            var a = CreateRect(_root.transform, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(_root.transform, "B", new Vector2(300f, 80f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            relation.AddTarget(b);
            relation.SetFollowWidth(b, true, false);
            relation.Apply();
            Assert.AreEqual(100f, a.rect.width, 0.01f);
            Assert.AreEqual(-200f, relation.Items[0].WidthDelta, 0.01f);

            relation.RelinkOnSelfChangeOverride = false;
            a.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400f);

            Assert.IsTrue(relation.ApplyIfNeeded());
            Assert.AreEqual(100f, a.rect.width, 0.01f);
            Assert.AreEqual(-200f, relation.Items[0].WidthDelta, 0.01f, "运行时不重记基线");
        }

        /// <summary>
        /// 目标是 stretch（left/right/top/bottom）布局时，父面板变高会同时改目标高度、
        /// 并带着自身移动。父面板变高不能被当成“自身被外部改动”重记比例，否则不跟随。
        /// </summary>
        [Test]
        public void FollowHeightTracksStretchTargetWhenParentResizes()
        {
            var (panel, target, self, relation) = CreateStretchScenario();

            relation.SetFollowHeight(target, true, true);
            relation.Apply();
            Assert.AreEqual(40f, self.rect.height, 0.01f);
            Assert.AreEqual(40f / 180f, relation.Items[0].HeightRatio, 0.001f);

            panel.sizeDelta = new Vector2(400f, 400f);
            Assert.AreEqual(380f, target.rect.height, 0.01f);

            relation.ApplyIfNeeded();

            // 比例保持 40/180：380 × 40/180 ≈ 84.44。
            Assert.AreEqual(380f * 40f / 180f, self.rect.height, 0.01f);
        }

        [Test]
        public void FollowHeightAbsoluteModeTracksStretchTargetWhenParentResizes()
        {
            var (panel, target, self, relation) = CreateStretchScenario();

            relation.SetFollowHeight(target, true, false);
            relation.Apply();
            Assert.AreEqual(-140f, relation.Items[0].HeightDelta, 0.01f);

            panel.sizeDelta = new Vector2(400f, 400f);
            relation.ApplyIfNeeded();

            // 差值保持 -140：380 - 140 = 240。
            Assert.AreEqual(240f, self.rect.height, 0.01f);
        }

        /// <summary>
        /// 目标 stretch 到父面板（上下各留 10），自身居中于父面板：
        /// 父面板高度变化时自身位置会跟着变，用来覆盖“自身被带着走”的场景。
        /// </summary>
        private (RectTransform panel, RectTransform target, RectTransform self, UIRelation relation)
            CreateStretchScenario()
        {
            var panel = CreateRect(_root.transform, "Panel", new Vector2(400f, 200f), Vector2.zero);

            var target = CreateRect(panel, "Target", new Vector2(0f, -20f), Vector2.zero);
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = Vector2.zero;

            var self = CreateRect(panel, "Self", new Vector2(100f, 40f), Vector2.zero);
            self.anchorMin = new Vector2(0.5f, 0.5f);
            self.anchorMax = new Vector2(0.5f, 0.5f);
            self.pivot = new Vector2(0.5f, 0.5f);
            self.anchoredPosition = Vector2.zero;

            var relation = self.gameObject.AddComponent<UIRelation>();
            relation.AddTarget(target);
            return (panel, target, self, relation);
        }

        [Test]
        public void LayoutGroupSizeControlConflictsWithSizeFollow()
        {
            var panel = CreateRect(_root.transform, "Panel", new Vector2(400f, 300f), Vector2.zero);
            var group = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.childControlWidth = true;
            group.childControlHeight = false;

            var a = CreateRect(panel, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(panel, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();
            relation.AddTarget(b);
            relation.SetFollowWidth(b, true, false);

            var conflict = UIRelationCore.DescribeLayoutConflict(a, relation.Items[0]);
            Assert.IsNotNull(conflict, "布局组控制宽度时“宽 → 宽”应冲突");
            StringAssert.Contains("宽 → 宽", conflict);

            // 高度没有被布局组控制，“高 → 高”不冲突。
            relation.SetFollowHeight(b, true, false);
            relation.SetFollowWidth(b, false);
            Assert.IsNull(UIRelationCore.DescribeLayoutConflict(a, relation.Items[0]),
                "childControlHeight 关闭时高度跟随不应冲突");

            // Ignore Layout 后布局组完全跳过该节点，冲突消失。
            relation.SetFollowWidth(b, true, false);
            var element = a.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            Assert.IsNull(UIRelationCore.DescribeLayoutConflict(a, relation.Items[0]));
        }

        [Test]
        public void LayoutGroupPositionControlConflictsWithLinks()
        {
            var panel = CreateRect(_root.transform, "Panel", new Vector2(400f, 300f), Vector2.zero);
            var group = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.childControlWidth = false;
            group.childControlHeight = false;

            var a = CreateRect(panel, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(panel, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();

            // HorizontalLayoutGroup 只写 x：垂直边对不冲突，水平边对冲突。
            relation.Add(b, RelationSide.Top, RelationSide.Top);
            Assert.IsNull(UIRelationCore.DescribeLayoutConflict(a, relation.Items[0]));

            relation.Add(b, RelationSide.Left, RelationSide.Left);
            var conflict = UIRelationCore.DescribeLayoutConflict(a, relation.Items[0]);
            Assert.IsNotNull(conflict);
            StringAssert.Contains("水平边关联", conflict);
        }

        [Test]
        public void GridLayoutGroupConflictsWithSizeAndPosition()
        {
            var panel = CreateRect(_root.transform, "Panel", new Vector2(400f, 300f), Vector2.zero);
            panel.gameObject.AddComponent<GridLayoutGroup>();

            var a = CreateRect(panel, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(panel, "B", new Vector2(100f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();
            relation.AddTarget(b);
            relation.SetFollowHeight(b, true, false);

            Assert.IsNotNull(UIRelationCore.DescribeLayoutConflict(a, relation.Items[0]));
        }

        [Test]
        public void ConflictingRelationIsNotApplied()
        {
            var panel = CreateRect(_root.transform, "Panel", new Vector2(400f, 300f), Vector2.zero);
            var group = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.childControlWidth = true;

            var a = CreateRect(panel, "A", new Vector2(100f, 50f), Vector2.zero);
            var b = CreateRect(panel, "B", new Vector2(200f, 50f), new Vector2(200f, 0f));
            var relation = a.gameObject.AddComponent<UIRelation>();
            relation.AddTarget(b);
            relation.SetFollowWidth(b, true, false);
            Assert.AreEqual(-100f, relation.Items[0].WidthDelta, 0.01f);

            // 目标变宽：宽度由布局组控制，关联应被跳过而不是写入。
            b.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400f);
            relation.ApplyIfNeeded();

            Assert.AreEqual(100f, a.rect.width, 0.01f);
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 size,
            Vector2 position)
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
