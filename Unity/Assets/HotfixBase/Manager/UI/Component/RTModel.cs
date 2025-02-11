﻿using FairyGUI;
using System.Collections.Generic;
using UnityEngine;
using static Ux.UIModel;

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
        int _posz = -1;
        bool _isInLoad;
        int _srcLayer;
        string _curAnim;
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

            _image = new Image();
            __container.SetNativeObject(_image);

            OnHideCallBack += _Release;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            if (_image != null)
            {
                _image.Dispose();
                _image = null;
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
            switch (State)
            {
                case UIState.Hide:
                case UIState.HideAnim:
                    return this;
            }
            return _Set(model, false, angle, scale);
        }
        RTModel _Set(GameObject model, bool isLoad, float angle, float scale)
        {
            _CreateCamera();
            _CreateTexture();

            if (Model != null && _isInLoad)
            {
                UnityPool.Push(Model);
            }

            _isInLoad = isLoad;
            Model = model;
            _srcLayer = model.layer;
            model.transform.localPosition = new Vector3(0, -1f, 5);
            model.transform.localScale = new Vector3(scale, scale, scale);
            model.transform.localEulerAngles = new Vector3(0, angle, 0);
            model.SetParent(_root, false);
            model.SetLayer(LayerMask.NameToLayer(Layers.RTModel));


            if (_entity == null)
            {
                _entity = Entity.Create<ModelEntity>();
            }
            _entity.Name = $"RTModel@{model.name}";
            _entity.Set(model);

            //if (!string.IsNullOrEmpty(_curAnim))
            //{
            //    Play(_curAnim);
            //}
            return this;
        }
        void _CreateCamera()
        {
            if (Camera != null) return;
            var go = ResMgr.Ins.LoadAsset<GameObject>(string.Format(PathHelper.Res.Prefab, "RTModelCamera"));
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

            _image.texture = new NTexture(_renderTexture);

            Timers.inst.AddUpdate(_Render);
            _Render();

        }
        void _Release()
        {
            Timers.inst.Remove(_Render);

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
                _queue?.Push(_posz);
                _posz = -1;
            }

            if (_renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(_renderTexture);
                _renderTexture = null;
            }

            if (_image != null)
            {
                _image.texture = null;
            }

            _curAnim = null;
        }

        public void Play(string anim)
        {
            if (_entity == null) return;
            if (string.IsNullOrEmpty(anim)) return;

            if (!_entity.Anim.Has(anim))
            {
                var clip = ResMgr.Ins.LoadAsset<AnimationClip>(anim);
                _entity.Anim.AddAnimation(anim, clip);
            }
            else if (_curAnim == anim) return;

            _curAnim = anim;
            _entity.Anim.Play(anim, 0.3f);
        }


        void _Render(object param = null)
        {
            Camera.targetTexture = _renderTexture;
            RenderTexture old = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            GL.Clear(true, true, Color.clear);
            Camera.Render();
            RenderTexture.active = old;
        }


    }
}
