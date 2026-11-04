using System.Collections;
using UnityEngine;

namespace Ux
{
    public class Layers 
    {
        /// <summary>
        /// 默认
        /// </summary>
        public const string Default = "Default";
        /// <summary>
        /// UI
        /// </summary>
        public const string UI = "UI";
        /// <summary>
        /// 地面
        /// </summary>
        public const string Ground = "Ground";
        /// <summary>
        /// 阻挡
        /// </summary>
        public const string Obstacle = "Obstacle";
        /// <summary>
        /// 隐藏层
        /// </summary>
        public const string Hidden = "Hidden";
        /// <summary>
        /// 战争迷雾
        /// </summary>
        public const string FogOfWar = "FogOfWar";
        /// <summary>
        /// UI模型
        /// </summary>
        public const string RTModel = "RTModel";
        /// <summary>
        /// SSC 直挂 UI 下的 3D 模型（只有 UI 相机渲染，场景相机默认不含它，避免双渲染）
        /// </summary>
        public const string UIModel = "UIModel";
    }
}