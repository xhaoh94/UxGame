using System.Collections;
using UnityEngine;

namespace Ux
{
    public interface IUnitVision
    {
        int Layer { set; }
        /// <summary>
        /// 表示玩家分组的位掩码
        /// 如玩家0是0001，玩家1是0010，则这两个玩家共同的视野是0011
        /// 如果有多个不同视野，则以2的N次方叠加 例如 视野1：0001、视野2：0010、视野3：0100
        /// </summary>
        int Mask { get; }
        float Radius { get; }
        /// <summary>
        /// 海拔高度
        /// </summary>
        short Altitude { get; }
        /// <summary>
        /// 草丛
        /// </summary>
        short GrassId { get; }
        Vector2Int TilePos { get; }
    }
}