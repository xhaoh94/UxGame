using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Ux.UIModel;

namespace Ux
{
    public class RTModel : UIObject
    {
        public GameObject Model { get; private set; }
        public Camera Camera { get; private set; }

        private RawImage _image;
        private Transform _root;
        private RenderTexture _renderTexture;
        private int _width;
        private int _height;
        private int _posz = -1;
        private bool _isInLoad;
        private int _srcLayer;
        private string _curAnim;
        private ModelEntity _entity;

        private static Stack<int> _queue;
        private static int GetPosZ()
        {
            _queue ??= new Stack<int>();
            if (_queue.Count == 0)
                for (var i = 100; i >= 0; i--) _queue.Push(i * 30);
            return _queue.Count > 0 ? _queue.Pop() : 0;
        }

        public RTModel(GameObject container, UIObject parent)
        {
            Init(container, parent);
            parent?.AddComponent(this);
            var rect = container != null ? container.transform as RectTransform : null;
            _width = Mathf.Max(1, rect != null ? Mathf.RoundToInt(rect.rect.width) : 1);
            _height = Mathf.Max(1, rect != null ? Mathf.RoundToInt(rect.rect.height) : 1);
            _image = container != null ? container.GetComponent<RawImage>() : null;
            OnHideCallback += ReleaseModel;
        }

        public RTModel Load(string location, float angle = 180, float scale = 1)
        {
            var model = ResMgr.Ins.LoadAsset<GameObject>(location);
            if (State is UIState.Hide or UIState.HideAnim)
            {
                if (model != null) UnityPool.Push(model);
                return this;
            }
            SetInternal(model, true, angle, scale);
            return this;
        }

        public RTModel Set(GameObject model, float angle = 180, float scale = 1)
        {
            if (State is UIState.Hide or UIState.HideAnim)
                return this;
            SetInternal(model, false, angle, scale);
            return this;
        }

        private void SetInternal(GameObject model, bool isLoad, float angle, float scale)
        {
            if (model == null) return;
            CreateCamera();
            CreateTexture();
            if (Model != null && _isInLoad) UnityPool.Push(Model);

            Model = model;
            _isInLoad = isLoad;
            _srcLayer = model.layer;
            model.transform.SetParent(_root, false);
            model.transform.localPosition = new Vector3(0f, -1f, 5f);
            model.transform.localScale = Vector3.one * scale;
            model.transform.localEulerAngles = new Vector3(0f, angle, 0f);
            model.SetLayer(LayerMask.NameToLayer(Layers.RTModel));

            _entity ??= Entity.Create<ModelEntity>();
            _entity.Name = $"RTModel@{model.name}";
            _entity.Set(model);
        }

        private void CreateCamera()
        {
            if (Camera != null) return;
            var prefab = ResMgr.Ins.LoadAsset<GameObject>(string.Format(PathHelper.Res.Prefab, "RTModelCamera"));
            Camera = prefab.GetComponent<Camera>();
            _posz = GetPosZ();
            Camera.transform.position = new Vector3(0f, 1000f, _posz);
            Camera.enabled = false;
            Object.DontDestroyOnLoad(Camera.gameObject);
            _root = prefab.transform;
        }

        private void CreateTexture()
        {
            if (_renderTexture != null) return;
            _renderTexture = RenderTexture.GetTemporary(_width, _height, 24, RenderTextureFormat.ARGB32);
            _renderTexture.antiAliasing = 1;
            _renderTexture.filterMode = FilterMode.Bilinear;
            if (_image != null) _image.texture = _renderTexture;
            GameMethod.Update += Render;
            Render();
        }

        private void ReleaseModel()
        {
            GameMethod.Update -= Render;
            _entity?.Destroy();
            _entity = null;
            if (Camera != null)
            {
                UnityPool.Push(Camera.gameObject);
                Camera = null;
            }
            if (Model != null)
            {
                Model.SetLayer(_srcLayer);
                if (_isInLoad) UnityPool.Push(Model);
            }
            Model = null;
            _isInLoad = false;
            if (_posz >= 0) { _queue?.Push(_posz); _posz = -1; }
            if (_renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(_renderTexture);
                _renderTexture = null;
            }
            if (_image != null) _image.texture = null;
            _curAnim = null;
        }

        public void Play(string animation)
        {
            if (_entity?.Anim == null || string.IsNullOrEmpty(animation)) return;
            if (!_entity.Anim.Has(animation))
                _entity.Anim.AddAnimation(animation, ResMgr.Ins.LoadAsset<AnimationClip>(animation));
            if (_curAnim == animation) return;
            _curAnim = animation;
            _entity.Anim.Play(animation, 0.3f);
        }

        private void Render()
        {
            if (Camera == null || _renderTexture == null) return;
            Camera.targetTexture = _renderTexture;
            var old = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            GL.Clear(true, true, Color.clear);
            Camera.Render();
            RenderTexture.active = old;
        }
    }
}
