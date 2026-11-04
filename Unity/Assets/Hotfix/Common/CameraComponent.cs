using Cinemachine;
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
            // mapCamera 可能尚未激活，GameObject.Find 找不到未激活对象——找不到不报错，
            // 此时 UI 相机自持 Base 渲染；等界面再次上屏时 TryAttachAny 会重新找宿主。
            MapCamera = GameObject.Find("mapCamera")?.GetComponent<Camera>();
            MapVCamear = GameObject.Find("mapVCam")?.GetComponent<CinemachineVirtualCamera>();
            if (MapCamera != null)
                UIMgr.Ins.AttachUIStack(MapCamera);
            //mapVCam 等相机堆栈配置由业务层处理
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
        }
    }
}