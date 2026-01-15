using Ux;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI;

namespace Ux
{
    public class Scene : Entity, IAwakeSystem<GameObject>
    {
        public CameraComponent Camera { get; private set; }
        public AStarComponent AStar { get; private set; }
        public GameObject Go { get; private set; }
        [EEViewer("玩家")]
        Dictionary<uint, Unit> players = new Dictionary<uint, Unit>();
        public void OnAwake(GameObject a)
        {
            Go = a;
            Link(Go);
            Camera = Add<CameraComponent>();
            AStar = Add<AStarComponent, AstarPath>(Go.GetOrAddComponent<AstarPath>());
            EventMgr.Ins.On<Pb.BcstUnitMove>(EventType.UNIT_MOVE, this, _OnUnitMove);
            EventMgr.Ins.On<Pb.BcstUnitUpdatePosition>(EventType.UNIT_UPDATE_POSITION, this, _OnUnitUpdatePosition);
            EventMgr.Ins.On<Pb.BcstUnitIntoView>(EventType.UNIT_INTO_VIEW, this, _OnUnitIntoView);
            EventMgr.Ins.On<Pb.BcstUnitOutofView>(EventType.UNIT_OUTOF_VIEW, this, _OnUnitOutofView);
            //AddComponent<FogOfWarComponent>();
        }

        public void AddPlayer(PlayerData playerData)
        {
            //Log.Debug("创建Unit" + playerData.id);
            var player = Add<Unit, PlayerData>(playerData.id, playerData);
            players.Add(playerData.id, player);
        }

        void _OnUnitMove(Pb.BcstUnitMove param)
        {
            //Log.Debug("移动Unit" + param.roleId);
            var player = Get<Unit>(param.roleId);
            if (player != null)
            {                
                player.Path.SetPoints(param.Points, param.pointIndex);
            }
        }        
        void _OnUnitUpdatePosition(Pb.BcstUnitUpdatePosition param)
        {
            var player = Get<Unit>(param.roleId);
            if (player != null)
            {
                player.Position = new Vector3(param.Point.X, param.Point.Y, param.Point.Z);
            }
        }

        void _OnUnitIntoView(Pb.BcstUnitIntoView param)
        {
            foreach (var role in param.Roles)
            {
                var player = Get<Unit>(role.roleId);
                if (player != null) continue;                
                var data = new PlayerData();
                data.data = role;
                data.self = false;
                data.name = "name_" + role.roleId;
                data.res = "Hero_ZS";
                AddPlayer(data);
            }
        }
        void _OnUnitOutofView(Pb.BcstUnitOutofView param)
        {
            foreach (var role in param.Roles)
            {
                Remove(role);
                players.Remove(role);
            }
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