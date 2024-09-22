using UnityEngine;
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
