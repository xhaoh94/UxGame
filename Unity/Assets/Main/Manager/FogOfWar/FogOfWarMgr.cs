using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ux
{
    public partial class FogOfWarMgr : Singleton<FogOfWarMgr>
    {
        const string LayerDefault = "Default";
        const string LayerHidden = "Hidden";
        const string LayerFogOfWar = "FogOfWar";
        const string Prefab = "FogOfWar";

        bool _init;
        public bool IsInit => _init;

        GameObject _FogOfWar;
        /// <summary>
        /// 相机
        /// </summary>
        Camera _mainCamera;

        /// <summary>
        /// 地图是否是中心为中心点，还是以左下角
        /// </summary>        
        bool _isCenter = true;
        /// <summary>
        /// 当前以什么Mask来查看视野
        /// </summary>        
        int _visionMask = 1;

        /// <summary>
        /// 是否开启已走过视野
        /// </summary>
        bool _wasVision = true;
        float _wasVisionAlpha = 0.3f;
        /// <summary>
        /// 多久更新一遍视野（单位秒）
        /// </summary>
        float _updateDuration = 0.1f;
        float _nextUpdateTime;
        /// <summary>
        /// 迷雾的平滑速度
        /// </summary>
        const float _smoothSpeed = 5f;


        /// <summary>
        /// 存放最开始的迷雾信息，用于给摄像机进行二次处理
        /// </summary>
        MeshRenderer _rawRenderer;

        /// <summary>
        /// 渲染最终的迷雾
        /// 注意要覆盖整个场景
        /// </summary>
        MeshRenderer _finalRenderer;

        /// <summary>
        /// 存放原始迷雾的贴图
        /// </summary>
        Texture2D _fowTex;

        /// <summary>
        /// 存放视野的具体数据
        /// </summary>
        VisionGrid _visionGrid;

        /// <summary>
        /// 存放地形高度等信息
        /// </summary>
        TerrainGrid _terrainGrid;

        /// <summary>
        /// 当前战争迷雾贴图的颜色信息
        /// </summary>
        Color[] _curtColors;

        /// <summary>
        /// 战争迷雾想要过渡到的目标颜色（不直接设置目标颜色是为了有渐变的效果）
        /// </summary>
        Color[] _targetColors;

        readonly List<Vector2Int> _circleByBoundingCircle = new List<Vector2Int>();
        readonly List<Vector2Int> _lineByBresenhams = new List<Vector2Int>();

        readonly HashSet<IUnitVision> _units = new HashSet<IUnitVision>();

        public float TileSize { get; private set; } = 1f;
        public int Width { get; private set; } = 200;
        public int Height { get; private set; } = 200;

        /// <summary>
        /// 设置相机，迷雾相机会跟以此相机为同视野
        /// </summary>
        /// <param name="mainCamera">相机</param>
        /// <param name="isCenter">地图是否为中心点</param>
        public void SetCamera(Camera mainCamera, bool isCenter = true)
        {
            _mainCamera = mainCamera;
            _isCenter = isCenter;
        }
        /// <summary>
        /// 设置视野mask
        /// </summary>
        /// <param name="mask"></param>
        public void SetVisionMask(int mask)
        {
            _visionMask = mask;
        }
        /// <summary>
        /// 是否保存已走过的视野
        /// </summary>
        /// <param name="b"></param>
        public void SetWasVision(bool b, float v)
        {
            _wasVision = b;
            _wasVisionAlpha = v;
        }
        /// <summary>
        /// 迷雾更新频率（秒）
        /// </summary>
        /// <param name="v"></param>
        public void SetUpdateDuration(float v)
        {
            _updateDuration = v;
        }

        public void Init(int width, int height, float size)
        {
            if (_mainCamera == null)
            {
                Log.Error("需要先设置相机");
                return;
            }
            _init = true;
            _FogOfWar = ResMgr.Ins.LoadAsset<GameObject>(Prefab);

            Width = width;
            Height = height;
            TileSize = size;
            _visionGrid = new VisionGrid(Width, Height);
            _terrainGrid = new TerrainGrid(Width, Height);
            _targetColors = new Color[Width * Height];
            _curtColors = new Color[Width * Height];
            _fowTex = new Texture2D(Width, Height, TextureFormat.Alpha8, false);

            _InitRawRenderer();
            _InitFinalRenderer();
            _InitBlurCamera();
            _InitFOWCamera();
            TimeMgr.Ins.DoUpdate(_Update);
            EventMgr.Ins.Send(MainEventType.FOG_OF_WAR_INIT);
        }
        public void Destroy()
        {
            _init = false;
            _units.Clear();
            TimeMgr.Ins.RemoveUpdate(_Update);
            _mainCamera = null;
            if (_FogOfWar != null)
            {
                UnityPool.Push(Prefab, _FogOfWar);
                _FogOfWar = null;
            }
        }

        public void AddUnit(IUnitVision unitVision)
        {
            _units.Add(unitVision);
        }
        public void RemoveUnit(IUnitVision unitVision)
        {
            _units.Remove(unitVision);
        }


        #region 初始化
        void _InitRawRenderer()
        {
            _rawRenderer = _FogOfWar.transform.Find("RawRenderer").GetComponent<MeshRenderer>();
            _rawRenderer.sharedMaterial.mainTexture = _fowTex;
        }

        void _InitFinalRenderer()
        {
            Transform trans = _FogOfWar.transform.Find("FinalRenderer");
            _finalRenderer = trans.GetComponent<MeshRenderer>();

            //将最终的迷雾覆盖到整张地图
            trans.localScale = new Vector3(Width * TileSize, Height * TileSize, 1);
            trans.rotation = Quaternion.Euler(90, 0, 0);
            if (_isCenter)
            {
                trans.position = new Vector3(0, 1, 0);
            }
            else
            {
                trans.position = new Vector3(trans.localScale.x / 2, 1, trans.localScale.y / 2);
            }
        }

        void _InitBlurCamera()
        {
            Camera cam = _FogOfWar.transform.Find("BlurCamera").GetComponent<Camera>();

            cam.depth = _mainCamera.depth + 1;
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
            _finalRenderer.sharedMaterial.mainTexture = rt;

            var pos = _rawRenderer.transform.position;
            cam.transform.position = new Vector3(pos.x, pos.y, pos.z - 1);
            cam.orthographicSize = 0.5f;
            cam.orthographic = true;
        }

        void _InitFOWCamera()
        {
            GameObject go = new GameObject("FogOfWarCamera");
            Camera cam = go.AddComponent<Camera>();
            var cameraData = cam.GetUniversalAdditionalCameraData();
            cameraData.renderType = CameraRenderType.Overlay;

            cam.cullingMask = LayerMask.GetMask(LayerFogOfWar);
            //cam.clearFlags = CameraClearFlags.Depth;
            if (_mainCamera != null)
            {
                //cam.depth = _mainCamera.depth + 1;
                cam.fieldOfView = _mainCamera.fieldOfView;
                cam.nearClipPlane = _mainCamera.nearClipPlane;
                cam.farClipPlane = _mainCamera.farClipPlane;
                cam.rect = _mainCamera.rect;
                cam.useOcclusionCulling = false;
                cam.allowHDR = _mainCamera.allowHDR;
                cam.allowMSAA = _mainCamera.allowMSAA;

                var trans = cam.transform;
                trans.SetParent(_mainCamera.transform);
                trans.localPosition = Vector3.zero;
                trans.localScale = Vector3.one;
                trans.localRotation = Quaternion.identity;
                _mainCamera.GetUniversalAdditionalCameraData().cameraStack.Insert(0, cam);
            }
        }
        #endregion

        #region 转换
        public int Index(Vector3 vec3)
        {
            return Index(WorldPosToTilePos(vec3));
        }
        public int Index(Vector2 vec2)
        {
            return Index(WorldPosToTilePos(vec2));
        }
        public int Index(Vector2Int vecInt)
        {
            return Index(vecInt.x, vecInt.y);
        }
        public int Index(int tileX, int tileY)
        {
            return tileX + tileY * Width;
        }

        public Vector2Int WorldPosToTilePos(float worldx, float worldy, bool checkRect = false)
        {
            if (_isCenter)
            {
                worldx += Width / 2f;
                worldy += Height / 2f;
            }
            int x = Mathf.FloorToInt(worldx / TileSize);
            int y = Mathf.FloorToInt(worldy / TileSize);
            if (checkRect)
            {
                if (x < 0) x = 0; else if (x >= Width) x = Width - 1;
                if (y < 0) y = 0; else if (y >= Height) y = Height - 1;
            }
            return new Vector2Int(x, y);
        }
        public Vector2Int WorldPosToTilePos(Vector2 worldPos)
        {
            return WorldPosToTilePos(worldPos.x, worldPos.y);
        }

        public Vector2Int WorldPosToTilePos(Vector3 worldPos)
        {
            return WorldPosToTilePos(worldPos.x, worldPos.z);
        }
        #endregion

        #region 私有方法
        void _Update()
        {
            if (!_init) return;
            if (_units.Count == 0) return;

            //定时更新视野数据
            if (Time.time >= _nextUpdateTime)
            {
                _nextUpdateTime = Time.time + _updateDuration;
                _CalculateVision();
                _UpdateTargetColors(_visionMask);
            }

            //判断单位的可见性
            _UpdateVisibles();

            //平滑颜色
            _SmoothColor();
        }
        /// <summary>
        /// 计算所有单位的视野数据
        /// </summary>
        /// <param name="units">所有单位的列表</param>
        void _CalculateVision()
        {
            _visionGrid.Reset();
            foreach (var unit in _units)
            {
                Vector2Int centerTile = unit.TilePos;
                if (IsOutsideMap(centerTile))
                {
                    continue;
                }
                _CircleByBoundingCircle(centerTile, unit.Radius, TileSize);
                for (int i = 0; i < _circleByBoundingCircle.Count; i++)
                {
                    if (IsBlocked(centerTile, _circleByBoundingCircle[i], unit)) continue;
                    var index = Index(_circleByBoundingCircle[i]);
                    _visionGrid.SetVisible(index, unit.Mask);
                }
            }
        }

        /// <summary>
        /// 更新原始迷雾贴图的目标颜色
        /// </summary>
        void _UpdateTargetColors(int entityMask)
        {
            _GetCameraGroundCrossPoint(_mainCamera, out var min, out var max);

            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    int index = Index(x, y);
                    if (IsVisible(index, entityMask))
                    {
                        _targetColors[index] = new Color(0, 0, 0, 0);
                        continue;
                    }
                    if (_wasVision && WasVisible(index, entityMask))
                    {
                        _targetColors[index] = new Color(0, 0, 0, _wasVisionAlpha);
                        continue;
                    }
                    _targetColors[index] = new Color(0, 0, 0, 1);
                }
            }

            //如果在这里直接设置颜色，则移动时会很明显发现迷雾是一顿一顿的，不平滑
            //m_fowTex.SetPixels(m_targetColors);
            //m_fowTex.Apply(false);
        }

        void _SmoothColor()
        {
            for (int i = 0; i < _targetColors.Length; i++)
            {
                Color target = _targetColors[i];
                Color curt = _curtColors[i];
                _curtColors[i] = Color.Lerp(curt, target, _smoothSpeed * Time.deltaTime);
            }

            _fowTex.SetPixels(_curtColors);
            _fowTex.Apply(false);
        }

        /// <summary>
        /// 根据可见性设置渲染层级
        /// </summary>
        void _UpdateVisibles()
        {
            foreach (var unit in _units)
            {
                string layerName = IsVisible(unit) ? LayerDefault : LayerHidden;
                unit.Layer = LayerMask.NameToLayer(layerName);
            }
        }


        /// <summary>
        /// 判断格子位置是否超过地图范围
        /// </summary>
        public bool IsOutsideMap(Vector2Int tilePos)
        {
            return tilePos.x < 0 || tilePos.x >= Width ||
                        tilePos.y < 0 || tilePos.y >= Height;
        }
        public bool IsOutsideMap(Vector2 vec2)
        {
            return IsOutsideMap(WorldPosToTilePos(vec2));
        }
        public bool IsOutsideMap(Vector3 vec3)
        {
            return IsOutsideMap(WorldPosToTilePos(vec3));
        }
        public bool IsOutsideMap(int index)
        {
            return index < 0 || index >= _targetColors.Length;
        }

        /// <summary>
        /// 两点间的视野是否因为地形被阻挡了
        /// </summary>
        bool IsBlocked(Vector2Int startTile, Vector2Int targetTile, IUnitVision unit)
        {
            _LineByBresenhams(startTile, targetTile);
            for (int i = 0; i < _lineByBresenhams.Count; i++)
            {
                var index = Index(_lineByBresenhams[i]);
                var altitude = GetAltitude(index);
                var grassId = GetGrass(index);
                if (altitude > unit.Altitude)
                    return true;
                if (grassId != 0 && grassId != unit.GrassId)
                    return true;
            }

            return false;
        }

        void _LineByBresenhams(Vector2Int start, Vector2Int end)
        {
            //GC：实际项目使用时最好用Pool来存取
            _lineByBresenhams.Clear();

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
                    _lineByBresenhams.Add(new Vector2Int(x, y));

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
                    _lineByBresenhams.Add(new Vector2Int(x, y));

                    eps += dx;
                    if ((eps << 1) >= dy)
                    {
                        x += ux;
                        eps -= dy;
                    }
                }
            }
        }

        /// <summary>
        /// 找到圆形覆盖的格子
        /// </summary>
        /// <param name="centerTile">圆心的格子坐标</param>
        /// <param name="radius">圆的半径</param>
        /// <param name="tileSize">一格的大小</param>
        /// <returns>圆形覆盖的格子</returns>
        void _CircleByBoundingCircle(Vector2Int centerTile, float radius, float tileSize)
        {
            //GC：实际项目使用时最好用Pool来存取
            _circleByBoundingCircle.Clear();
            int radiusCount = Mathf.CeilToInt(radius / tileSize);
            int sqr = radiusCount * radiusCount;
            int top = centerTile.y + radiusCount;
            if (top >= Height) top = Height - 1;
            int bottom = centerTile.y - radiusCount;
            if (bottom < 0) bottom = 0;

            for (int y = bottom; y <= top; y++)
            {
                int dy = y - centerTile.y;
                int dx = Mathf.FloorToInt(Mathf.Sqrt(sqr - dy * dy));
                int left = centerTile.x - dx;
                if (left < 0) left = 0;
                int right = centerTile.x + dx;
                if (right >= Width) right = Width - 1;
                for (int x = left; x <= right; x++)
                    _circleByBoundingCircle.Add(new Vector2Int(x, y));
            }
        }

        void _GetCameraGroundCrossPoint(Camera camera, out Vector2Int min, out Vector2Int max)
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

            //获取视野与地面的交点，或是远裁剪面垂直投射到地面的交点；
            var LeftBottom = _CheckGroundSignPoint(camera, f0);
            var LeftTop = _CheckGroundSignPoint(camera, f1);
            var RightBottom = _CheckGroundSignPoint(camera, f2);
            var RightTop = _CheckGroundSignPoint(camera, f3);

            float minX = Mathf.Min(camera.transform.position.x, Mathf.Min(LeftTop.x, RightTop.x), Mathf.Min(LeftBottom.x, RightBottom.x));
            float minY = Mathf.Min(camera.transform.position.z, Mathf.Min(LeftTop.z, RightTop.z), Mathf.Min(LeftBottom.z, RightBottom.z));
            min = WorldPosToTilePos(minX, minY, true);
            float maxX = Mathf.Max(camera.transform.position.x, Mathf.Max(LeftTop.x, RightTop.x), Mathf.Max(LeftBottom.x, RightBottom.x));
            float maxY = Mathf.Max(camera.transform.position.z, Mathf.Max(LeftTop.z, RightTop.z), Mathf.Max(LeftBottom.z, RightBottom.z));
            max = WorldPosToTilePos(maxX, maxY, true);
        }

        Vector3 _CheckGroundSignPoint(Camera camera, Vector3 dri)
        {
            Vector3 cpt = camera.transform.position;
            Vector3 farPlaneNormal = camera.transform.forward;
            Vector3 farPlanePoint = camera.transform.position + (farPlaneNormal * camera.farClipPlane);

            float height = 0;

            //计算与远裁剪面的交点；
            var signPoint = _GetIntersectWithLineAndPlane(cpt, dri, farPlaneNormal, farPlanePoint);

            //这里相机先到达了远裁剪面，而没有与地面相交；
            if (signPoint.y > height)
            {
                //将远裁剪面的位置投影到地面上返回
                signPoint.y = height;
                return signPoint;
            }
            //此时被地面截断；
            Vector3 groundPoint = new Vector3(0, 0, 0);
            signPoint = _GetIntersectWithLineAndPlane(cpt, dri, Vector3.up, groundPoint);

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
        Vector3 _GetIntersectWithLineAndPlane(Vector3 point, Vector3 direct, Vector3 planeNormal, Vector3 planePoint)
        {
            float d = Vector3.Dot(planePoint - point, planeNormal) / Vector3.Dot(direct.normalized, planeNormal);
            //直线与平面的交点
            Vector3 hitPoint = (d * direct.normalized) + point;
            return hitPoint;
        }
        #endregion
    }
}
