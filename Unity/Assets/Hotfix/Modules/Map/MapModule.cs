using Cysharp.Threading.Tasks;
using Ux;
using UnityEngine;
using System.Collections.Generic;

namespace Ux
{
    [Module]
    public class MapModule : ModuleBase<MapModule>
    {
        public World World { get; private set; }

        public async UniTask EnterMap(string mapName, Pb.S2CEnterMap resp)
        {
            if (World == null)
            {
                World = Entity.Create<World>();
            }
            var go = await ResMgr.Ins.LoadAssetAsync<GameObject>(mapName);
            var map = World.AddChild<Map, GameObject>(go);
            World.EnterMap(map);

            var data = new PlayerData();
            data.data = resp.Self;
            data.self = true;
            data.name = "name_" + data.data.roleId;
            data.res = "Hero_CK";
            map.AddPlayer(data);

            foreach (var other in resp.Others)
            {
                if (other == null) continue;
                var data2 = new PlayerData();
                data2.self = false;
                data2.data = other;
                data2.name = "name_" + other.roleId;
                data2.res = "Hero_CK";
                map.AddPlayer(data2);
            }
        }

        public void SendMove(List<Vector3> points)
        {
            var req = new Pb.C2SMove();
            foreach (var point in points)
            {
                req.Points.Add(new Pb.Vector3() { X = point.x, Y = point.y, Z = point.z });
            }
            NetMgr.Ins.Send(Pb.CS.C2S_Move, req);
        }

        [Net(Pb.BCST.Bcst_Move)]
        void _BcstMove(Pb.BcstMove param)
        {
            EventMgr.Ins.Send(EventType.EntityMove, param);
        }

        [Net(Pb.BCST.Bcst_EnterMap)]
        void _BcstLeaveMap(Pb.BcstEnterMap param)
        {
            EventMgr.Ins.Send(EventType.EntityEnterVision, param);
            
        }

        [Net(Pb.BCST.Bcst_LeaveMap)]
        void _BcstLeaveMap(Pb.BcstLeaveMap param)
        {
            EventMgr.Ins.Send(EventType.EntityLeaveVision, param);
        }
        public void ExitMap()
        {
            World.ExitMap();
        }

        protected override void OnRelease()
        {
            World.Destroy();
            World = null;
        }
    }
}
