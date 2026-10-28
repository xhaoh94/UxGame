using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ux.Editor
{
    /// <summary>仅在编辑状态替换 ScrollRect，保留其配置、事件及已加载场景里的序列化引用。</summary>
    public static class UGUIVirtualListScrollRectUpgrade
    {
        [MenuItem("CONTEXT/ScrollRect/升级为虚拟列表 ScrollRect")]
        private static void UpgradeFromMenu(MenuCommand command)
        {
            Upgrade((ScrollRect)command.context);
        }

        public static UGUIVirtualListScrollRect Upgrade(ScrollRect source)
        {
            if (source == null)
                throw new System.ArgumentNullException(nameof(source));
            if (source is UGUIVirtualListScrollRect existing)
                return existing;
            if (source.GetType() != typeof(ScrollRect))
                throw new System.InvalidOperationException("检测到其他自定义 ScrollRect 子类，请手动接入布局补偿，不要自动替换。");
            if (EditorApplication.isPlayingOrWillChangePlaymode || !source.gameObject.scene.IsValid())
                throw new System.InvalidOperationException("请在编辑状态对场景里的 ScrollRect 执行升级。");

            var settings = new Settings(source);
            var references = FindReferences(source);
            var gameObject = source.gameObject;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("升级虚拟列表 ScrollRect");
            try
            {
                // ScrollRect 不允许重复挂载，必须先记录配置再替换组件。
                Undo.DestroyObjectImmediate(source);
                var replacement = Undo.AddComponent<UGUIVirtualListScrollRect>(gameObject);
                settings.Apply(replacement);
                foreach (var reference in references)
                {
                    Object owner = ReferenceEquals(reference.Owner, source) ? replacement : reference.Owner;
                    if (owner == null)
                        continue;
                    var serialized = new SerializedObject(owner);
                    var property = serialized.FindProperty(reference.Path);
                    if (property == null)
                        continue;
                    property.objectReferenceValue = replacement;
                    serialized.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(replacement);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                Undo.CollapseUndoOperations(group);
                return replacement;
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static List<Reference> FindReferences(ScrollRect source)
        {
            var result = new List<Reference>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                        continue;
                    var serialized = new SerializedObject(component);
                    var iterator = serialized.GetIterator();
                    while (iterator.Next(true))
                    {
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == source)
                            result.Add(new Reference { Owner = component, Path = iterator.propertyPath });
                    }
                }
            }
            return result;
        }

        private sealed class Reference
        {
            public Object Owner;
            public string Path;
        }

        private sealed class Settings
        {
            private readonly RectTransform _content;
            private readonly RectTransform _viewport;
            private readonly bool _enabled;
            private readonly bool _horizontal;
            private readonly bool _vertical;
            private readonly ScrollRect.MovementType _movement;
            private readonly float _elasticity;
            private readonly bool _inertia;
            private readonly float _deceleration;
            private readonly float _sensitivity;
            private readonly Scrollbar _horizontalBar;
            private readonly Scrollbar _verticalBar;
            private readonly ScrollRect.ScrollbarVisibility _horizontalVisibility;
            private readonly ScrollRect.ScrollbarVisibility _verticalVisibility;
            private readonly float _horizontalSpacing;
            private readonly float _verticalSpacing;
            private readonly ScrollRect.ScrollRectEvent _events;

            public Settings(ScrollRect source)
            {
                _content = source.content;
                _viewport = source.viewport;
                _enabled = source.enabled;
                _horizontal = source.horizontal;
                _vertical = source.vertical;
                _movement = source.movementType;
                _elasticity = source.elasticity;
                _inertia = source.inertia;
                _deceleration = source.decelerationRate;
                _sensitivity = source.scrollSensitivity;
                _horizontalBar = source.horizontalScrollbar;
                _verticalBar = source.verticalScrollbar;
                _horizontalVisibility = source.horizontalScrollbarVisibility;
                _verticalVisibility = source.verticalScrollbarVisibility;
                _horizontalSpacing = source.horizontalScrollbarSpacing;
                _verticalSpacing = source.verticalScrollbarSpacing;
                _events = source.onValueChanged;
            }

            public void Apply(ScrollRect target)
            {
                target.content = _content;
                target.viewport = _viewport;
                target.horizontal = _horizontal;
                target.vertical = _vertical;
                target.movementType = _movement;
                target.elasticity = _elasticity;
                target.inertia = _inertia;
                target.decelerationRate = _deceleration;
                target.scrollSensitivity = _sensitivity;
                target.horizontalScrollbar = _horizontalBar;
                target.verticalScrollbar = _verticalBar;
                target.horizontalScrollbarVisibility = _horizontalVisibility;
                target.verticalScrollbarVisibility = _verticalVisibility;
                target.horizontalScrollbarSpacing = _horizontalSpacing;
                target.verticalScrollbarSpacing = _verticalSpacing;
                target.onValueChanged = _events;
                target.enabled = _enabled;
            }
        }
    }
}
