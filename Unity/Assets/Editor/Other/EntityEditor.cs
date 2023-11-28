using UnityEditor;
using UnityEngine;
using Ux;

namespace Assets.Editor.Other
{
    [CustomEditor(typeof(Ux.EntityEditorViewer), true)]
    public class EntityEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var entity = target as EntityEditorViewer;
            entity?.Layout();
        }
    }
}