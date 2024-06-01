#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ux
{
    partial class TimelineComponent
    {
        double _editorLastTime;
        void _EditorInit()
        {
            if (!Application.isPlaying)
            {
                if (_isPlaying)
                {
                    _editorLastTime = EditorApplication.timeSinceStartup;
                    UnityEditor.EditorApplication.update += _EditorUpdate;
                }
                else
                {
                    UnityEditor.EditorApplication.update -= _EditorUpdate;
                }
            }
        }
        void _EditorUpdate()
        {
            var deltaTime = EditorApplication.timeSinceStartup - _editorLastTime;
            Evaluate((float)(deltaTime * PlaySpeed));
        }
    }
}
#endif