using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Ux
{
    public class Blur
    {
        public static void SetCamera(Camera camera, bool isFlag)
        {
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = isFlag;
        }
    }
}
