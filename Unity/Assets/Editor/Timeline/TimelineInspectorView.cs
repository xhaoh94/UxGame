using System;
using UnityEngine;
using UnityEngine.UIElements;
using Ux.Editor.Timeline;

namespace Assets.Editor.Timeline
{
    public class TimelineInspectorBase : VisualElement
    {
        protected readonly ITimelineEditorSource Source;
        protected readonly object AssetObject;
        readonly object selection;
        Func<bool> callback;

        public TimelineInspectorBase(
            ITimelineEditorSource source,
            object selection,
            object assetObject)
        {
            Source = source;
            this.selection = selection;
            AssetObject = assetObject;
            style.flexGrow = 1;
            Source.Bind(selection, OnFreshView);
        }

        public bool IsSame(object value) => ReferenceEquals(selection, value);

        public void SetChcekValid(Func<bool> value)
        {
            callback = value;
        }

        protected bool ChcekValid()
        {
            return callback?.Invoke() ?? true;
        }

        public void Release()
        {
            Source.Unbind(selection, OnFreshView);
        }

        protected static Label CreateTitle(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 14;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 4;
            label.style.marginBottom = 8;
            return label;
        }

        protected void CommitChange()
        {
            Source.CommitInspectorChange(AssetObject);
            TimelineWindow.wnd?.clipView?.RefreshLayout();
        }

        protected virtual void OnFreshView() { }
    }

    public sealed class TimelineInspectorView
    {
        readonly VisualElement root;
        readonly TimelineEditorDocument document;
        TimelineInspectorBase current;

        public TimelineInspectorView(VisualElement root, TimelineEditorDocument document)
        {
            this.root = root;
            this.document = document;
            ShowEmptyState();
        }

        public void FreshInspector(object selection, Func<bool> chcekValid)
        {
            if (selection == null)
            {
                Clear();
                ShowEmptyState();
                return;
            }

            if (current != null && current.IsSame(selection))
            {
                current.SetChcekValid(chcekValid);
                return;
            }

            Clear();
            current = document?.CreateInspector(selection);
            if (current == null)
            {
                ShowEmptyState();
                return;
            }

            current.SetChcekValid(chcekValid);
            root.Add(current);
        }

        public void Clear()
        {
            current?.Release();
            current = null;
            root.Clear();
        }

        void ShowEmptyState()
        {
            var label = new Label("选择轨道或 Clip 以编辑属性");
            label.style.marginTop = 18;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = new Color(0.55f, 0.55f, 0.55f);
            root.Add(label);
        }
    }
}
