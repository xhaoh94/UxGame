using Cysharp.Threading.Tasks;
using Pathfinding;
using UnityEngine;
using UnityEngine.Playables;
using static UnityEditor.FilePathAttribute;

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
        const string _ecs_root_pool = "_$ecs_root_pool$_";        
        public GameObject Model { get; private set; }
        public AnimComponent Anim => Get<AnimComponent>();
        public TimelineComponent Timeline => Get<TimelineComponent>();
        public StateComponent State => Get<StateComponent>();
        public SeekerComponent Seeker => Get<SeekerComponent>();
        public PathComponent Path => Get<PathComponent>();
        //public PlayableDirectorComponent Director => Get<PlayableDirectorComponent>();

        #region Get-Set
        private Vector3 _postion;
        [EEViewer("位置")]
        public Vector3 Position
        {
            get => _postion;
            set
            {
                _postion = value;
                if (Viewer != null)
                {
                    Viewer.transform.position = _postion;
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
                if (Viewer != null)
                {
                    Viewer.transform.rotation = _rotation;
                }
            }
        }

        [EEViewer("是否可见")]
        public BoolValue Visible { get; private set; }
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
                    Viewer.gameObject.SetLayer(_layer);
                }
            }
        }
        #endregion

        PlayerData _playerData;
        public void OnAwake(PlayerData playerData)
        {
            _playerData = playerData;
            var root = UnityPool.Get(_ecs_root_pool, () => new GameObject());
            root.name = _playerData.name;
            if (_playerData.self)
            {
                Map.Camera.SetFollow(root.transform);
                //Map.Camera.SetLookAt(root.transform);
            }
            Link(root);

            Add<StateComponent>();
            Add<TimelineComponent>();
            Add<PathComponent>();
            if (playerData.self)
            {
                Add<OperateComponent>();
            }
            Visible = new BoolValue(OnVisableChanged);          
            
           
            LoadModel().Forget();
            Position = _playerData.pos;
        }

        public Scene Map => ParentAs<Scene>();

        async UniTaskVoid LoadModel()
        {
            if (Model != null)
            {
                UnityPool.Push(Model);
            }
            Model = await ResMgr.Ins.LoadAssetAsync<GameObject>(_playerData.res);
            Model.SetParent(Viewer.transform);
            Model.layer = Layer;
 
            Add<AnimComponent, Animator>(Model.GetComponentInChildren<Animator>());
            Add<SeekerComponent, Seeker>(Model.GetComponent<Seeker>());
            //Add<PlayableDirectorComponent, PlayableDirector>(Model.GetOrAddComponent<PlayableDirector>());
            //Director.SetBinding("Anim Track", Viewer.GetComponentInChildren<Animator>());
            StateMgr.Ins.Update(ID);
        }

        protected override void OnDestroy()
        {
            UnityPool.Push(_ecs_root_pool,Viewer.gameObject);
            Visible.Release();
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