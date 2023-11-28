using Cysharp.Threading.Tasks;
using Ux;
using System.Collections;
using Pathfinding;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ux
{
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
        PlayerData playerData;
        public AnimComponent Anim { get; private set; }
        public StateComponent State { get; private set; }
        public OperateComponent Operate { get; private set; }
        public SeekerComponent Seeker { get; private set; }
        public PlayableDirectorComponent Director { get; private set; }

        public void OnAwake(PlayerData a)
        {
            playerData = a;
            State = AddComponent<StateComponent>();
            Operate = AddComponent<OperateComponent>();
            Postion = a.pos;
            LoadPlayer().Forget();
        }

        public Map Map => Parent as Map;

        async UniTaskVoid LoadPlayer()
        {
            Go = await ResMgr.Ins.LoadAssetAsync<GameObject>(playerData.res);
            Go.transform.SetParent(Go.transform);
            Go.transform.position = Postion;
            Go.transform.rotation = Rotation;
            Map.Camera.SetFollow(Go.transform);
            Map.Camera.SetLookAt(Go.transform);
            SetMono(Go);

            Anim = AddComponent<AnimComponent, Animator>(Go.GetComponentInChildren<Animator>());
            Seeker = AddComponent<SeekerComponent, Seeker>(Go.GetComponent<Seeker>());
            Director = AddComponent<PlayableDirectorComponent, PlayableDirector>(Go.GetOrAddComponent<PlayableDirector>());
            Director.SetBinding("Animation Track", Anim.Animator);
        }

        protected override void OnDestroy()
        {
            UnityEngine.Object.Destroy(Go);
            playerData = null;
        }

        private Vector3 _postion;
        [EntityViewer("位置")]
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
        [EntityViewer("旋转")]
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