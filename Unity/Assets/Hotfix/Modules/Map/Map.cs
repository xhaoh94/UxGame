using Ux;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class Map : Entity, IAwakeSystem<GameObject>
    {
        public CameraComponent Camera { get; private set; }
        public GameObject Go { get; private set; }
        [EEViewer("玩家")]
        Dictionary<int, Player> players = new Dictionary<int, Player>();
        public void OnAwake(GameObject a)
        {
            Go = a;
            SetMono(Go);
            Camera = AddComponent<CameraComponent>();
            AddComponent<AStarComponent, AstarPath>(Go.GetOrAddComponent<AstarPath>());
        }

        public void AddPlayer(PlayerData playerData)
        {            
            var player = AddChild<Player, PlayerData>(playerData.id, playerData);
            players.Add(playerData.id, player);
        }

        protected override void OnDestroy()
        {
            UnityPool.Push(Go);
            //UnityEngine.Object.Destroy(Go);
            players.Clear();
            Camera = null;
        }
    }
}