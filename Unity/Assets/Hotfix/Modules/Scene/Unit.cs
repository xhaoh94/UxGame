using Cysharp.Threading.Tasks;
using Ux;
using System.Collections;
using Pathfinding;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ux
{
    [EEViewer]
    public class PlayerData
    {
        public bool self;
        public Pb.Entity data;
        public uint id => data.roleId;
        public Vector3 pos => new Vector3(data.Position.X, data.Position.Y, data.Position.Z);
        public int mask => data.roleMask;
        public string name;
        public string res;
    }

    public class Unit : Entity, IAwakeSystem<PlayerData>
    {
        public GameObject Model { get; private set; }
        public AnimComponent Anim => GetComponent<AnimComponent>();
        public StateComponent State => GetComponent<StateComponent>();
        public SeekerComponent Seeker => GetComponent<SeekerComponent>();
        public PathComponent Path => GetComponent<PathComponent>();
        public PlayableDirectorComponent Director => GetComponent<PlayableDirectorComponent>();

        #region Get-Set
        private Vector3 _postion;
        [EEViewer("位置")]
        public Vector3 Position
        {
            get => _postion;
            set
            {
                _postion = value;
                if (Model != null)
                {
                    Model.transform.position = _postion;
                }
                _UpdateFogOfWar();
            }
        }

        private Quaternion _rotation;
        [EEViewer("旋转")]
        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                if (Model != null)
                {
                    Model.transform.rotation = _rotation;
                }
            }
        }

        [EEViewer("是否可见")]
        public BoolValue Visable { get; private set; }
        void OnVisableChanged(bool v)
        {
            Layer = LayerMask.NameToLayer(v ? Layers.Default : Layers.Hidden);
        }

        private int _layer;
        public int Layer
        {
            get => _layer;
            set
            {
                if (_layer != value)
                {
                    _layer = value;
                    Model?.SetLayer(_layer);
                }
            }
        }
        #endregion

        PlayerData _playerData;
        public void OnAwake(PlayerData playerData)
        {
            _playerData = playerData;
            AddComponent<StateComponent>();
            AddComponent<PathComponent>();
            if (playerData.self)
            {
                AddComponent<OperateComponent>();
            }
            Visable = new BoolValue(OnVisableChanged);
            Position = _playerData.pos;
            LoadPlayer().Forget();
        }

        public Scene Map => ParentAs<Scene>();

        async UniTaskVoid LoadPlayer()
        {
            Model = await ResMgr.Ins.LoadAssetAsync<GameObject>(_playerData.res);
            SetMono(Model);
            Model.transform.position = Position;
            Model.transform.rotation = Rotation;
            Model.layer = Layer;

            if (_playerData.self)
            {
                Map.Camera.SetFollow(Model.transform);
                Map.Camera.SetLookAt(Model.transform);
            }

            AddComponent<AnimComponent, Animator>(Model.GetComponentInChildren<Animator>());
            AddComponent<SeekerComponent, Seeker>(Model.GetComponent<Seeker>());
            AddComponent<PlayableDirectorComponent, PlayableDirector>(Model.GetOrAddComponent<PlayableDirector>());
            Director.SetBinding("Animation Track", Anim.Animator);
            StateMgr.Ins.Update(ID);
        }

        protected override void OnDestroy()
        {
            UnityPool.Push(Model);
            Visable.Release();
        }



        #region fogofwar        
        void _UpdateFogOfWar()
        {
            var unitVision = GetComponent<UnitVisionComponent>();
            if (unitVision == null)
            {
                unitVision = AddComponent<UnitVisionComponent>();
            }
            unitVision.UpdateUnit();
        }
        public int Mask => _playerData.mask;
        public bool IsSelf => _playerData.self;
        #endregion
    }
}