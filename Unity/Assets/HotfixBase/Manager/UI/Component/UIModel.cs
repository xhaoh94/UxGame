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
                if (Anim != null) Remove(Anim);
                Anim = Add<AnimComponent, Animator>(model != null ? model.GetComponentInChildren<Animator>() : null);
            }
            protected override void OnDestroy()
            {
                base.OnDestroy();
                Anim = null;
            }
        }

        public GameObject Model { get; private set; }
        public bool CloneMaterial { get; set; }
        private bool _isInLoad;
        private readonly Transform _container;
        private ModelEntity _entity;

        public UIModel(GameObject container, UIObject parent)
        {
            Init(container, parent);
            _container = container != null ? container.transform : null;
            parent?.AddComponent(this);
            OnHideCallback += ReleaseModel;
        }

        public UIModel Load(string location, float angle = 180, float scale = 180)
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

        public UIModel Set(GameObject model, float angle = 180, float scale = 180)
        {
            if (State is UIState.Hide or UIState.HideAnim)
                return this;
            SetInternal(model, false, angle, scale);
            return this;
        }

        private void SetInternal(GameObject model, bool isLoad, float angle, float scale)
        {
            if (_container == null || model == null)
            {
                Log.Error("UIModel 容器或模型为空");
                return;
            }

            if (Model != null && _isInLoad)
                UnityPool.Push(Model);

            Model = model;
            Model.transform.SetParent(_container, false);
            var rect = GameObject.transform as RectTransform;
            var width = rect != null ? rect.rect.width : 0f;
            var height = rect != null ? rect.rect.height : 0f;
            Model.transform.localPosition = new Vector3(width * 0.5f, -height, 0f);
            Model.transform.localScale = Vector3.one * scale;
            Model.transform.localEulerAngles = new Vector3(0f, angle, 0f);
            _isInLoad = isLoad;

            _entity ??= Entity.Create<ModelEntity>();
            _entity.Name = $"UIModel@{model.name}";
            _entity.Set(model);
        }

        public void Refresh() { }

        public void Play(string animation)
        {
            if (_entity?.Anim == null || string.IsNullOrEmpty(animation)) return;
            var clip = ResMgr.Ins.LoadAsset<AnimationClip>(animation);
            _entity.Anim.AddAnimation(animation, clip);
            _entity.Anim.Play(animation, 0.3f);
        }

        private void ReleaseModel()
        {
            _entity?.Destroy();
            _entity = null;
            if (Model != null)
            {
                Model.transform.SetParent(null, false);
                if (_isInLoad) UnityPool.Push(Model);
            }
            Model = null;
            _isInLoad = false;
        }
    }
}
