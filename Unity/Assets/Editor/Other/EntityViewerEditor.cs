using UnityEditor;
using Ux;
namespace Ux.Editor
{
    [CustomEditor(typeof(Ux.EntityHierarchy), true)]
    public class EntityViewerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var entity = target as EntityHierarchy;
            entity?.Layout();
        }
    }
}