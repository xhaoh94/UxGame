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

    public class Player : Entity, IAwakeSystem<PlayerData>
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
            Postion = a.pos;
            LoadPlayer(a).Forget();
        }

        public Map Map => Parent as Map;

        async UniTaskVoid LoadPlayer(PlayerData playerData)
        {
            Go = await ResMgr.Ins.LoadAssetAsync<GameObject>(playerData.res);            
            SetMono(Go);            
            Go.transform.position = Postion;
            Go.transform.rotation = Rotation;
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
            Anim = null;
            State = null;
            Operate = null;
            Seeker = null;
            Director = null;
        }

        private Vector3 _postion;
        [EEViewer("位置")]
        public Vector3 Postion
        {
            get => _postion;
            set
            {
                _postion = value;
                if (Go != null)
                {
                    Go.transform.position = _postion;
                }
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
    }
}