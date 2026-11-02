using System;
using UnityEngine;

namespace Ux
{
    /// <summary>关联的边。水平用 Left/Center/Right，垂直用 Top/Middle/Bottom。</summary>
    public enum RelationSide
    {
        None = 0,
        Left = 1,
        Center = 2,
        Right = 3,
        Top = 4,
        Middle = 5,
        Bottom = 6,
    }

    /// <summary>
    /// 一条边对，例如“自身左边 → 目标左边”。
    /// Offset 是该边对保持的距离，勾选关联时按当前位置自动记录，之后目标变化时距离不变。
    /// </summary>
    [Serializable]
    public struct UIRelationLink
    {
        [Tooltip("自身边。")]
        public RelationSide Self;

        [Tooltip("目标边。")]
        public RelationSide Target;

        [Tooltip("保持的距离；勾选关联时自动记录当前位置，通常不需要手改。")]
        public float Offset;

        [Tooltip("距离按目标尺寸比例解释：距离 = 比例 × 目标宽/高。")]
        public bool Percent;

        public bool IsHorizontal => UIRelationCore.IsHorizontal(Self) && UIRelationCore.IsHorizontal(Target);

        public bool IsVertical => UIRelationCore.IsVertical(Self) && UIRelationCore.IsVertical(Target);

        public bool IsValid => IsHorizontal || IsVertical;
    }

    /// <summary>一次关联计算的变更结果，用于决定是否需要下游继续收敛。</summary>
    [Flags]
    public enum RelationChange
    {
        None = 0,
        Moved = 1,
        Resized = 2,
    }

    /// <summary>
    /// 一个目标的全部关联，对应 FairyGUI 里“关联到某个对象/容器”的一组勾选。
    /// 同一水平轴可同时勾“左→左”和“右→右”，此时两边距离都保持、宽度自动拉伸。
    /// </summary>
    [Serializable]
    public struct UIRelationItem
    {
        [Tooltip("关联目标，可以是父容器，也可以是同层级或跨层级的其他节点。")]
        public RectTransform Target;

        [Tooltip("勾选的边对，距离在勾选时自动记录。")]
        public UIRelationLink[] Links;

        [Tooltip("宽 -> 宽：目标宽度变化时跟随。")]
        public bool FollowWidth;

        [Tooltip("宽 -> 宽使用百分比；关闭时按固定差值跟随。")]
        public bool WidthUsePercent;

        [Tooltip("宽度百分比：自身宽 = (目标宽 + 差值) × 比例，勾选时自动记录。")]
        public float WidthRatio;

        [Tooltip("宽度差值：绝对值模式下自身宽 = 目标宽 + 差值。")]
        public float WidthDelta;

        [Tooltip("高 -> 高：目标高度变化时跟随。")]
        public bool FollowHeight;

        [Tooltip("高 -> 高使用百分比；关闭时按固定差值跟随。")]
        public bool HeightUsePercent;

        [Tooltip("高度百分比：自身高 = (目标高 + 差值) × 比例，勾选时自动记录。")]
        public float HeightRatio;

        [Tooltip("高度差值：绝对值模式下自身高 = 目标高 + 差值。")]
        public float HeightDelta;

        public bool IsEmpty => Target == null ||
            ((Links == null || Links.Length == 0) && !FollowWidth && !FollowHeight);
    }
}
