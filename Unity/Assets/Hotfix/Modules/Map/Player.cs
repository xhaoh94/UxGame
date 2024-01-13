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

    public class Player : Entity, IAwakeSystem<PlayerData>, IUnitVisionEntity
    {
        public GameObject Go { get; private set; }
        public AnimComponent Anim { get; private set; }
        public StateComponent State { get; private set; }
        public OperateComponent Operate { get; private set; }
        public SeekerComponent Seeker { get; private set; }
        public PlayableDirectorComponent Director { get; private set; }

        PlayerData _playerData;
        public void OnAwake(PlayerData playerData)
        {
            _playerData = playerData;
            State = AddComponent<StateComponent>();
            if (_playerData.self)
            {
                Operate = AddComponent<OperateComponent>();
            }
            Position = _playerData.pos;
            LoadPlayer().Forget();
        }

        public Map Map => ParentAs<Map>();

        async UniTaskVoid LoadPlayer()
        {
            Go = await ResMgr.Ins.LoadAssetAsync<GameObject>(_playerData.res);
            SetMono(Go);
            Go.transform.position = Position;
            Go.transform.rotation = Rotation;
            Go.layer = Layer;

            if (_playerData.self)
            {
                Map.Camera.SetFollow(Go.transform);
                Map.Camera.SetLookAt(Go.transform);
            }

            Anim = AddComponent<AnimComponent, Animator>(Go.GetComponentInChildren<Animator>());
            Seeker = AddComponent<SeekerComponent, Seeker>(Go.GetComponent<Seeker>());
            Director = AddComponent<PlayableDirectorComponent, PlayableDirector>(Go.GetOrAddComponent<PlayableDirector>());
            Director.SetBinding("Animation Track", Anim.Animator);
        }

        protected override void OnDestroy()
        {
            UnityPool.Push(Go);
            Anim = null;
            State = null;
            Operate = null;
            Seeker = null;
            Director = null;
        }

        private Vector3 _postion;
        [EEViewer("位置")]
        public Vector3 Position
        {
            get => _postion;
            set
            {
                _postion = value;
                if (Go != null)
                {
                    Go.transform.position = _postion;
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
                if (Go != null)
                {
                    Go.transform.rotation = _rotation;
                }
            }
        }
        #region fogofwar        
        void _UpdateFogOfWar()
        {
            var unitVision = GetComponent<UnitVisionComponent>();
            if (unitVision == null)
            {
                unitVision = AddComponent<UnitVisionComponent, IUnitVisionEntity, PlayerData>(this, _playerData);
            }
            unitVision.UpdateUnit();
        }
        int _layer;
        public int Layer
        {
            get => _layer;
            set
            {
                if (_layer != value)
                {
                    _layer = value;
                    Go?.SetLayer(_layer);
                }
            }
        }
        public int Mask => _playerData.mask;
        #endregion
    }
}