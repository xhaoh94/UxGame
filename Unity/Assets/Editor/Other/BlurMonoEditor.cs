using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ux;

[CustomEditor(typeof(BlurMono), true)]
public class BlurMonoEditor : Editor
{
    protected virtual void OnEnable()
    {
        if (serializedObject == null)
            return;

    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        BlurMono blur = target as BlurMono;

        serializedObject.Update();
        SceneBlur.IsFlag = EditorGUILayout.Toggle("是否开启模糊", SceneBlur.IsFlag);
        if (SceneBlur.IsFlag)
        {
            SceneBlur.BlurTimes = EditorGUILayout.IntSlider("模糊次数", SceneBlur.BlurTimes, 0, 5);
            SceneBlur.DownSample = EditorGUILayout.IntSlider("图片缩放程度", SceneBlur.DownSample, 1, 7);
            SceneBlur.BlurRadius = EditorGUILayout.Slider("模糊半径", SceneBlur.BlurRadius, 0, 5);
            SceneBlur.BlurDepth = EditorGUILayout.Slider("模糊景深", SceneBlur.BlurDepth, 0, 1);
            SceneBlur.BlurValue = EditorGUILayout.Slider("总体模糊", SceneBlur.BlurValue, 0, 0.5f);
        }
    }
}
