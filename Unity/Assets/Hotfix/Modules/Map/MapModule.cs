using Cysharp.Threading.Tasks;
using Ux;
using UnityEngine;

namespace Ux
{
    public sealed class World : Entity
    {
        public static World Ins { get; private set; }
        private Map map;
        public World()
        {
            Ins = this;
        }

        public void EnterMap(Map newMap)
        {
            if (map != null)
            {
                World.Ins.RemoveChild(map);
            }
            map = newMap;
        }
        public void ExitMap()
        {
            if (map != null)
            {
                World.Ins.RemoveChild(map);
            }
        }
    }


    [Module]
    public class MapModule : ModuleBase<MapModule>
    {
        protected override void OnInit()
        {
            base.OnInit();
            Entity.Create<World>();
        }

        public async UniTask EnterMap(string mapName)
        {
            var go = await ResMgr.Ins.LoadAssetAsync<GameObject>(mapName);
            var map = World.Ins.AddChild<Map, GameObject>(go);
            World.Ins.EnterMap(map);
            var pos = new Vector3(UnityEngine.Random.Range(-3, 3), 0.5f, UnityEngine.Random.Range(-3, 3));
            var data = new PlayerData();
            data.id = 1;
            data.name = "name_" + data.id;
            data.res = "Hero_CK";
            data.pos = pos;
            map.AddPlayer(data);
        }
        public void ExitMap()
        {
            World.Ins.ExitMap();
        }

        protected override void OnRelease()
        {
            World.Ins.Destroy();
        }
    }
}