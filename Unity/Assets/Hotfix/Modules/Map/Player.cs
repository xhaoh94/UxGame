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
        public int id;
        public string name;
        public Vector3 pos;
        public string res;
    }

    public class Player : Entity, IAwakeSystem<PlayerData>, IUnitVision
    {
        public GameObject Go { get; private set; }
        public AnimComponent Anim { get; private set; }
        public StateComponent State { get; private set; }
        public OperateComponent Operate { get; private set; }
        public SeekerComponent Seeker { get; private set; }
        public PlayableDirectorComponent Director { get; private set; }

        public void OnAwake(PlayerData a)
        {
            State = AddComponent<StateComponent>();
            Operate = AddComponent<OperateComponent>();
            Position = a.pos;
            LoadPlayer(a).Forget();
        }

        public Map Map => ParentAs<Map>();

        async UniTaskVoid LoadPlayer(PlayerData playerData)
        {
            Go = await ResMgr.Ins.LoadAssetAsync<GameObject>(playerData.res);
            SetMono(Go);
            Go.transform.position = Position;
            Go.transform.rotation = Rotation;
            Go.layer = Layer;
            Map.Camera.SetFollow(Go.transform);
            Map.Camera.SetLookAt(Go.transform);

            Anim = AddComponent<AnimComponent, Animator>(Go.GetComponentInChildren<Animator>());
            Seeker = AddComponent<SeekerComponent, Seeker>(Go.GetComponent<Seeker>());
            Director = AddComponent<PlayableDirectorComponent, PlayableDirector>(Go.GetOrAddComponent<PlayableDirector>());
            Director.SetBinding("Animation Track", Anim.Animator);
        }

        protected override void OnDestroy()
        {
            UnityPool.Push(Go);
            Map.FogOfWar?.RemoveUnit(ID);
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
            var fogOfWar = Map.FogOfWar;
            if (fogOfWar != null)
            {
                fogOfWar.TerrainGrid.GetData(_postion, out short altitude, out short grassId);
                var unitVision = GetComponent<UnitVision>();
                if (unitVision == null)
                {
                    unitVision = AddComponent<UnitVision, IUnitVision>(this);
                }
                unitVision.UpdateUnit(fogOfWar, _postion);
                fogOfWar.UpdateUnit(ID, unitVision);
            }
        }
        int _layer;
        public int Layer
        {
            get
            {
                return _layer;
            }
            set
            {
                if (_layer != value)
                {
                    _layer = value;
                    if (Go != null)
                    {
                        Go.layer = _layer;
                    }
                }
            }
        }
        #endregion
    }
}