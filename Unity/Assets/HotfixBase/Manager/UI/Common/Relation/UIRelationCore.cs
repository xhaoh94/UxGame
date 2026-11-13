using UnityEngine;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// 关联计算核心。做几何计算与布局冲突判定，不持有状态，可在编辑器和 EditMode 测试里直接调用。
    /// 语义对齐 FairyGUI：边对记录“距离”，宽高记录“比例”，目标变化时按记录值跟随。
    /// </summary>
    public static class UIRelationCore
    {
        /// <summary>小于该值的位移/尺寸差异视为未变化，避免无效写入触发 Canvas 重建。</summary>
        public const float Epsilon = 0.01f;

        public static bool IsHorizontal(RelationSide side) =>
            side == RelationSide.Left || side == RelationSide.Center || side == RelationSide.Right;

        public static bool IsVertical(RelationSide side) =>
            side == RelationSide.Top || side == RelationSide.Middle || side == RelationSide.Bottom;

        /// <summary>
        /// 应用一个目标的全部关联。返回是否发生移动/尺寸变化；未变化时不写 RectTransform。
        /// </summary>
        public static RelationChange Apply(RectTransform self, in UIRelationItem item)
        {
            if (self == null) return RelationChange.None;
            var target = item.Target;
            if (target == null) return RelationChange.None;
            if (ReferenceEquals(self, target)) return RelationChange.None;

            // 同轴两条边对时宽度/高度由边距推导，此时忽略宽高比例，避免互相覆盖。
            var hasLeft = HasSide(item, RelationSide.Left);
            var hasRight = HasSide(item, RelationSide.Right);
            var hasTop = HasSide(item, RelationSide.Top);
            var hasBottom = HasSide(item, RelationSide.Bottom);

            var change = RelationChange.None;
            if (ApplySize(self, item, target, !(hasLeft && hasRight), !(hasTop && hasBottom)))
                change |= RelationChange.Resized;
            if (ApplyLinks(self, item, target))
                change |= RelationChange.Moved | RelationChange.Resized;
            return change;
        }

        /// <summary>
        /// 测量“自身边到目标边”的当前距离（世界空间），用于勾选关联时记录初始距离。
        /// Percent 为 true 时返回按目标尺寸归一化后的比例。
        /// </summary>
        public static float MeasureOffset(RectTransform self, RelationSide selfSide,
            RectTransform target, RelationSide targetSide, bool percent)
        {
            if (self == null || target == null) return 0f;
            if (IsHorizontal(selfSide) && IsHorizontal(targetSide))
            {
                var distance = GetEdgeWorldX(self, selfSide) - GetEdgeWorldX(target, targetSide);
                return percent && Mathf.Abs(target.rect.width) > Epsilon
                    ? distance / target.rect.width
                    : distance;
            }
            if (IsVertical(selfSide) && IsVertical(targetSide))
            {
                var distance = GetEdgeWorldY(self, selfSide) - GetEdgeWorldY(target, targetSide);
                return percent && Mathf.Abs(target.rect.height) > Epsilon
                    ? distance / target.rect.height
                    : distance;
            }
            return 0f;
        }

        /// <summary>测量宽度比例：自身宽 / 目标宽。用于勾选“宽 -> 宽”时记录。</summary>
        public static float MeasureWidthRatio(RectTransform self, RectTransform target)
        {
            if (self == null || target == null) return 1f;
            if (Mathf.Abs(target.rect.width) <= Epsilon) return 1f;
            return self.rect.width / target.rect.width;
        }

        /// <summary>测量高度比例：自身高 / 目标高。用于勾选“高 -> 高”时记录。</summary>
        public static float MeasureHeightRatio(RectTransform self, RectTransform target)
        {
            if (self == null || target == null) return 1f;
            if (Mathf.Abs(target.rect.height) <= Epsilon) return 1f;
            return self.rect.height / target.rect.height;
        }

        /// <summary>测量宽度差值：自身宽 - 目标宽。绝对值模式使用。</summary>
        public static float MeasureWidthDelta(RectTransform self, RectTransform target)
        {
            if (self == null || target == null) return 0f;
            return self.rect.width - target.rect.width;
        }

        /// <summary>测量高度差值：自身高 - 目标高。绝对值模式使用。</summary>
        public static float MeasureHeightDelta(RectTransform self, RectTransform target)
        {
            if (self == null || target == null) return 0f;
            return self.rect.height - target.rect.height;
        }

        /// <summary>编辑器校验，返回 null 表示没有问题。</summary>
        public static string Describe(RectTransform self, in UIRelationItem item)
        {
            if (item.Target == null)
                return "关联目标为空。";
            if (self != null && ReferenceEquals(self, item.Target))
                return "关联目标不能是自身。";
            if (self != null && item.Target.IsChildOf(self))
                return "关联目标不能是自身的子节点，会形成循环。";
            if (item.IsEmpty)
                return "没有勾选任何关联。";
            if (HasRotatedAncestor(item.Target))
                return "目标带旋转，当前按轴对齐计算，结果可能不准确。";

            if (item.FollowWidth && item.WidthUsePercent && item.WidthRatio < -Epsilon)
                return "宽度比例为负数，无法应用。";
            if (item.FollowHeight && item.HeightUsePercent && item.HeightRatio < -Epsilon)
                return "高度比例为负数，无法应用。";

            if (item.Links == null) return null;
            for (int i = 0; i < item.Links.Length; i++)
            {
                if (!item.Links[i].IsValid)
                    return "存在自身边与目标边类型不匹配的关联（水平必须配水平，垂直必须配垂直）。";
            }
            return null;
        }

        /// <summary>
        /// 检测「自身的关联项」与直接父级 LayoutGroup 的冲突。
        /// 布局组会写子节点的位置与尺寸，同一轴上的关联每帧都会被它覆盖，无法生效。
        /// 节点挂了 LayoutElement 且勾选 Ignore Layout 时，布局组会完全跳过他，不算冲突。
        /// 返回 null 表示没有冲突。
        /// </summary>
        public static string DescribeLayoutConflict(RectTransform self, in UIRelationItem item)
        {
            if (self == null || item.IsEmpty) return null;

            // 布局组按 ILayoutIgnorer 跳过子节点：位置和尺寸都归关联自己管。
            var ignorer = self.GetComponent<ILayoutIgnorer>();
            if (ignorer != null && ignorer.ignoreLayout) return null;

            var parent = self.parent as RectTransform;
            if (parent == null) return null;
            var group = parent.GetComponent<LayoutGroup>();
            if (group == null || !group.isActiveAndEnabled) return null;

            GetLayoutControl(group, out var controlsWidth, out var controlsHeight,
                out var controlsX, out var controlsY);

            var followWidth = item.FollowWidth && controlsWidth;
            var followHeight = item.FollowHeight && controlsHeight;
            var linkX = controlsX && HasLinkOnAxis(item, true);
            var linkY = controlsY && HasLinkOnAxis(item, false);
            if (!followWidth && !followHeight && !linkX && !linkY) return null;

            var conflicts = new System.Collections.Generic.List<string>(4);
            if (followWidth) conflicts.Add("“宽 → 宽”");
            if (followHeight) conflicts.Add("“高 → 高”");
            if (linkX) conflicts.Add("水平边关联");
            if (linkY) conflicts.Add("垂直边关联");

            var sizeConflict = followWidth || followHeight;
            var positionConflict = linkX || linkY;
            var drives = sizeConflict && positionConflict ? "尺寸和位置"
                : sizeConflict ? "尺寸"
                : "位置";

            return $"父节点「{parent.name}」的 {group.GetType().Name} 会覆盖本节点的{drives}" +
                   $"（{string.Join("、", conflicts)}），这些关联无法生效。" +
                   "请关闭该轴的 Child Control，或在本节点加 LayoutElement 并勾选 Ignore Layout。";
        }

        /// <summary>
        /// 布局组会写哪些位置/尺寸轴。Horizontal/Vertical 只写沿轴位置，尺寸按 childControl；
        /// GridLayoutGroup 强制写位置与尺寸。
        /// </summary>
        private static void GetLayoutControl(LayoutGroup group, out bool width, out bool height,
            out bool x, out bool y)
        {
            width = false;
            height = false;
            x = false;
            y = false;
            switch (group)
            {
                case HorizontalLayoutGroup horizontal:
                    x = true;
                    width = horizontal.childControlWidth;
                    height = horizontal.childControlHeight;
                    break;
                case VerticalLayoutGroup vertical:
                    y = true;
                    width = vertical.childControlWidth;
                    height = vertical.childControlHeight;
                    break;
                case GridLayoutGroup:
                    x = true;
                    y = true;
                    width = true;
                    height = true;
                    break;
            }
        }

        private static bool HasLinkOnAxis(in UIRelationItem item, bool horizontal)
        {
            if (item.Links == null) return false;
            for (int i = 0; i < item.Links.Length; i++)
            {
                var link = item.Links[i];
                if (!link.IsValid) continue;
                if (horizontal ? link.IsHorizontal : link.IsVertical) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ 尺寸

        private static bool ApplySize(RectTransform self, in UIRelationItem item, RectTransform target,
            bool allowWidth, bool allowHeight)
        {
            var changed = false;
            if (item.FollowWidth && allowWidth)
                changed |= SetWidth(self, ResolveSize(target.rect.width, item.WidthUsePercent,
                    item.WidthRatio, item.WidthDelta));
            if (item.FollowHeight && allowHeight)
                changed |= SetHeight(self, ResolveSize(target.rect.height, item.HeightUsePercent,
                    item.HeightRatio, item.HeightDelta));
            return changed;
        }

        /// <summary>
        /// 百分比：自身尺寸 = 目标尺寸 × 比例；绝对值：自身尺寸 = 目标尺寸 + 差值。
        /// 两种模式互相独立，切换时只使用对应模式的值，不会把另一份残留值叠加进来。
        /// </summary>
        public static float ResolveSize(float targetSize, bool usePercent, float ratio, float delta)
        {
            if (!usePercent) return targetSize + delta;
            if (Mathf.Abs(ratio) <= Epsilon) return targetSize;
            return targetSize * ratio;
        }

        private static bool HasSide(in UIRelationItem item, RelationSide side)
        {
            if (item.Links == null) return false;
            for (int i = 0; i < item.Links.Length; i++)
                if (item.Links[i].Self == side) return true;
            return false;
        }

        // ------------------------------------------------------------------ 边对

        private static bool ApplyLinks(RectTransform self, in UIRelationItem item, RectTransform target)
        {
            if (item.Links == null || item.Links.Length == 0) return false;

            bool changed = false;
            if (ApplyHorizontal(self, item, target)) changed = true;
            if (ApplyVertical(self, item, target)) changed = true;
            return changed;
        }

        private static bool ApplyHorizontal(RectTransform self, in UIRelationItem item, RectTransform target)
        {
            bool hasLeft = false, hasRight = false, hasCenter = false;
            float desiredLeft = 0f, desiredRight = 0f, desiredCenter = 0f;

            for (int i = 0; i < item.Links.Length; i++)
            {
                var link = item.Links[i];
                if (!link.IsHorizontal) continue;
                var targetEdge = GetEdgeWorldX(target, link.Target);
                var distance = link.Percent ? link.Offset * target.rect.width : link.Offset;
                switch (link.Self)
                {
                    case RelationSide.Left:
                        hasLeft = true;
                        desiredLeft = targetEdge + distance;
                        break;
                    case RelationSide.Right:
                        hasRight = true;
                        desiredRight = targetEdge + distance;
                        break;
                    case RelationSide.Center:
                        hasCenter = true;
                        desiredCenter = targetEdge + distance;
                        break;
                }
            }

            if (hasLeft && hasRight)
                return StretchHorizontal(self, desiredLeft, desiredRight);

            if (hasLeft)
                return MoveToEdgeX(self, RelationSide.Left, desiredLeft);
            if (hasRight)
                return MoveToEdgeX(self, RelationSide.Right, desiredRight);
            if (hasCenter)
                return MoveToEdgeX(self, RelationSide.Center, desiredCenter);
            return false;
        }

        private static bool ApplyVertical(RectTransform self, in UIRelationItem item, RectTransform target)
        {
            bool hasTop = false, hasBottom = false, hasMiddle = false;
            float desiredTop = 0f, desiredBottom = 0f, desiredMiddle = 0f;

            for (int i = 0; i < item.Links.Length; i++)
            {
                var link = item.Links[i];
                if (!link.IsVertical) continue;
                var targetEdge = GetEdgeWorldY(target, link.Target);
                var distance = link.Percent ? link.Offset * target.rect.height : link.Offset;
                switch (link.Self)
                {
                    case RelationSide.Top:
                        hasTop = true;
                        desiredTop = targetEdge + distance;
                        break;
                    case RelationSide.Bottom:
                        hasBottom = true;
                        desiredBottom = targetEdge + distance;
                        break;
                    case RelationSide.Middle:
                        hasMiddle = true;
                        desiredMiddle = targetEdge + distance;
                        break;
                }
            }

            if (hasTop && hasBottom)
                return StretchVertical(self, desiredTop, desiredBottom);

            if (hasTop)
                return MoveToEdgeY(self, RelationSide.Top, desiredTop);
            if (hasBottom)
                return MoveToEdgeY(self, RelationSide.Bottom, desiredBottom);
            if (hasMiddle)
                return MoveToEdgeY(self, RelationSide.Middle, desiredMiddle);
            return false;
        }

        // ------------------------------------------------------------------ 移动

        private static bool MoveToEdgeX(RectTransform self, RelationSide side, float desiredWorldX)
        {
            var current = GetEdgeWorldX(self, side);
            var delta = desiredWorldX - current;
            if (Mathf.Abs(delta) < Epsilon) return false;
            self.position += new Vector3(delta, 0f, 0f);
            return true;
        }

        private static bool MoveToEdgeY(RectTransform self, RelationSide side, float desiredWorldY)
        {
            var current = GetEdgeWorldY(self, side);
            var delta = desiredWorldY - current;
            if (Mathf.Abs(delta) < Epsilon) return false;
            self.position += new Vector3(0f, delta, 0f);
            return true;
        }

        /// <summary>左右两边同时关联：左边定位、宽度由两条边推导，效果等于“跟随父容器拉伸”。</summary>
        private static bool StretchHorizontal(RectTransform self, float desiredLeft, float desiredRight)
        {
            var worldWidth = Mathf.Max(0f, desiredRight - desiredLeft);
            var scaleX = Mathf.Abs(self.lossyScale.x) > Epsilon ? Mathf.Abs(self.lossyScale.x) : 1f;
            var localWidth = worldWidth / scaleX;

            var changed = false;
            if (Mathf.Abs(self.rect.width - localWidth) >= Epsilon)
            {
                self.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, localWidth);
                changed = true;
            }
            if (MoveToEdgeX(self, RelationSide.Left, desiredLeft))
                changed = true;
            return changed;
        }

        private static bool StretchVertical(RectTransform self, float desiredTop, float desiredBottom)
        {
            var worldHeight = Mathf.Max(0f, desiredTop - desiredBottom);
            var scaleY = Mathf.Abs(self.lossyScale.y) > Epsilon ? Mathf.Abs(self.lossyScale.y) : 1f;
            var localHeight = worldHeight / scaleY;

            var changed = false;
            if (Mathf.Abs(self.rect.height - localHeight) >= Epsilon)
            {
                self.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, localHeight);
                changed = true;
            }
            if (MoveToEdgeY(self, RelationSide.Top, desiredTop))
                changed = true;
            return changed;
        }

        // ------------------------------------------------------------------ 工具

        private static bool SetWidth(RectTransform self, float width)
        {
            width = Mathf.Max(0f, width);
            if (Mathf.Abs(self.rect.width - width) < Epsilon) return false;
            self.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            return true;
        }

        private static bool SetHeight(RectTransform self, float height)
        {
            height = Mathf.Max(0f, height);
            if (Mathf.Abs(self.rect.height - height) < Epsilon) return false;
            self.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            return true;
        }

        /// <summary>取某条边的世界坐标（水平轴）。</summary>
        public static float GetEdgeWorldX(RectTransform rect, RelationSide side)
        {
            var local = rect.rect;
            var x = side == RelationSide.Left ? local.xMin
                : side == RelationSide.Right ? local.xMax
                : local.center.x;
            return rect.TransformPoint(new Vector3(x, local.center.y, 0f)).x;
        }

        /// <summary>取某条边的世界坐标（垂直轴）。</summary>
        public static float GetEdgeWorldY(RectTransform rect, RelationSide side)
        {
            var local = rect.rect;
            var y = side == RelationSide.Bottom ? local.yMin
                : side == RelationSide.Top ? local.yMax
                : local.center.y;
            return rect.TransformPoint(new Vector3(local.center.x, y, 0f)).y;
        }

        private static bool HasRotatedAncestor(RectTransform rect)
        {
            var current = rect;
            while (current != null)
            {
                // 用四元数点积判断是否为单位旋转：localEulerAngles 每次都要做四元数->欧拉转换，
                // 且 0°/360° 附近的浮点误差会误判成"有旋转"。
                if (Mathf.Abs(Quaternion.Dot(current.localRotation, Quaternion.identity)) < 1f - 1e-7f)
                    return true;
                current = current.parent as RectTransform;
            }
            return false;
        }
    }
}
