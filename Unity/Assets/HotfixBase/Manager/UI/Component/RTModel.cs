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
        RenderTexture _renderTexture;
        int _width;
        int _height;
        int _posz;
        bool _isInLoad;
        int _srcLayer;
        ModelEntity _entity;

        const int GAP = 30;
        static Stack<int> _queue;
        static int GetPosZ()
        {
            if (_queue == null)
            {
                _queue = new Stack<int>();
                for (int i = 100; i >= 0; i--)
                {
                    _queue.Push(i * 30);
                }
            }
            if (_queue.Count == 0)
            {
                Log.Warning("GetPosZ超出预存范围！");
                return 0;
            }
            return _queue.Pop();
        }

        //const int S_LAYER = 10;
        //const int E_LAYER = 29;
        //static Queue<int> _queue;
        //static int GetLyaer()
        //{
        //    if (_queue == null)
        //    {
        //        _queue = new Queue<int>();
        //        for (int i = S_LAYER; i <= E_LAYER; i++)
        //        {
        //            _queue.Enqueue(i);
        //        }
        //    }
        //    if (_queue.Count == 0)
        //    {
        //        Log.Warning("Layer超出预存范围！");
        //        return 0;
        //    }
        //    return _queue.Dequeue();
        //}

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

        public RTModel Load(string location, float angle = 180, float scale = 1)
        {
            var model = ResMgr.Ins.LoadAsset<GameObject>(location);
            switch (State)
            {
                case UIState.Hide:
                case UIState.HideAnim:
                    UnityPool.Push(model);
                    return this;
            }
            return _Set(model, true, angle, scale);
        }
        public RTModel Set(GameObject model, float angle = 180, float scale = 1)
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
            _srcLayer = model.layer;
            model.transform.localPosition = new Vector3(0, -1f, 5);
            model.transform.localScale = new Vector3(scale, scale, scale);
            model.transform.localEulerAngles = new Vector3(0, angle, 0);
            model.SetParent(this._root, false);
            model.SetLayer(LayerMask.NameToLayer(Layers.RTModel));


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
            var go = ResMgr.Ins.LoadAsset<GameObject>("RTModelCamera");
            Camera = go.GetComponent<Camera>();
            _posz = GetPosZ();
            Camera.transform.position = new Vector3(0, 1000, _posz);
            Camera.enabled = false;
            Object.DontDestroyOnLoad(Camera.gameObject);
            _root = go.transform;
        }
        void _CreateTexture()
        {
            if (_renderTexture != null)
                return;

            _renderTexture = RenderTexture.GetTemporary(_width, _height, 24, RenderTextureFormat.ARGB32);
            _renderTexture.antiAliasing = 1;
            _renderTexture.filterMode = FilterMode.Bilinear;
            _renderTexture.anisoLevel = 0;
            _renderTexture.useMipMap = false;

            this._image.texture = new NTexture(_renderTexture);

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

            Model?.SetLayer(_srcLayer);
            if (_isInLoad)
            {
                UnityPool.Push(Model);
                _isInLoad = false;
            }
            Model = null;

            if (_posz >= -1)
            {
                _queue.Push(_posz);
                _posz = -1;
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
