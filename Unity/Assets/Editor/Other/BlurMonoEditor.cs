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
        Blur.IsFlag = EditorGUILayout.Toggle("是否开启模糊", Blur.IsFlag);
        if (Blur.IsFlag)
        {
            Blur.BlurTimes = EditorGUILayout.IntSlider("模糊次数", Blur.BlurTimes, 0, 5);
            Blur.DownSample = EditorGUILayout.IntSlider("图片缩放程度", Blur.DownSample, 1, 7);
            Blur.BlurRadius = EditorGUILayout.Slider("模糊半径", Blur.BlurRadius, 0, 5);
            Blur.BlurDepth = EditorGUILayout.Slider("模糊景深", Blur.BlurDepth, 0, 1);
            Blur.BlurValue = EditorGUILayout.Slider("总体模糊", Blur.BlurValue, 0, 0.5f);
        }
    }
}
