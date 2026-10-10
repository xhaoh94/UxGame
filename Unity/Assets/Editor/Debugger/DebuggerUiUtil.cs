using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.Debugger
{
    /// <summary>
    /// 调试器窗口手搭 UI 的样式帮手（原来由 uxml 提供的常用样式组合）。
    /// 手搭时必须给控件设 name：辅助类（DebuggerObjectSearchListView 等）靠 root.Q&lt;T&gt;("name") 取控件。
    /// </summary>
    internal static class DebuggerUiUtil
    {
        static readonly Color DarkBackground = new Color(47f / 255f, 47f / 255f, 47f / 255f);
        static readonly Color IdTextColor = new Color(226f / 255f, 207f / 255f, 48f / 255f);

        public static VisualElement Row()
        {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;
            return element;
        }

        public static void SetBorder(this VisualElement element, Color color)
        {
            element.style.borderLeftWidth = 1f;
            element.style.borderRightWidth = 1f;
            element.style.borderTopWidth = 1f;
            element.style.borderBottomWidth = 1f;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
        }

        public static void SetMargin(this VisualElement element, float value)
        {
            element.style.marginLeft = value;
            element.style.marginRight = value;
            element.style.marginTop = value;
            element.style.marginBottom = value;
        }

        /// <summary>行项左侧的 ID 标签（深底黄字，宽 300）。</summary>
        public static Label IdLabel(string name)
        {
            var label = new Label { name = name };
            label.style.width = 300f;
            label.style.flexGrow = 0f;
            label.style.flexShrink = 1f;
            label.style.color = IdTextColor;
            label.SetMargin(1f);
            return label;
        }

        /// <summary>深色背景的行项左栏容器。</summary>
        public static VisualElement IdBox(Label label)
        {
            var box = new VisualElement();
            box.style.backgroundColor = DarkBackground;
            box.Add(label);
            return box;
        }

        /// <summary>只读文本框（原 uxml 的 picking-mode=Ignore + readonly）。</summary>
        public static TextField LockedField(string name, string label)
        {
            return new TextField(label) { name = name, pickingMode = PickingMode.Ignore, isReadOnly = true };
        }

        public static ListView DynamicList(string name)
        {
            var list = new ListView { name = name };
            list.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            return list;
        }
    }
}
