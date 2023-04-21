using Cysharp.Threading.Tasks;
using Ux;
using System.Collections;
using Pathfinding;
using UnityEngine;

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

        public void OnAwake(PlayerData a)
        {
            playerData = a;
            State = AddComponent<StateComponent>();
            Operate = AddComponent<OperateComponent>();
            LoadPlayer().Forget();
        }

        public Map Map => Parent as Map;

        async UniTaskVoid LoadPlayer()
        {
            var handle = ResMgr.Instance.LoadAssetAsync<GameObject>(playerData.res);
            await handle.ToUniTask();
            Go = handle.InstantiateSync();
            Go.transform.SetParent(Go.transform);
            Go.transform.position = playerData.pos;
            Map.Camera.SetFollow(Go.transform);
            Map.Camera.SetLookAt(Go.transform);

            Anim = AddComponent<AnimComponent, Animator>(Go.GetComponentInChildren<Animator>());
            Seeker = AddComponent<SeekerComponent, Seeker>(Go.GetComponent<Seeker>());
        }

        protected override void OnDestroy()
        {
            UnityEngine.Object.Destroy(Go);
            playerData = null;
        }

        private Vector3 _postion;

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