using UnityEngine;
using UnityEngine.Rendering;

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
        private readonly UIObject _parent;
        private ModelEntity _entity;
        private SortingGroup _sortingGroup;
        private int _srcLayer;

        public UIModel(GameObject container, UIObject parent)
        {
            Init(container, parent);
            _container = container != null ? container.transform : null;
            _parent = parent;
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
            // z 朝相机偏一点：Canvas 平面在相机前方 100 处，模型比平面近才不会被裁。
            Model.transform.localPosition = new Vector3(width * 0.5f, -height, -1f);
            Model.transform.localScale = Vector3.one * scale;
            Model.transform.localEulerAngles = new Vector3(0f, angle, 0f);
            _isInLoad = isLoad;

            // 场景相机的 cullingMask 不含 UIModel 层，模型只能由 UI 相机渲染一次。
            _srcLayer = model.layer;
            model.SetLayer(LayerMask.NameToLayer(Layers.UIModel));

            _entity ??= Entity.Create<ModelEntity>();
            _entity.Name = $"UIModel@{model.name}";
            _entity.Set(model);
            SetupStageSorting(model);
        }

        /// <summary>
        /// 直挂模型的排序：SortingGroup + 父界面的 SortingLayer，order 比父界面高半个步长。
        /// 父界面重排时通过 OnStageSortingChanged 联动。
        /// </summary>
        private void SetupStageSorting(GameObject model)
        {
            _sortingGroup = model.GetComponent<SortingGroup>();
            if (_sortingGroup == null)
                _sortingGroup = model.AddComponent<SortingGroup>();
            ApplyStageSorting();
        }

        private void ApplyStageSorting()
        {
            if (_sortingGroup == null) return;
            if (_parent is not UIBase view || string.IsNullOrEmpty(view.StageSortingLayerName))
            {
                Log.Error("UIModel 需要挂在已上屏的界面（UIBase）下才能参与 UI 排序");
                return;
            }

            _sortingGroup.sortingLayerName = view.StageSortingLayerName;
            _sortingGroup.sortingOrder = view.StageSortingOrder + UIStageHandler.ModelOffset;

            // 模型默认 opaque（队列 2500 以内），会画在所有 UI 之前且不参与 SortingLayer 排序；
            // 提到 3000 进透明队列，才能跟 UI 互相穿插。
            var renderers = Model.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var mats = renderers[i].materials;
                var dirty = false;
                for (var j = 0; j < mats.Length; j++)
                {
                    if (mats[j] == null || mats[j].renderQueue >= 3000) continue;
                    mats[j].renderQueue = 3000;
                    dirty = true;
                }

                if (dirty) renderers[i].materials = mats;
            }
        }

        internal override void OnStageSortingChanged(int order)
        {
            if (_sortingGroup != null)
                _sortingGroup.sortingOrder = order + UIStageHandler.ModelOffset;
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
                Model.SetLayer(_srcLayer);
                Model.transform.SetParent(null, false);
                if (_isInLoad) UnityPool.Push(Model);
            }
            Model = null;
            _isInLoad = false;
        }
    }
}
