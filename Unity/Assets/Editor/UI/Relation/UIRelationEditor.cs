using UnityEditor;
using UnityEngine;

namespace Ux.Editor
{
    /// <summary>
    /// 与 FairyGUI 一致的勾选式关联面板：
    /// 勾上「自身边 → 目标边」即保持两者当前距离；勾上「宽 → 宽」即保持当前宽高比例。
    /// </summary>
    [CustomEditor(typeof(UIRelation))]
    [CanEditMultipleObjects]
    public sealed class UIRelationEditor : UnityEditor.Editor
    {
        private static readonly RelationSide[] HorizontalSides =
            { RelationSide.Left, RelationSide.Center, RelationSide.Right };

        private static readonly RelationSide[] VerticalSides =
            { RelationSide.Top, RelationSide.Middle, RelationSide.Bottom };

        private const float CellWidth = 35f;
        // 文字左右各留这么多；右侧这段就是"文字 → 勾选框"的距离。
        private const float LabelGap = 8f;
        private static float _rowLabelWidth;
        // 只留勾选框本身的位置：这一列没有文字，放宽只会把「%」推远。
        private const float FollowToggleWidth = 20f;
        // 「%」画成与勾选框同边长的方块；整行高的按钮会比勾选框大一圈。
        private const float PercentBoxSize = 14f;

        private static GUIStyle _centerLabel;
        private static GUIStyle _centerBoldLabel;

        private static GUIStyle CenterLabel => _centerLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
        };

