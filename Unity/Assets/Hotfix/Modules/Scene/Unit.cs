using Cysharp.Threading.Tasks;
using Pathfinding;
using UnityEngine;
using UnityEngine.Playables;

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
        public GameObject Root { get; private set; }
        public AnimComponent Anim => Get<AnimComponent>();
        public StateComponent State => Get<StateComponent>();
        public SeekerComponent Seeker => Get<SeekerComponent>();
        public PathComponent Path => Get<PathComponent>();
        public PlayableDirectorComponent Director => Get<PlayableDirectorComponent>();

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
                    Root?.SetLayer(_layer);
                }
            }
        }
        #endregion

        PlayerData _playerData;
        public void OnAwake(PlayerData playerData)
        {
            _playerData = playerData;
            Add<StateComponent>();
            Add<PathComponent>();
            if (playerData.self)
            {
                Add<OperateComponent>();
            }
            Visable = new BoolValue(OnVisableChanged);
            Position = _playerData.pos;
            Root = new GameObject();
            LinkModel(Root);
            LoadPlayer().Forget();
        }

        public Scene Map => ParentAs<Scene>();

        async UniTaskVoid LoadPlayer()
        {
            var model = await ResMgr.Ins.LoadAssetAsync<GameObject>(_playerData.res);
            model.SetParent(Root.transform);
            model.transform.position = Position;
            model.transform.rotation = Rotation;
            model.layer = Layer;

            if (_playerData.self)
            {
                Map.Camera.SetFollow(Model.transform);
                //Map.Camera.SetLookAt(Model.transform);
            }

            Add<AnimComponent, Animator>(Model.GetComponentInChildren<Animator>());
            Add<SeekerComponent, Seeker>(Model.GetComponent<Seeker>());
            Add<PlayableDirectorComponent, PlayableDirector>(model.GetOrAddComponent<PlayableDirector>());
            Director.SetBinding("Anim Track", Model.GetComponentInChildren<Animator>());
            StateMgr.Ins.Update(ID);
        }

        protected override void OnDestroy()
        {
            UnityPool.Push(Root);
            Visable.Release();
        }



        #region fogofwar        
        void _UpdateFogOfWar()
        {
            var unitVision = Get<UnitVisionComponent>();
            if (unitVision == null)
            {
                unitVision = Add<UnitVisionComponent>();
            }
            unitVision.UpdateUnit();
        }
        public int Mask => _playerData.mask;
        public bool IsSelf => _playerData.self;
        #endregion
    }
}