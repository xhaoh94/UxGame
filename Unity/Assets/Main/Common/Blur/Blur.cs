using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ux
{
    public class Blur
    {
#if UNITY_EDITOR
        public static BlurMono mono;
#endif

        public static bool IsFlag { get; set; }
        static int _blurTimes = 3;
        // 模糊次数
        public static int BlurTimes
        {
            get { return _blurTimes; }
            set { _blurTimes = value; IsChangle = true; }

        }
        static int _downSample = 3;
        // 图片缩放程度
        public static int DownSample
        {
            get { return _downSample; }
            set { _downSample = value; IsChangle = true; }
        }
        static float _blurRadius = 2f;
        // 模糊半径
        public static float BlurRadius
        {
            get { return _blurRadius; }
            set { _blurRadius = value; IsChangle = true; }
        }
        static float _blurDepth = 0.1f;
        // 模糊景深
        public static float BlurDepth
        {
            get { return _blurDepth; }
            set { _blurDepth = value; IsChangle = true; }
        }

        public static float _blurValue = 0.3f;
        // 总体模糊程度
        public static float BlurValue
        {
            get { return _blurValue; }
            set { _blurValue = value; IsChangle = true; }
        }

        public static bool IsChangle { get; set; } = true;

        public static void SetSceneBlur(bool isFlag)
        {
#if UNITY_EDITOR
            if (mono == null)
            {
                var go = new GameObject("[Blur]");
                UnityEngine.Object.DontDestroyOnLoad(go);
                mono = go.AddComponent<BlurMono>();
            }
#endif            
            Blur.IsFlag = isFlag;
        }
    }
}
