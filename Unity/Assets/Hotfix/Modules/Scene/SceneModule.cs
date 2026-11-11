using Cysharp.Threading.Tasks;
using Ux;
using UnityEngine;
using System.Collections.Generic;

namespace Ux
{
    [Module]
    public class SceneModule : ModuleBase<SceneModule>
    {        
        public World World { get; private set; }

        protected override void OnInit()
        {
            base.OnInit();
            // 网络消息显式绑定，替代原来的 [Net] 方法特性扫描。
            NetMgr.Ins.Bind<Pb.BcstUnitIntoView>(Pb.BCST.Bcst_UnitIntoView, this, _BcstUnitIntoView);
            NetMgr.Ins.Bind<Pb.BcstUnitOutofView>(Pb.BCST.Bcst_UnitOutofView, this, _BcstUnitOutofView);
            NetMgr.Ins.Bind<Pb.BcstUnitMove>(Pb.BCST.Bcst_UnitMove, this, _BcstUnitMove);
            NetMgr.Ins.Bind<Pb.BcstUnitUpdatePosition>(Pb.BCST.Bcst_UnitUpdatePosition, this, _BcstUnitUpdatePosition);
        }

        protected override void OnRelease()
        {
            // NetMgr 用的是独立的事件系统，不走 ModuleBase 的 OffTag，这里显式解绑。

            World.Destroy();
            World = null;
        }
        public async UniTask EnterScene(string mapName)
        {
            if (World == null)
            {
                World = Entity.Create<World>();
            }
            var path = string.Format(PathHelper.Res.Prefab, mapName);
            var go = await ResMgr.Ins.LoadGameObjectAsync(path);
            var map = World.Add<Scene, GameObject>(go);
            World.EnterScene(map);

            var data = new PlayerData();
            data.data = LoginModule.Ins.resp.Self;
            data.self = true;
            data.name = "name_" + data.data.roleId;
            data.res = string.Format(PathHelper.Res.Prefab, "Role_Dummy");
            data.combatProfileName = "Role_DummyCombatProfile";
            map.AddPlayer(data);

            //foreach (var other in resp.Others)
            //{
            //    if (other == null) continue;
            //    var data2 = new PlayerData();
            //    data2.self = false;
            //    data2.data = other;
            //    data2.name = "name_" + other.roleId;
            //    data2.res = "Hero_CK";
            //    map.AddPlayer(data2);
            //}
        }

        public void LeaveScene()
        {
            World.LeaveScene();
        }

        #region 移动相关
        public void SendMove(List<Vector3> points)
        {
            var req = new Pb.C2SMove();
            var resp = new Pb.BcstUnitMove() { roleId = 1, pointIndex = 0 };
            foreach (var point in points)
            {
                req.Points.Add(new Pb.Vector3() { X = point.x, Y = point.y, Z = point.z });
                resp.Points.Add(new Pb.Vector3() { X = point.x, Y = point.y, Z = point.z });
            }
            //NetMgr.Ins.Send(Pb.CS.C2S_Move, req);
            _BcstUnitMove(resp);
        }

        /// <summary>
        /// 移动链路中转：把方向输入包成"服务器广播格式"，再抛回本地事件。
        ///
        /// 这里刻意复用 Pb.BcstUnitMove 而不是自定义本地结构 —— 本地玩家和远端玩家的移动，
        /// 走的是同一套消息、同一个事件、同一段处理代码，区别只在于数据来源
        /// （本地来自摇杆，远端来自 _BcstUnitMove 的网络回调）。
        /// 等接上真实网络时，只需把下面被注释的 Send 打开，整条逻辑不用动。
        ///
        /// 注意 roleId 目前写死 1：只支持自己移动。多单位要改成传真实 roleId。
        /// </summary>
        public void SendMove(Vector2 vector2)
        {
            var resp = new Pb.BcstUnitMove() { roleId = 1, pointIndex = 0 };
            // Vector2 的 (x, y) 映射到世界坐标的 (X, Z)，Y 恒为 0。
            resp.Points.Add(new Pb.Vector3() { X = vector2.x,Y = 0, Z = vector2.y });
            EventMgr.Ins.Run(EventType.UNIT_MOVE, resp);
        }
        #endregion

        #region 广播相关


        void _BcstUnitIntoView(Pb.BcstUnitIntoView param)
        {
            EventMgr.Ins.Run(EventType.UNIT_INTO_VIEW, param);
        }

        void _BcstUnitOutofView(Pb.BcstUnitOutofView param)
        {
            EventMgr.Ins.Run(EventType.UNIT_OUTOF_VIEW, param);
        }

        void _BcstUnitMove(Pb.BcstUnitMove param)
        {
            EventMgr.Ins.Run(EventType.UNIT_MOVE, param);
        }

        void _BcstUnitUpdatePosition(Pb.BcstUnitUpdatePosition param)
        {
            EventMgr.Ins.Run(EventType.UNIT_UPDATE_POSITION, param);
        }
        #endregion
    }
}
