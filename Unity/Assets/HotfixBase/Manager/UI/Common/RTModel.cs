using FairyGUI;
using FairyGUI.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Ux.UIModel;
using NativeBlendMode = UnityEngine.Rendering.BlendMode;

namespace Ux
{
    public class RTModel : UIObject
    {
        public GameObject Model { get; private set; }
        public Camera Camera { get; private set; }

        Image _image;
        Transform _root;
        Transform _modelRoot;
        Transform _background;
        RenderTexture _renderTexture;
        int _width;
        int _height;
        int _layer;

        bool _isInLoad;
        ModelEntity _entity;

        const int S_LAYER = 10;
        const int E_LAYER = 29;
        static Queue<int> _queue;
        static int GetLyaer()
        {
            if (_queue == null)
            {
                _queue = new Queue<int>();
                for (int i = S_LAYER; i <= E_LAYER; i++)
                {
                    _queue.Enqueue(i);
                }
            }
            if (_queue.Count == 0)
            {
                Log.Warning("Layer超出预存范围！");
                return 0;
            }
            return _queue.Dequeue();
        }

        #region 组件

        protected virtual GGraph __container => GObject.asGraph;

        #endregion 
        public RTModel(GObject container, UIObject parent)
        {
            Init(container, parent);
            parent?.Components?.Add(this);

            _width = (int)container.width;
            _height = (int)container.height;

            this._image = new Image();
            __container.SetNativeObject(this._image);
        }

        protected override void OnHide()
        {
            base.OnHide();
            _Release();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            if (this._image != null)
            {
                this._image.Dispose();
                this._image = null;
            }
        }

        public void SetBackground(GObject image)
        {
            SetBackground(image, null);
        }

