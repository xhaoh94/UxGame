using Cinemachine;
using Ux;
using UnityEngine;

namespace Ux
{
    public class CameraComponent : Entity, IAwakeSystem
    {
        public Camera MapCamera { get; private set; }
        public CinemachineVirtualCamera MapVCamear { get; private set; }

        public void OnAwake()
        {
            MapCamera = GameObject.Find("mapCamera").GetComponent<Camera>();
            MapVCamear = GameObject.Find("mapVCam").GetComponent<CinemachineVirtualCamera>();
        }

        public void SetFollow(Transform follow)
        {
            MapVCamear.Follow = follow;
        }
        public void SetLookAt(Transform follow)
        {
            MapVCamear.LookAt = follow;
        }
    }
}