using Ux;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI;

namespace Ux
{
    public class Scene : Entity, IAwakeSystem<GameObject>, IEventSystem
    {
        public CameraComponent Camera { get; private set; }
        public AStarComponent AStar { get; private set; }
        public GameObject Go { get; private set; }
        [EEViewer("玩家")]
        Dictionary<uint, Player> players = new Dictionary<uint, Player>();
        public void OnAwake(GameObject a)
        {
            Go = a;
            SetMono(Go);
            Camera = AddComponent<CameraComponent>();
            AStar = AddComponent<AStarComponent, AstarPath>(Go.GetOrAddComponent<AstarPath>());
            AddComponent<FogOfWarComponent>();
        }

        public void AddPlayer(PlayerData playerData)
        {
            var player = AddChild<Player, PlayerData>(playerData.id, playerData);
            players.Add(playerData.id, player);
        }

        [Evt(EventType.UNIT_MOVE)]
        void _OnUnitMove(Pb.BcstUnitMove param)
        {
            var player = GetChild<Player>(param.roleId);
            if (player != null)
            {
                player.Seeker.SetPoints(param.Points, param.pointIndex);
            }
        }
        [Evt(EventType.UNIT_UPDATE_POSITION)]
        void _OnUnitUpdatePosition(Pb.BcstUnitUpdatePosition param)
        {
            var player = GetChild<Player>(param.roleId);
            if (player != null)
            {
                player.Position = new Vector3(param.Point.X, param.Point.Y, param.Point.Z);
            }
        }

        [Evt(EventType.UNIT_INTO_VIEW)]
        void _OnUnitIntoView(Pb.BcstUnitIntoView param)
        {
            var data = new PlayerData();
            data.data = param.Role;
            data.self = false;
            data.name = "name_" + data.data.roleId;
            data.res = "Hero_CK";
            AddPlayer(data);
        }
        [Evt(EventType.UNIT_OUTOF_VIEW)]
        void _OnUnitOutofView(Pb.BcstUnitOutofView param)
        {
            RemoveChild(param.roleId);
        }

        protected override void OnDestroy()
        {
            UnityPool.Push(Go);
            players.Clear();
            Camera = null;
            AStar = null;
        }
    }
}