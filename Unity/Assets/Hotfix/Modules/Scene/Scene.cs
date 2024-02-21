﻿using Ux;
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
        Dictionary<uint, Unit> players = new Dictionary<uint, Unit>();
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
            //Log.Debug("创建Unit" + playerData.id);
            var player = AddChild<Unit, PlayerData>(playerData.id, playerData);
            players.Add(playerData.id, player);
        }

        [Evt(EventType.UNIT_MOVE)]
        void _OnUnitMove(Pb.BcstUnitMove param)
        {
            //Log.Debug("移动Unit" + param.roleId);
            var player = GetChild<Unit>(param.roleId);
            if (player != null)
            {                
                player.Path.SetPoints(param.Points, param.pointIndex);
            }
        }
        [Evt(EventType.UNIT_UPDATE_POSITION)]
        void _OnUnitUpdatePosition(Pb.BcstUnitUpdatePosition param)
        {
            var player = GetChild<Unit>(param.roleId);
            if (player != null)
            {
                player.Position = new Vector3(param.Point.X, param.Point.Y, param.Point.Z);
            }
        }

        [Evt(EventType.UNIT_INTO_VIEW)]
        void _OnUnitIntoView(Pb.BcstUnitIntoView param)
        {
            foreach (var role in param.Roles)
            {
                var player = GetChild<Unit>(role.roleId);
                if (player != null) continue;                
                var data = new PlayerData();
                data.data = role;
                data.self = false;
                data.name = "name_" + role.roleId;
                data.res = "Hero_ZS";
                AddPlayer(data);
            }
        }
        [Evt(EventType.UNIT_OUTOF_VIEW)]
        void _OnUnitOutofView(Pb.BcstUnitOutofView param)
        {
            foreach (var role in param.Roles)
            {
                RemoveChild(role);
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