        private static GUIStyle CenterBoldLabel => _centerBoldLabel ??=
            new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
            };

        /// <summary>
        /// 标签列宽度按实际文字宽度算：写死一个值，一旦小于文字宽度就会裁字，
        /// 而且这个宽度同时决定勾选框的起始位置。
        /// </summary>
        private static float RowLabelWidth
        {
            get
            {
                if (_rowLabelWidth > 0f) return _rowLabelWidth;
                var textWidth = Mathf.Max(
                    CenterBoldLabel.CalcSize(new GUIContent("宽 → 宽")).x,
                    CenterLabel.CalcSize(new GUIContent("Middle")).x);
                _rowLabelWidth = textWidth + LabelGap * 2f;
                return _rowLabelWidth;
            }
        }

        private static GUIStyle _percentButton;

        /// <summary>
        /// miniButton 自带 fixedHeight=16 会覆盖传入矩形的高度，且内边距会把窄按钮里的文字挤掉；
        /// 复制一份清掉固定尺寸与内边距，矩形传多大就画多大。
        /// </summary>
        private static GUIStyle PercentButton => _percentButton ??= new GUIStyle(EditorStyles.miniButton)
        {
            fixedWidth = 0,
            fixedHeight = 0,
            padding = new RectOffset(0, 0, 0, 0),
            alignment = TextAnchor.MiddleCenter,
        };

        /// <summary>「%」方形按钮：与勾选框同边长，垂直居中于行高。</summary>
        private static Rect GetPercentRect(float x, float rowY, float lineHeight)
        {
            return new Rect(x, rowY + (lineHeight - PercentBoxSize) * 0.5f, PercentBoxSize,
                PercentBoxSize);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var relation = (UIRelation)target;

            EditorGUILayout.HelpBox(
                "勾选后保持当前距离或比例，对象不会跳动；目标变化时自动跟随。" +
                "左右两条同时勾选会拉伸宽度。",
                MessageType.None);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_enableInEditor"),
                new GUIContent("编辑器实时跟随"));
            serializedObject.ApplyModifiedProperties();

            if (!serializedObject.isEditingMultipleObjects)
                DrawTargets(relation);

            EditorGUILayout.Space();
            if (GUILayout.Button("添加目标"))
            {
                Undo.RecordObject(relation, "添加目标");
                relation.AddTarget();
                EditorUtility.SetDirty(relation);
                serializedObject.Update();
                Repaint();
            }

            if (!serializedObject.isEditingMultipleObjects)
                DrawWarnings(relation);
        }

        private void DrawTargets(UIRelation relation)
        {
            var items = relation.Items;
            if (items == null) return;

            for (int index = 0; index < items.Length; index++)
            {
                // 结构变化（增删目标）后立即结束本帧绘制，下一帧按新数组重画。
                if (DrawTargetBlock(relation, index))
                {
                    Repaint();
                    return;
                }
            }
        }

        /// <summary>返回 true 表示结构已变化，需要退出本次绘制。</summary>
        private bool DrawTargetBlock(UIRelation relation, int index)
        {
            var items = relation.Items;
            if (items == null || index >= items.Length) return false;
            var item = items[index];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var replaced = false;
            var removed = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    var picked = EditorGUILayout.ObjectField("目标", item.Target,
                        typeof(RectTransform), true) as RectTransform;
                    if (scope.changed)
                    {
                        Undo.RecordObject(relation, "修改目标");
                        relation.SetTarget(index, picked);
                        EditorUtility.SetDirty(relation);
                        serializedObject.Update();
                        replaced = true;
                    }
                }

                if (!replaced && GUILayout.Button("移除", EditorStyles.miniButton, GUILayout.Width(48f)))
                {
                    Undo.RecordObject(relation, "移除目标");
                    relation.RemoveAt(index);
                    EditorUtility.SetDirty(relation);
                    serializedObject.Update();
                    removed = true;
                }
            }

            // 结构变化后必须结束本帧，下一帧按新数组重画。
            if (replaced || removed)
            {
                EditorGUILayout.EndVertical();
                return true;
            }

            var rect = item.Target;
            if (rect == null)
            {
                EditorGUILayout.HelpBox("拖入目标节点后即可勾选关联。", MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                return false;
            }

            if (rect == (RectTransform)relation.transform)
            {
                EditorGUILayout.HelpBox("目标不能是自身。", MessageType.Error);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                return false;
            }

            var changed = false;
            DrawSizeRows(relation, rect, ref changed);
            DrawMatrix(relation, rect, true, ref changed);
            DrawMatrix(relation, rect, false, ref changed);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("重新记录距离", EditorStyles.miniButton, GUILayout.Width(96f)))
                {
                    Undo.RecordObject(relation, "重新记录关联距离");
                    relation.Relink(rect);
                    EditorUtility.SetDirty(relation);
                    changed = true;
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            if (changed) serializedObject.Update();
            return changed;
        }


        private void DrawSizeRows(UIRelation relation, RectTransform rect, ref bool changed)
        {
            var item = FindItem(relation, rect);
            changed |= DrawFollowRow(relation, rect, "宽 → 宽", string.Empty, item.FollowWidth,
                item.WidthUsePercent, true);
            changed |= DrawFollowRow(relation, rect, "高 → 高", string.Empty,  item.FollowHeight,
                item.HeightUsePercent, false);
        }

        /// <summary>
        /// 宽/高跟随：勾选时按当前尺寸记录；「%」点亮表示按比例跟随，熄灭表示按固定差值跟随。
        /// 用固定矩形排版，避免 GUILayout 把开关挤到不同位置。
        /// </summary>
        private bool DrawFollowRow(UIRelation relation, RectTransform rect, string rowLabel,
            string label, bool follow, bool usePercent, bool widthAxis)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var row = GUILayoutUtility.GetRect(0f, lineHeight, GUILayout.ExpandWidth(true));
            var x = row.x;

            if (!string.IsNullOrEmpty(rowLabel))
                GUI.Label(new Rect(x, row.y, RowLabelWidth, lineHeight), rowLabel,
                    CenterBoldLabel);
            x += RowLabelWidth;

            var followRect = new Rect(x, row.y, FollowToggleWidth, lineHeight);
            var newFollow = GUI.Toggle(followRect, follow, label, EditorStyles.toggle);
            x += FollowToggleWidth;

            var percentRect = GetPercentRect(x, row.y, lineHeight);
            bool newUsePercent;
            using (new EditorGUI.DisabledScope(!follow))
                newUsePercent = GUI.Toggle(percentRect, usePercent, "%", PercentButton);

            if (newFollow != follow)
            {
                Undo.RecordObject(relation, "UIRelation 尺寸关联");
                if (widthAxis) relation.SetFollowWidth(rect, newFollow);
                else relation.SetFollowHeight(rect, newFollow);
                EditorUtility.SetDirty(relation);
                return true;
            }

            if (!follow || newUsePercent == usePercent) return false;

            // 切换量纲时按“当前两个组件的实际尺寸”重新记录，不沿用另一量纲的旧值。
            Undo.RecordObject(relation, "UIRelation 尺寸量纲");
            relation.SetSizeFollowMode(rect, widthAxis ? newUsePercent : null,
                widthAxis ? null : newUsePercent);
            EditorUtility.SetDirty(relation);
            return true;
        }

        /// <summary>
        /// 勾选矩阵。每行末尾的「%」是该行距离量纲开关：
        /// 点亮表示距离按目标宽/高比例，熄灭表示固定像素。位置固定，不随勾选变化。
        /// </summary>
        private bool DrawMatrix(UIRelation relation, RectTransform rect, bool horizontal,
            ref bool changed)
        {
            var sides = horizontal ? HorizontalSides : VerticalSides;
            var item = FindItem(relation, rect);
            var links = item.Links ?? System.Array.Empty<UIRelationLink>();
            var lineHeight = EditorGUIUtility.singleLineHeight;

            // 表头：自身边标签 + 目标边列名 + 行末量纲列名
            var header = GUILayoutUtility.GetRect(0f, lineHeight, GUILayout.ExpandWidth(true));
            // GUI.Label(new Rect(header.x, header.y, RowLabelWidth, lineHeight),
            //     horizontal ? "水平" : "垂直", CenterBoldLabel);
            for (int i = 0; i < sides.Length; i++)
                GUI.Label(new Rect(header.x + RowLabelWidth + i * CellWidth, header.y, CellWidth,
                    lineHeight), sides[i].ToString(), CenterLabel);
            GUI.Label(new Rect(header.x + RowLabelWidth + sides.Length * CellWidth, header.y,
                PercentBoxSize, lineHeight), "", CenterLabel);

            foreach (var selfSide in sides)
            {
                var row = GUILayoutUtility.GetRect(0f, lineHeight, GUILayout.ExpandWidth(true));
                GUI.Label(new Rect(row.x, row.y, RowLabelWidth, lineHeight), selfSide.ToString(),
                    CenterLabel);

                var x = row.x + RowLabelWidth;
                foreach (var targetSide in sides)
                {
                    var linkIndex = FindLink(links, selfSide, targetSide);
                    var on = linkIndex >= 0;
                    var toggleRect = new Rect(x + (CellWidth - 16f) * 0.5f, row.y, 16f, lineHeight);
                    var newOn = EditorGUI.Toggle(toggleRect, on);
                    x += CellWidth;

                    if (newOn == on) continue;

                    Undo.RecordObject(relation, "UIRelation 关联");
                    if (newOn) relation.Add(rect, selfSide, targetSide);
                    else relation.Remove(rect, selfSide, targetSide);
                    EditorUtility.SetDirty(relation);
                    changed = true;
                    return changed;
                }

                // 行末量纲开关：取该行第一条关联的状态，切换时作用于整行。
                var rowState = GetRowPercent(links, selfSide, out var hasLink);
                using (new EditorGUI.DisabledScope(!hasLink))
                {
                    var newPercent = GUI.Toggle(GetPercentRect(x, row.y, lineHeight),
                        rowState, "%", PercentButton);
                    if (hasLink && newPercent != rowState)
                    {
                        Undo.RecordObject(relation, "UIRelation 边对量纲");
                        SetRowPercent(relation, rect, links, selfSide, newPercent);
                        EditorUtility.SetDirty(relation);
                        changed = true;
                        return changed;
                    }
                }
            }

            return changed;
        }

        private static bool GetRowPercent(UIRelationLink[] links, RelationSide selfSide,
            out bool hasLink)
        {
            hasLink = false;
            var percent = false;
            for (int i = 0; i < links.Length; i++)
            {
                if (links[i].Self != selfSide) continue;
                if (!hasLink)
                {
                    percent = links[i].Percent;
                    hasLink = true;
                }
            }
            return percent;
        }

        private static void SetRowPercent(UIRelation relation, RectTransform rect,
            UIRelationLink[] links, RelationSide selfSide, bool percent)
        {
            for (int i = 0; i < links.Length; i++)
            {
                if (links[i].Self != selfSide) continue;
                relation.SetLinkPercent(rect, selfSide, links[i].Target, percent);
            }
        }

        private static int FindLink(UIRelationLink[] links, RelationSide selfSide,
            RelationSide targetSide)
        {
            for (int i = 0; i < links.Length; i++)
                if (links[i].Self == selfSide && links[i].Target == targetSide) return i;
            return -1;
        }

        private static UIRelationItem FindItem(UIRelation relation, RectTransform rect)
        {
            var items = relation.Items;
            if (items == null) return default;
            for (int i = 0; i < items.Length; i++)
                if (items[i].Target == rect) return items[i];
            return default;
        }

        private void DrawWarnings(UIRelation relation)
        {
            var rect = relation.transform as RectTransform;
            if (rect == null)
            {
                EditorGUILayout.HelpBox("UIRelation 只能挂在 RectTransform 节点上。", MessageType.Error);
                return;
            }

            var items = relation.Items;
            if (items == null) return;
            for (int i = 0; i < items.Length; i++)
            {
                var warning = UIRelationCore.Describe(rect, items[i]);
                if (warning != null)
                    EditorGUILayout.HelpBox($"关联 {i}: {warning}", MessageType.Warning);
            }
        }
    }
}
