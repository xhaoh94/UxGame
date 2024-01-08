using UnityEngine;

namespace Ux
{
    public interface IUnitVision
    {
        FogOfWarComponent FogOfWar { get; }
        Vector3 Position { get; }
        int Layer { get; set; }
    }
    /// <summary>
    /// 单位视野
    /// 通常每个单位有一个对应的单位视野
    /// 部分复杂单位，如建筑，会有多个单位视野
    /// </summary>
    public class UnitVisionConponent : Entity, IAwakeSystem<IUnitVision>
    {
        /// <summary>
        /// 表示玩家分组的位掩码
        /// 如玩家0是0001，玩家1是0010，则这两个玩家共同的视野是0011
        /// </summary>
        public int Mask { get; private set; } = 1;

        /// <summary>
        /// 视野范围
        /// </summary>
        public float Range { get; private set; } = 3;

        /// <summary>
        /// 单位的所在海拔，用于阻挡视线
        /// </summary>
        public short TerrainHeight { get; private set; }

        /// <summary>
        /// 单位所在的草丛
        /// </summary>
        public short GrassId { get; private set; }

        public IUnitVision Unit { get; private set; }
        public void OnAwake(IUnitVision a)
        {
            Unit = a;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Unit == null) return;
            if (Unit.FogOfWar == null) return;
            Unit.FogOfWar.RemoveUnit(this);
            Unit = null;
            Mask = 0;
            Range = 3;
            TerrainHeight = 0;
            GrassId = 0;
        }

        public Vector2 WorldPos
        {
            get { return Unit.Position.XZ(); }
        }


        public void UpdateUnit(Vector3 position)
        {
            if (Unit == null) return;
            if (Unit.FogOfWar == null) return;
            Unit.FogOfWar.TerrainGrid.GetData(position, out short altitude, out short grassId);
            TerrainHeight = altitude;
            GrassId = grassId;
        }
    }
}
