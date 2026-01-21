using System;
using UnityEngine;
using UnityEngine.Rendering;
[Serializable, VolumeComponentMenu("CustomPostprocess/Blur/GaussianBlur2")]
public class GaussianBlur2 : VolumeComponent
{
    //[参数设置]
    public IntParameter times = new ClampedIntParameter(1, 0, 5);               // 模糊次数--- ClampedIntParameter保证变量值在给定范围内
    public IntParameter downSample = new ClampedIntParameter(2, 1, 7);                      // 图片缩放程度
    //[Shader参数]
    public FloatParameter radius = new ClampedFloatParameter(1.0f, 0.0f, 5.0f); // 模糊半径
    public FloatParameter depth = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);          // 模糊景深
    public FloatParameter value = new ClampedFloatParameter(0.2f, 0.0f, 0.5f);      // 总体模糊程度
}

