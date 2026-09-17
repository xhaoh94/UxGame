using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class PathComponent : Entity, IAwakeSystem
    {
        public List<Vector3> Points { get; set; }
        public int PathIndex { get; set; }

        /// <summary>
        /// 遗留字段：只在自己的文件里被写，全项目没有任何地方读它。
        /// 战斗移动只看 MoveVector2，别被这个名字误导成"跑步状态"。
        /// </summary>
        public bool IsRun { get; private set; }

        /// <summary>
        /// 移动链路的输入落点：本帧的移动方向（注意是"方向向量"，不是目标坐标）。
        /// 战斗逻辑层唯一认的移动输入来源：CombatComponent.MoveInput 直接读它。
        /// 零向量 = 不动 = 会被判定成 Idle。
        /// </summary>
        public Vector2 MoveVector2 { get; private set; }
        Unit Unit => Parent as Unit;

        void IAwakeSystem.OnAwake()
        {
            Points = new List<Vector3>();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Points.Clear();
            PathIndex = 0;
            IsRun = false;
        }
        /// <summary>
        /// 移动链路 · 输入落地点：把事件里的坐标数据落成"本帧移动方向"。
        ///
        /// ⚠ 语义已经变了，别按老名字理解：
        ///   旧实现（见下面被注释的代码）是把 points 当**路径点列表**逐个跟随；
        ///   现在只取 points[0] 当**方向向量**，整条路径被忽略。
        ///   所以 SendMove(Vector2) 那条路是对的（传进来的就是方向），
        ///   但 A* 寻路传来的 p.vectorPath 会坏掉 —— points[0] 是路径起点(≈角色当前世界坐标)，
        ///   当方向用会指向奇怪的方向，且通常接近零向量，结果就是"点了没反应"。
        ///   要恢复寻路跟随，得把下面注释掉的路径逻辑接回来。
        /// </summary>
        public void SetPoints(List<Pb.Vector3> points, int moveIndex)
        {
            //PathIndex = moveIndex;
            //Points.Clear();
            //foreach (var point in points)
            //{
            //    Points.Add(new Vector3(point.X, point.Y, point.Z));
            //}
            //IsRun = true;

            if (points == null || points.Count == 0)
            {
                MoveVector2 = Vector2.zero;
                IsRun = false;
                return;
            }

            MoveVector2 = new Vector2(points[0].X, points[0].Z);
            IsRun = MoveVector2.sqrMagnitude > 0.0001f;
        }
        public void Stop()
        {
            IsRun = false;
            MoveVector2 = Vector2.zero;
            PathIndex = 0;
            Points.Clear();
        }
    }
}