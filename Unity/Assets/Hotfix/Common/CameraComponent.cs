﻿using Cinemachine;
using Ux;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
            UIMgr.Ins.SetSceneCamera(MapCamera);
            //MapCamera.GetUniversalAdditionalCameraData().cameraStack.Add(FairyGUI.StageCamera.main);            
        }

        public void SetFollow(Transform follow)
        {
            MapVCamear.Follow = follow;
        }
        public void SetLookAt(Transform follow)
        {
            MapVCamear.LookAt = follow;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            MapCamera = null;
            MapVCamear = null;
            UIMgr.Ins.SetSceneCamera(null);
        }
    }
}