        public void SetBackground(GObject image1, GObject image2)
        {
            Image source1 = (Image)image1.displayObject;
            Image source2 = image2 != null ? (Image)image2.displayObject : null;

            Vector3 pos = _background.position;
            pos.z = Camera.farClipPlane;
            _background.position = pos;

            Vector2[] uv = new Vector2[4];
            Vector2[] uv2 = null;

            Rect rect = _image.TransformRect(new Rect(0, 0, this._width, this._height), source1);
            Rect uvRect = GetImageUVRect(source1, rect, uv);

            if (source2 != null)
            {
                rect = _image.TransformRect(new Rect(0, 0, this._width, this._height), source2);
                uv2 = new Vector2[4];
                GetImageUVRect(source2, rect, uv2);
            }

            Vector3[] vertices = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector2 v = uv[i];
                vertices[i] = new Vector3((v.x - uvRect.x) / uvRect.width * 2 - 1,
                    (v.y - uvRect.y) / uvRect.height * 2 - 1, 0);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uv;
            if (uv2 != null)
                mesh.uv2 = uv2;
            mesh.colors32 = new Color32[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new int[] { 0, 1, 2, 2, 3, 0 };

            MeshFilter meshFilter = this._background.gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = this._background.gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer meshRenderer = this._background.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = this._background.gameObject.AddComponent<MeshRenderer>();
#if (UNITY_5 || UNITY_5_3_OR_NEWER)
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#else
        meshRenderer.castShadows = false;
#endif
            meshRenderer.receiveShadows = false;
            Shader shader = Shader.Find("Game/FullScreen");
            Material mat = new Material(shader);
            mat.mainTexture = source1.texture.nativeTexture;
            if (source2 != null)
                mat.SetTexture("_Tex2", source2.texture.nativeTexture);
            meshRenderer.material = mat;
        }

        Rect GetImageUVRect(Image image, Rect localRect, Vector2[] uv)
        {
            Rect imageRect = new Rect(0, 0, image.size.x, image.size.y);
            Rect bound = ToolSet.Intersection(ref imageRect, ref localRect);
            Rect uvRect = image.texture.uvRect;

            if (image.scale9Grid != null)
            {
                Rect gridRect = (Rect)image.scale9Grid;
                float sourceW = image.texture.width;
                float sourceH = image.texture.height;
                uvRect = Rect.MinMaxRect(Mathf.Lerp(uvRect.xMin, uvRect.xMax, gridRect.xMin / sourceW),
                    Mathf.Lerp(uvRect.yMin, uvRect.yMax, (sourceH - gridRect.yMax) / sourceH),
                    Mathf.Lerp(uvRect.xMin, uvRect.xMax, gridRect.xMax / sourceW),
                    Mathf.Lerp(uvRect.yMin, uvRect.yMax, (sourceH - gridRect.yMin) / sourceH));

                float vw = imageRect.width - (sourceW - gridRect.width);
                float vh = imageRect.height - (sourceH - gridRect.height);
                uvRect = Rect.MinMaxRect(Mathf.Lerp(uvRect.xMin, uvRect.xMax, (bound.x - gridRect.x) / vw),
                    Mathf.Lerp(uvRect.yMin, uvRect.yMax, (imageRect.height - bound.yMax - (sourceH - gridRect.yMax)) / vh),
                     Mathf.Lerp(uvRect.xMin, uvRect.xMax, (bound.xMax - gridRect.x) / vw),
                     Mathf.Lerp(uvRect.yMin, uvRect.yMax, (imageRect.height - bound.yMin - gridRect.y) / vh));
            }
            else
            {
                uvRect = Rect.MinMaxRect(Mathf.Lerp(uvRect.xMin, uvRect.xMax, bound.xMin / imageRect.width),
                    Mathf.Lerp(uvRect.yMin, uvRect.yMax, (imageRect.height - bound.yMax) / imageRect.height),
                    Mathf.Lerp(uvRect.xMin, uvRect.xMax, bound.xMax / imageRect.width),
                    Mathf.Lerp(uvRect.yMin, uvRect.yMax, (imageRect.height - bound.yMin) / imageRect.height));
            }

            uv[0] = uvRect.position;
            uv[1] = new Vector2(uvRect.xMin, uvRect.yMax);
            uv[2] = new Vector2(uvRect.xMax, uvRect.yMax);
            uv[3] = new Vector2(uvRect.xMax, uvRect.yMin);

            if (image.texture.rotated)
                ToolSet.RotateUV(uv, ref image.texture.uvRect);

            return uvRect;
        }

        public RTModel Load(string location, float angle = 180, float scale = 1)
        {
            var model = ResMgr.Ins.LoadAsset<GameObject>(location);
            return _Set(model, true, angle, scale);
        }
        public RTModel Set(GameObject model, float angle=180, float scale=1)
        {
            return _Set(model, false, angle, scale);
        }
        public RTModel _Set(GameObject model, bool isLoad, float angle, float scale)
        {
            _CreateCamera();
            _CreateTexture();

            if (Model != null && _isInLoad)
            {
                UnityPool.Push(Model);
            }

            _isInLoad = isLoad;
            this.Model = model;

            model.transform.localPosition = new Vector3(0, -0.5f, 5);
            model.transform.localScale = new Vector3(scale, scale, scale);
            model.transform.localEulerAngles = new Vector3(0, angle, 0);
            model.SetParent(this._modelRoot, false);

            _root.gameObject.SetLayer(_layer);


            if (_entity == null)
            {
                _entity = Entity.Create<ModelEntity>(true);
            }
            _entity.Name = $"RTModel@{model.name}";
            _entity.Set(model);

            return this;
        }
        void _CreateCamera()
        {
            if (Camera != null) return;

            _layer = GetLyaer();

            var go = ResMgr.Ins.LoadAsset<GameObject>("RTModelCamera");
            Camera = go.GetComponent<Camera>();
            Camera.transform.position = new Vector3(0, 1000, 0);
            Camera.cullingMask = 1 << _layer;
            Camera.enabled = false;

            Object.DontDestroyOnLoad(Camera.gameObject);

            _root = go.transform.Find("RTModel");
            if (_root == null)
            {
                _root = new GameObject("RTModel").transform;
                _root.SetParent(go.transform, false);
            }
            _modelRoot = _root.Find("modelRoot");
            if (_modelRoot == null)
            {
                _modelRoot = new GameObject("modelRoot").transform;
                _modelRoot.SetParent(_root, false);
            }

            _background = _root.Find("background");
            if (_background == null)
            {
                _background = new GameObject("background").transform;
                _background.SetParent(_root, false);

            }
        }
        void _CreateTexture()
        {
            if (_renderTexture != null)
                return;

            _renderTexture = RenderTexture.GetTemporary(_width, _height, 24, RenderTextureFormat.ARGB32);
            _renderTexture.antiAliasing = 1;
            _renderTexture.filterMode= FilterMode.Bilinear;
            _renderTexture.anisoLevel = 0;
            _renderTexture.useMipMap = false;

            this._image.texture = new NTexture(_renderTexture);
            this._image.blendMode = BlendMode.Off;
            //BlendModeUtils.Override(BlendMode.Custom1, NativeBlendMode.One, NativeBlendMode.OneMinusSrcAlpha);
            //this._image.blendMode = BlendMode.Custom1;

            Timers.inst.AddUpdate(this.Render);
            Render();

        }
        void _Release()
        {
            Timers.inst.Remove(this.Render);

            _entity?.Destroy();
            _entity = null;

            if (Camera != null)
            {
                UnityPool.Push(Camera.gameObject);
                Camera = null;
            }

            var temGo = Model;
            if (_isInLoad)
            {
                UnityPool.Push(Model);
                _isInLoad = false;
            }
            Model = null;

            if (_layer != -1)
            {
                _queue.Enqueue(_layer);
                _layer = -1;
            }

            if (_renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(_renderTexture);
                _renderTexture = null;
            }

            if (this._image != null)
            {
                this._image.texture = null;
            }
            
        }

        public void Play(string Anim)
        {
            if (_entity == null) return;

            var clip = ResMgr.Ins.LoadAsset<AnimationClip>(Anim);
            _entity.Anim.AddAnimation(Anim, clip);
            _entity.Anim.Play(Anim, 0.3f);
        }


        void Render(object param = null)
        {            
            Camera.targetTexture = this._renderTexture;
            RenderTexture old = RenderTexture.active;
            RenderTexture.active = this._renderTexture;
            GL.Clear(true, true, Color.clear);
            Camera.Render();
            RenderTexture.active = old;
        }


    }
}
