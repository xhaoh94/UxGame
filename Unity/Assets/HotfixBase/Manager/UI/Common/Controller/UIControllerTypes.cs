using System;
using UnityEngine;

namespace Ux
{
    /// <summary>控制器的一页，对应 FairyGUI 的 Controller Page。</summary>
    [Serializable]
    public struct ControllerPage
    {
        [Tooltip("页面名称，用于 SetSelectedPage。")]
        public string Name;

        [Tooltip("页面 Id，业务侧可以按 Id 判断当前页。")]
        public int Id;

#if UNITY_EDITOR
        [Tooltip("仅编辑器备注，不参与运行时逻辑。")]
        public string EditorNote;
#endif
    }

    /// <summary>
    /// 某一页对目标对象的属性值。属性是否启用由编辑器中的页面通用属性统一管理；
    /// 每个页面保存自己的值，未启用的属性回落到对象进入控制器前的默认状态。
    /// </summary>
    [Serializable]
    public struct ControllerState
    {
        [Tooltip("该状态对应的页面序号。")]
        public int PageIndex;

        public bool SetVisible;
        [Tooltip("页面激活时对象的可见性。")]
        public bool Visible;

        public bool SetPosition;
        [Tooltip("anchoredPosition。")]
        public Vector2 Position;

        public bool SetSize;
        [Tooltip("sizeDelta。")]
        public Vector2 Size;

        public bool SetColor;
        public Color Color;

        public bool SetText;
        public string Text;

        public bool SetAlpha;
        [Range(0f, 1f)] public float Alpha;

        public bool SetRotation;
        [Tooltip("Z 轴旋转角度。")]
        public float Rotation;

        public bool SetScale;
        public Vector3 Scale;

        public bool HasAny => SetVisible || SetPosition || SetSize || SetColor || SetText ||
            SetAlpha || SetRotation || SetScale;
    }
}
