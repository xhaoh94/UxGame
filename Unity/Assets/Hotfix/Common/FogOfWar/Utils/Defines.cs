
using UnityEngine;

public static class Defines
{
    #region Layers
    public const string c_LayerDefault = "Default";
    public const string c_LayerHidden = "Hidden";
    public const string c_LayerFogOfWar = "FogOfWar";
    #endregion
}

/// <summary>
/// 相机地面交点
/// Y 轴为地面高度
/// </summary>
public struct CameraGroundCrossPoint
{
    //public CameraGroundCrossPoint(Camera camera)
    //{
    //    _camera = camera;
    //}
    public Camera Camera;
    public Vector3 LeftBottom;
    public Vector3 LeftTop;
    public Vector3 RightBottom;
    public Vector3 RightTop;

    /// <summary>
    /// 最小位置
    /// </summary>
    public Vector2 minPosition
    {
        get
        {
            float minX = Mathf.Min(Mathf.Min(LeftTop.x, RightTop.x), Mathf.Min(LeftBottom.x, RightBottom.x));
            float minZ = Mathf.Min(Mathf.Min(LeftTop.z, RightTop.z), Mathf.Min(LeftBottom.z, RightBottom.z));
            Vector3 mainCamPos = Camera.transform.position;
            return new Vector2(Mathf.Min(mainCamPos.x, minX), Mathf.Min(mainCamPos.z, minZ));
        }
    }

    /// <summary>
    /// 最大位置
    /// </summary>
    public Vector2 maxPosition
    {
        get
        {
            float maxX = Mathf.Max(Mathf.Max(LeftTop.x, RightTop.x), Mathf.Max(LeftBottom.x, RightBottom.x));
            float maxZ = Mathf.Max(Mathf.Max(LeftTop.z, RightTop.z), Mathf.Max(LeftBottom.z, RightBottom.z));
            Vector3 mainCamPos = Camera.transform.position;
            return new Vector2(Mathf.Max(maxX, mainCamPos.x), Mathf.Max(mainCamPos.z, maxZ));
        }
    }

}