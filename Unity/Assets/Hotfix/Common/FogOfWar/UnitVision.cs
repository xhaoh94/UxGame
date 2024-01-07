using UnityEngine;

namespace Ux
{
    public interface IUnitVision
    {
        Vector3 Position { get; }
        int Layer { get; set; }
    }
    /// <summary>
    /// 单位视野
    /// 通常每个单位有一个对应的单位视野
    /// 部分复杂单位，如建筑，会有多个单位视野
    /// </summary>
    public class UnitVision : Entity, IAwakeSystem<IUnitVision>
    {
        /// <summary>
        /// 表示玩家分组的位掩码
        /// 如玩家0是0001，玩家1是0010，则这两个玩家共同的视野是0011
        /// </summary>
        public int Mask { get; set; } = 1;

        /// <summary>
        /// 视野范围
        /// </summary>
        public float Range { get; set; } = 5;

        /// <summary>
        /// 单位的所在海拔，用于阻挡视线
        /// </summary>
        public short TerrainHeight { get; set; }

        /// <summary>
        /// 单位所在的草丛
        /// </summary>
        public short GrassId { get; set; }

        public IUnitVision unit;

        #region get-set
        public Vector2 WorldPos
        {
            get { return unit.Position.XZ(); }
        }

        public void OnAwake(IUnitVision a)
        {
            unit = a;
        }
        public void UpdateUnit(FogOfWarComponent fogOfWar, Vector3 position)
        {
            fogOfWar.TerrainGrid.GetData(position, out short altitude, out short grassId);
            TerrainHeight = altitude;
            GrassId = grassId;
        }
        #endregion
    }
}
