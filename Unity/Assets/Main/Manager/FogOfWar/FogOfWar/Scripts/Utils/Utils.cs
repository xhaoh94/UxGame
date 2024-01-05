using UnityEngine;
using System.Collections.Generic;

public static class Utils
{
    /// <summary>
    /// 利用bresenhams直线算法找到两点间的所有格子
    /// </summary>
    /// <param name="start">直线起点</param>
    /// <param name="end">直线终点</param>
    /// <returns>直线覆盖的格子</returns>
    public static List<Vector2Int> LineByBresenhams(Vector2Int start, Vector2Int end)
    {
        //GC：实际项目使用时最好用Pool来存取
        List<Vector2Int> result = new List<Vector2Int>();

        int dx = end.x - start.x;
        int dy = end.y - start.y;
        int ux = dx > 0 ? 1 : -1;
        int uy = dy > 0 ? 1 : -1;
        int x = start.x;
        int y = start.y;
        int eps = 0;
        dx = Mathf.Abs(dx);
        dy = Mathf.Abs(dy);

        if (dx > dy)
        {
            for (x = start.x; x != end.x; x += ux)
            {
                result.Add(new Vector2Int(x, y));

                eps += dy;
                if ((eps << 1) >= dx)
                {
                    y += uy;
                    eps -= dx;
                }
            }
        }
        else
        {
            for (y = start.y; y != end.y; y += uy)
            {
                result.Add(new Vector2Int(x, y));

                eps += dx;
                if ((eps << 1) >= dy)
                {
                    x += ux;
                    eps -= dy;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 找到圆形覆盖的格子
    /// </summary>
    /// <param name="centerTile">圆心的格子坐标</param>
    /// <param name="radius">圆的半径</param>
    /// <param name="tileSize">一格的大小</param>
    /// <returns>圆形覆盖的格子</returns>
    public static List<Vector2Int> CircleByBoundingCircle(Vector2Int centerTile, float radius, float tileSize)
    {
        //GC：实际项目使用时最好用Pool来存取
        List<Vector2Int> result = new List<Vector2Int>();

        int radiusCount = Mathf.CeilToInt(radius / tileSize);
        int sqr = radiusCount * radiusCount;
        int top = centerTile.y + radiusCount;
        int bottom = centerTile.y - radiusCount;

        for (int y = bottom; y <= top; y++)
        {
            int dy = y - centerTile.y;
            int dx = Mathf.FloorToInt(Mathf.Sqrt(sqr - dy * dy));
            int left = centerTile.x - dx;
            int right = centerTile.x + dx;
            for (int x = left; x <= right; x++)
                result.Add(new Vector2Int(x, y));
        }

        return result;
    }

    public static Vector2 XZ(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
    }

    public static CameraGroundCrossPoint getCameraGroundCrossPoint(Camera camera)
    {
        var fov = camera.fieldOfView;//相机的Fov        
        var asp = camera.aspect;

        var yf = Mathf.Tan(fov / 2 * Mathf.Deg2Rad);
        var xf = yf * asp;
        //获取相机视野的四条射线；
        Matrix4x4 l2w = camera.transform.localToWorldMatrix;
        Vector3 f0 = l2w * new Vector3(-xf, -yf, 1);
        Vector3 f1 = l2w * new Vector3(-xf, yf, 1);
        Vector3 f2 = l2w * new Vector3(xf, -yf, 1);
        Vector3 f3 = l2w * new Vector3(xf, yf, 1);

        CameraGroundCrossPoint crossPoint = new CameraGroundCrossPoint();
        crossPoint.Camera = camera;
        //获取视野与地面的交点，或是远裁剪面垂直投射到地面的交点；
        crossPoint.LeftBottom = CheckGroundSignPoint(camera, f0);
        crossPoint.LeftTop = CheckGroundSignPoint(camera, f1);
        crossPoint.RightBottom = CheckGroundSignPoint(camera, f2);
        crossPoint.RightTop = CheckGroundSignPoint(camera, f3);

        return crossPoint;
    }

    private static Vector3 CheckGroundSignPoint(Camera camera, Vector3 dri)
    {
        Vector3 cpt = camera.transform.position;
        Vector3 farPlaneNormal = camera.transform.forward;
        Vector3 farPlanePoint = camera.transform.position + (farPlaneNormal * camera.farClipPlane);

        float height = 0;

        //计算与远裁剪面的交点；
        var signPoint = GetIntersectWithLineAndPlane(cpt, dri, farPlaneNormal, farPlanePoint);

        //这里相机先到达了远裁剪面，而没有与地面相交；
        if (signPoint.y > height)
        {
            //将远裁剪面的位置投影到地面上返回
            signPoint.y = height;
            return signPoint;
        }
        //此时被地面截断；
        Vector3 groundPoint = new Vector3(0, 0, 0);
        signPoint = GetIntersectWithLineAndPlane(cpt, dri, Vector3.up, groundPoint);

        return signPoint;
    }

    /// <summary>
    /// 计算直线与平面的交点
    /// </summary>
    /// <param name="point">直线上某一点</param>
    /// <param name="direct">直线的方向</param>
    /// <param name="planeNormal">垂直于平面的的向量</param>
    /// <param name="planePoint">平面上的任意一点</param>
    /// <returns></returns>
    public static Vector3 GetIntersectWithLineAndPlane(Vector3 point, Vector3 direct, Vector3 planeNormal, Vector3 planePoint)
    {
        float d = Vector3.Dot(planePoint - point, planeNormal) / Vector3.Dot(direct.normalized, planeNormal);
        //直线与平面的交点
        Vector3 hitPoint = (d * direct.normalized) + point;
        return hitPoint;
    }
}