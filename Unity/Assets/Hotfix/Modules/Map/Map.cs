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
            Camera = AddComponent<CameraComponent>();
            AddComponent<AStarComponent, AstarPath>(Go.GetOrAddComponent<AstarPath>());
        }
        [EEViewer("测试")]
        Dictionary<int, PlayerData> playerData = new Dictionary<int, PlayerData>()
        {
            { 1,new PlayerData(){
                id = 1,
                res = "xxx1",
                pos = new Vector3(100, 100, 100),
                name = "ccc1",
            }},
            { 2,new PlayerData(){
                id = 2,
                res = "xxx",
                pos = new Vector3(100, 100, 100),
                name = "ccc",
            }}
        };

        [EEViewer("测试1")]
        List< PlayerData> playerData2 = new List<PlayerData>()
        {
            new PlayerData(){
                id = 1,
                res = "xxx1",
                pos = new Vector3(100, 100, 100),
                name = "ccc1",
            },
            new PlayerData(){
                id = 2,
                res = "xxx",
                pos = new Vector3(100, 100, 100),
                name = "ccc",
            }
        };
        public void AddPlayer(PlayerData playerData)
        {            
            var player = AddChild<Player, PlayerData>(playerData.id, playerData);
            players.Add(playerData.id, player);
        }

        protected override void OnDestroy()
        {
            UnityEngine.Object.Destroy(Go);
            players.Clear();
        }
    }
}