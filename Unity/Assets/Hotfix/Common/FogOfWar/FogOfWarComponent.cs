using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Ux
{
    public class FogOfWarComponent : Entity, IAwakeSystem, IUpdateSystem
    {
        GameObject FogOfWar;
        /// <summary>
        /// 地图是否是中心为中心点，还是以左下角
        /// </summary>
        bool IsCenter = true;
        /// <summary>
        /// 多久更新一遍视野（单位秒）
        /// </summary>
        const float c_updateDuration = 0.1f;

        /// <summary>
        /// 迷雾的平滑速度
        /// </summary>
        const float c_smoothSpeed = 5f;

        /// <summary>
        /// 当前以什么Mask来查看视野
        /// </summary>
        [SerializeField]
        int m_visionMask = 1;

        /// <summary>
        /// 存放最开始的迷雾信息，用于给摄像机进行二次处理
        /// </summary>
        MeshRenderer m_rawRenderer;

        /// <summary>
        /// 渲染最终的迷雾
        /// 注意要覆盖整个场景
        /// </summary>
        MeshRenderer m_finalRenderer;

        /// <summary>
        /// 存放原始迷雾的贴图
        /// </summary>
        Texture2D m_fowTex;

        /// <summary>
        /// 存放视野的具体数据
        /// </summary>
        VisionGrid m_visionGrid;

        /// <summary>
        /// 存放地形高度等信息
        /// </summary>
        TerrainGrid m_terrainGrid;

        /// <summary>
        /// 当前战争迷雾贴图的颜色信息
        /// </summary>
        Color[] m_curtColors;

        /// <summary>
        /// 战争迷雾想要过渡到的目标颜色（不直接设置目标颜色是为了有渐变的效果）
        /// </summary>
        Color[] m_targetColors;

        float m_nextUpdateTime;

        #region get-set        
        public float TileSize { get; private set; } = 1f;
        public int Width { get; private set; } = 200;

        public int Height { get; private set; } = 200;

        public TerrainGrid TerrainGrid { get { return m_terrainGrid; } }

        readonly Dictionary<long, UnitVision> m_unitDict = new Dictionary<long, UnitVision>();

        #endregion
        Map Map => ParentAs<Map>();
        Camera mainCamera => Map.Camera.MapCamera;
        public void OnAwake()
        {
            FogOfWar = new GameObject("FogOfWar");
            SetMono(FogOfWar);
            var astar = Map.GetComponent<AStarComponent>();
            var gridGraph = astar.AstarPath.data.gridGraph;

            Width = gridGraph.width;
            Height = gridGraph.Depth;
            TileSize = gridGraph.nodeSize;
            m_visionGrid = new VisionGrid(Width, Height);
            m_terrainGrid = new TerrainGrid(this, Width, Height);
            m_targetColors = new Color[Width * Height];
            m_curtColors = new Color[Width * Height];
            m_fowTex = new Texture2D(Width, Height, TextureFormat.Alpha8, false);

            InitRawRenderer();
            InitFinalRenderer();
            InitBlurCamera();
            InitFOWCamera();
        }
        void IUpdateSystem.OnUpdate()
        {
            if (m_unitDict.Count == 0)
                return;

            //定时更新视野数据
            if (Time.time >= m_nextUpdateTime)
            {
                m_nextUpdateTime = Time.time + c_updateDuration;
                CalculateVision();
                UpdateTargetColors(m_visionMask);
            }

            //判断单位的可见性
            UpdateVisibles();

            //平滑颜色
            SmoothColor();
        }
        /// <summary>
        /// 计算所有单位的视野数据
        /// </summary>
        /// <param name="units">所有单位的列表</param>
        void CalculateVision()
        {
            m_visionGrid.Clear();
            foreach (var (_, unit) in m_unitDict)
            {
                Vector2Int centerTile = WorldPosToTilePos(unit.WorldPos);

                if (IsOutsideMap(centerTile))
                {
                    //Debug.LogError($"单位{unit.m_gameObject.name}的格子位置{centerTile}超出了地图范围");
                    continue;
                }

                List<Vector2Int> tiles = CircleByBoundingCircle(centerTile, unit.Range, TileSize);
                for (int i = 0; i < tiles.Count; i++)
                {
                    if (!IsBlocked(centerTile, tiles[i], unit))
                        m_visionGrid.SetVisible(tiles[i].x, tiles[i].y, unit.Mask);
                }
            }
        }



        /// <summary>
        /// 更新原始迷雾贴图的目标颜色
        /// </summary>
        void UpdateTargetColors(int entityMask)
        {
            //var points = getCameraGroundCrossPoint(mainCamera);

            //var min = WorldPosToTilePos(points.minPosition);
            //var max = WorldPosToTilePos(points.maxPosition);

            //for (int x = min.x; x < max.x; x++)
            //{
            //    for (int y = min.y; y < max.y; y++)
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int index = x + y * Width;
                    m_targetColors[index] = new Color(0, 0, 0, 1);

                    if (m_visionGrid.IsVisible(x, y, entityMask))
                        m_targetColors[index] = new Color(0, 0, 0, 0);
                    else if (m_visionGrid.WasVisible(x, y, entityMask))
                        m_targetColors[index] = new Color(0, 0, 0, 0.4f);
                }
            }

            //如果在这里直接设置颜色，则移动时会很明显发现迷雾是一顿一顿的，不平滑
            //m_fowTex.SetPixels(m_targetColors);
            //m_fowTex.Apply(false);
        }

        void SmoothColor()
        {
            for (int i = 0; i < m_targetColors.Length; i++)
            {
                Color target = m_targetColors[i];
                Color curt = m_curtColors[i];
                m_curtColors[i] = Color.Lerp(curt, target, c_smoothSpeed * Time.deltaTime);
            }

            m_fowTex.SetPixels(m_curtColors);
            m_fowTex.Apply(false);
        }

        /// <summary>
        /// 根据可见性设置渲染层级
        /// </summary>
        void UpdateVisibles()
        {
            foreach (var (_, unit) in m_unitDict)
            {
                string layerName = IsVisible(m_visionMask, unit) ? Defines.c_LayerDefault : Defines.c_LayerHidden;
                unit.unit.Layer = LayerMask.NameToLayer(layerName);
            }
        }
        public void UpdateUnit(long id, UnitVision unitVision)
        {
            m_unitDict[id] = unitVision;
        }
        public void RemoveUnit(long id)
        {
            m_unitDict.Remove(id);
        }
        #region 初始化
        void InitRawRenderer()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "RawRenderer";
            go.tag = "FogOfWar";
            go.layer = LayerMask.NameToLayer(Defines.c_LayerFogOfWar);
            go.transform.SetParent(FogOfWar.transform);
            go.transform.position = new Vector3(1000, 0, 0);

            m_rawRenderer = go.GetComponent<MeshRenderer>();
            m_rawRenderer.sharedMaterial = new Material(Shader.Find("Ux/FogOfWar/RawFogOfWar"))
            {
                mainTexture = m_fowTex
            };
        }

        void InitFinalRenderer()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "FinalRenderer";
            go.layer = LayerMask.NameToLayer(Defines.c_LayerFogOfWar);
            go.tag = "FogOfWar";

            Transform trans = go.transform;
            trans.SetParent(FogOfWar.transform);
            m_finalRenderer = trans.GetComponent<MeshRenderer>();
            m_finalRenderer.sharedMaterial = new Material(Shader.Find("Ux/FogOfWar/FinalFogOfWar"));

            //将最终的迷雾覆盖到整张地图
            trans.localScale = new Vector3(Width * TileSize, Height * TileSize, 1);
            trans.rotation = Quaternion.Euler(90, 0, 0);
            if (IsCenter)
            {
                trans.position = new Vector3(0, 1, 0);
            }
            else
            {
                trans.position = new Vector3(trans.localScale.x / 2, 1, trans.localScale.y / 2);
            }
        }

        void InitBlurCamera()
        {
            GameObject go = new GameObject("BlurCamera");
            go.transform.SetParent(FogOfWar.transform);
            go.AddComponent<Blur>();

            Camera cam = go.AddComponent<Camera>();

            cam.cullingMask = LayerMask.GetMask(Defines.c_LayerFogOfWar);
            cam.clearFlags = CameraClearFlags.Depth;

            cam.depth = mainCamera.depth + 1;
            cam.useOcclusionCulling = false;
            cam.allowHDR = false;
            cam.allowMSAA = false;

            int width = Width * 4;
            int height = Height * 4;
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            RenderTexture rt = new RenderTexture(width, height, 0, format);
            rt.antiAliasing = 1;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;

            cam.targetTexture = rt;
            m_finalRenderer.sharedMaterial.mainTexture = rt;

            var pos = m_rawRenderer.transform.position;
            cam.transform.position = new Vector3(pos.x, pos.y, pos.z - 1);
            cam.orthographicSize = 0.5f;
            cam.orthographic = true;
        }

        void InitFOWCamera()
        {
            GameObject go = new GameObject("FogOfWarCamera");
            Camera cam = go.AddComponent<Camera>();
            var cameraData = cam.GetUniversalAdditionalCameraData();
            cameraData.renderType = CameraRenderType.Overlay;
            mainCamera.GetUniversalAdditionalCameraData().cameraStack.Add(cam);

            cam.cullingMask = LayerMask.GetMask(Defines.c_LayerFogOfWar);
            cam.clearFlags = CameraClearFlags.Depth;
            var main = mainCamera;
            if (main != null)
            {
                cam.depth = main.depth + 1;
                cam.fieldOfView = main.fieldOfView;
                cam.nearClipPlane = main.nearClipPlane;
                cam.farClipPlane = main.farClipPlane;
                cam.rect = main.rect;
                cam.useOcclusionCulling = false;
                cam.allowHDR = false;
                cam.allowMSAA = false;

                var trans = cam.transform;
                trans.SetParent(main.transform);
                trans.localPosition = Vector3.zero;
                trans.localScale = Vector3.one;
                trans.localRotation = Quaternion.identity;
            }
        }
        #endregion

        #region 转换
        public Vector2Int WorldPosToTilePos(Vector2 worldPos)
        {
            float offx = 0, offy = 0;
            if (IsCenter)
            {
                offx = Width / 2;
                offy = Height / 2;
            }
            int x = (int)((worldPos.x + offx) / TileSize);
            int y = (int)((worldPos.y + offy) / TileSize);
            return new Vector2Int(x, y);
        }

        public Vector2Int WorldPosToTilePos(Vector3 worldPos)
        {
            return WorldPosToTilePos(new Vector2(worldPos.x, worldPos.z));
        }
        #endregion

        #region 方法
        bool IsVisible(int curtMask, UnitVision unit)
        {
            if ((curtMask & unit.Mask) > 0)
                return true;

            Vector2Int tilePos = WorldPosToTilePos(unit.WorldPos);
            if (m_visionGrid.IsVisible(tilePos, curtMask))
                return true;

            return false;
        }
        /// <summary>
        /// 判断格子位置是否超过地图范围
        /// </summary>
        bool IsOutsideMap(Vector2Int tilePos)
        {
            return tilePos.x < 0 || tilePos.x >= Width ||
                        tilePos.y < 0 || tilePos.y > Height;
        }

        /// <summary>
        /// 两点间的视野是否因为地形被阻挡了
        /// </summary>
        bool IsBlocked(Vector2Int startTile, Vector2Int targetTile, UnitVision unit)
        {
            List<Vector2Int> points = LineByBresenhams(startTile, targetTile);
            for (int i = 0; i < points.Count; i++)
            {
                m_terrainGrid.GetData(points[i], out short altitude, out short grassId);
                if (altitude > unit.TerrainHeight)
                    return true;

                if (grassId != 0 && grassId != unit.GrassId)
                    return true;
            }

            return false;
        }

        List<Vector2Int> LineByBresenhams(Vector2Int start, Vector2Int end)
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
        List<Vector2Int> CircleByBoundingCircle(Vector2Int centerTile, float radius, float tileSize)
        {
            //GC：实际项目使用时最好用Pool来存取
            List<Vector2Int> result = new List<Vector2Int>();

            int radiusCount = Mathf.CeilToInt(radius / tileSize);
            int sqr = radiusCount * radiusCount;
            int top = centerTile.y + radiusCount;
            if (top > Height) top = Height;
            int bottom = centerTile.y - radiusCount;
            if (bottom < 0) bottom = 0;

            for (int y = bottom; y <= top; y++)
            {
                int dy = y - centerTile.y;
                int dx = Mathf.FloorToInt(Mathf.Sqrt(sqr - dy * dy));
                int left = centerTile.x - dx;
                if (left < 0) left = 0;
                int right = centerTile.x + dx;
                if (right > Width) right = Width;
                for (int x = left; x <= right; x++)
                    result.Add(new Vector2Int(x, y));
            }

            return result;
        }

        CameraGroundCrossPoint getCameraGroundCrossPoint(Camera camera)
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

        Vector3 CheckGroundSignPoint(Camera camera, Vector3 dri)
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
        Vector3 GetIntersectWithLineAndPlane(Vector3 point, Vector3 direct, Vector3 planeNormal, Vector3 planePoint)
        {
            float d = Vector3.Dot(planePoint - point, planeNormal) / Vector3.Dot(direct.normalized, planeNormal);
            //直线与平面的交点
            Vector3 hitPoint = (d * direct.normalized) + point;
            return hitPoint;
        }
        #endregion
    }
}