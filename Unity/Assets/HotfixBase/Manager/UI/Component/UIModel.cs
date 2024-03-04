using FairyGUI;
using System.Collections;
using UnityEngine;

namespace Ux
{
    public class UIModel : UIObject
    {
        public class ModelEntity : Entity
        {
            public AnimComponent Anim { get; private set; }

            public void Set(GameObject model)
            {
                if (Anim != null)
                {
                    RemoveComponent(Anim);
                }
                Anim = AddComponent<AnimComponent, Animator>(model.GetComponentInChildren<Animator>());
            }

            protected override void OnDestroy()
            {
                base.OnDestroy();
                Anim = null;
            }
        }
        public GameObject Model { get; private set; }
        public bool CloneMaterial { get; set; }
        GoWrapper _wrapper;
        bool _isInLoad;
        ModelEntity _entity;
        #region 组件

        protected virtual GGraph __container => GObject.asGraph;

        #endregion        
        public UIModel(GObject container, UIObject parent)
        {
            Init(container, parent);
            parent?.Components?.Add(this);
        }
        protected override void CreateChildren()
        {

        }

        protected override void OnHide()
        {
            base.OnHide();
            _Release();
        }

        public UIModel Load(string location, float angle = 180, float scale = 180)
        {
            var model = ResMgr.Ins.LoadAsset<GameObject>(location);
            _Set(model, true, angle, scale);
            return this;
        }
        public UIModel Set(GameObject model, float angle = 180, float scale = 180)
        {
            _Set(model, false, angle, scale);
            return this;
        }
        void _Set(GameObject model, bool isLoad, float angle, float scale)
        {
            if (__container == null)
            {
                Log.Error("UIModel:没有存储模型的容器");
                return;
            }
            if (model == null)
            {
                Log.Error("UIModel:模型为null");
                return;
            }
            Model = model;
            Model.transform.localPosition = new Vector3(GObject.width * 0.5f, -GObject.height, 1000);
            Model.transform.localScale = new Vector3(scale, scale, scale);
            Model.transform.localEulerAngles = new Vector3(0, angle, 0);
            if (_wrapper == null)
            {
                _wrapper = new GoWrapper();
            }
            else
            {
                if (_wrapper.wrapTarget != null && _isInLoad)
                {
                    UnityPool.Push(_wrapper.wrapTarget);
                }
            }
            _wrapper.SetWrapTarget(model, CloneMaterial);

            _isInLoad = isLoad;
            __container.SetNativeObject(_wrapper);

            if (_entity == null)
            {
                _entity = Entity.Create<ModelEntity>(true);
            }
            _entity.Name = $"UIModel@{model.name}";
            _entity.Set(model);
        }

        public void Refresh()
        {
            if (_wrapper == null) return;
            if (_wrapper.wrapTarget == null) return;
            _wrapper.CacheRenderers();
        }

        public void Play(string Anim)
        {
            if (_entity == null) return;

            var clip = ResMgr.Ins.LoadAsset<AnimationClip>(Anim);
            _entity.Anim.AddAnimation(Anim, clip);
            _entity.Anim.Play(Anim, 0.3f);
        }

        void _Release()
        {
            _entity?.Destroy();
            _entity = null;

            if (_wrapper == null) return;
            if (_wrapper.wrapTarget == null) return;
            var temGo = _wrapper.wrapTarget;
            _wrapper.wrapTarget = null;
            if (_isInLoad)
            {
                UnityPool.Push(temGo);
                _isInLoad = false;
            }
        }
    }
}