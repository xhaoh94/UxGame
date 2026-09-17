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
        readonly SortedDictionary<uint, Unit> players = new();
        public long SimulationFrame => SimulationClock.Ins.CurrentFrame;

        public void OnAwake(GameObject a)
        {
            Go = a;
            Link(Go);
            Camera = Add<CameraComponent>();
            AStar = Add<AStarComponent, AstarPath>(Go.GetOrAddComponent<AstarPath>());
            // 移动链路的前置：订阅移动事件。注意：不区分本地/远端，两者共用一个事件。
            EventMgr.Ins.On<Pb.BcstUnitMove>(EventType.UNIT_MOVE, this, _OnUnitMove);
            EventMgr.Ins.On<Pb.BcstUnitUpdatePosition>(EventType.UNIT_UPDATE_POSITION, this, _OnUnitUpdatePosition);
            EventMgr.Ins.On<Pb.BcstUnitIntoView>(EventType.UNIT_INTO_VIEW, this, _OnUnitIntoView);
            EventMgr.Ins.On<Pb.BcstUnitOutofView>(EventType.UNIT_OUTOF_VIEW, this, _OnUnitOutofView);
            // ★ 全项目战斗逻辑的时间起点 ★
            // 逻辑帧时钟一旦跑起来，它会以 60Hz 不断发出"帧号 +1"的事件；
            // CombatMgr 订阅了这个事件，于是 BattleWorld.Tick 被反复调用 ——
            // 上面两条链路真正"跑起来"的那一段（取出命令、判定状态、结算位移）都在那一层里发生。
            // 注意：Unity 的 Update 只负责喂时间，它不参与战斗逻辑。
            var simulationClock = SimulationClock.Ins;
            if (!simulationClock.IsRunning)
            {
                simulationClock.StartLocalRealtime(TimelineAsset.DefaultFrameRate);
            }
            //AddComponent<FogOfWarComponent>();
        }

        public void AddPlayer(PlayerData playerData)
        {
            //Log.Debug("创建Unit" + playerData.id);
            var player = Add<Unit, PlayerData>(playerData.id, playerData);
            players.Add(playerData.id, player);
        }

        /// <summary>
        /// 移动链路中转：按 roleId 找到对应 Unit，把输入交给它的 PathComponent。
        ///
        /// 本地玩家和远端玩家在这里汇合：本地走 SceneModule.SendMove，
        /// 远端走 _BcstUnitMove 的网络回调，但两者发的是同一个事件、进的是同一个方法。
        /// 所以"自己移动"和"看到别人移动"在逻辑层完全等价，表现代码不需要区分。
        /// </summary>
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
            // 逻辑帧由 CombatMgr 分发给战斗世界，单位在 CombatComponent 内自行注册/注销。
            // 这里按场景粒度收口：切场景即结束所有战斗，避免主世界残留旧帧号导致新一轮推进被忽略。
            CombatMgr.Ins.DestroyAllWorlds();
            SimulationClock.Ins.Stop();
            UnityPool.Push(Go);
            players.Clear();
            Camera = null;
            AStar = null;
        }
    }
}