﻿using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 单位视野
    /// 通常每个单位有一个对应的单位视野
    /// 部分复杂单位，如建筑，会有多个单位视野
    /// </summary>
    public class UnitVisionComponent : Entity, IAwakeSystem, IEventSystem, IFogOfWarUnit
    {
        public BoolValue Visable => _unit.Visible;
        /// <summary>
        /// 表示玩家分组的位掩码
        /// 如玩家0是0001，玩家1是0010，则这两个玩家共同的视野是0011
        /// </summary>
        public int Mask => _unit.Mask;

        /// <summary>
        /// 视野范围
        /// </summary>
        public float Radius { get; private set; } = 5;

        /// <summary>
        /// 单位的所在海拔，用于阻挡视线
        /// </summary>
        public short Altitude { get; private set; }

        /// <summary>
        /// 单位所在的草丛
        /// </summary>
        public short GrassId { get; private set; }
        /// <summary>
        /// 格子坐标
        /// </summary>
        public Vector2Int TilePos => FogOfWarMgr.Ins.WorldPosToTilePos(_unit.Position);

        Unit _unit => ParentAs<Unit>();

        void IAwakeSystem.OnAwake()
        {            
            FogOfWarMgr.Ins.AddUnit(this);
            if (_unit.IsSelf)
            {
                FogOfWarMgr.Ins.SetVisionMask(_unit.Mask);
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            FogOfWarMgr.Ins.RemoveUnit(this);
            Radius = 3;
            Altitude = 0;
            GrassId = 0;            
        }

        [MainEvt(MainEventType.FOG_OF_WAR_INIT)]
        public void UpdateUnit()
        {
            var ins = FogOfWarMgr.Ins;
            if (ins.IsInit)
            {
                var index = ins.Index(TilePos);
                Altitude = ins.GetAltitude(index);
                GrassId = ins.GetGrass(index);
            }
        }
    }
}
