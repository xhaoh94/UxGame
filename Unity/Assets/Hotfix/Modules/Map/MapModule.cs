using Cysharp.Threading.Tasks;
using Ux;
using UnityEngine;

namespace Ux
{
    public sealed class World : Entity
    {
        public static World Ins { get; private set; }

        public World()
        {
            Ins = this;
        }
    }


    [Module]
    public class MapModule : ModuleBase<MapModule>
    {
        private Map map;

        //private Player self;
        protected override void OnInit()
        {
            base.OnInit();
            Entity.Create<World>();
            Log.Debug("xxxxxxxxxxxxxxxxxxxxxxxxx");
        }


        public async void EnterMap(string mapName)
        {
            var handle = ResMgr.Instance.LoadAssetAsync<GameObject>(mapName);
            await handle.ToUniTask();
            var go = handle.InstantiateSync();
            handle.Release();
            var newMap = World.Ins.AddChild<Map, GameObject>(go);
            if (map != null)
            {
                World.Ins.RemoveChild(map);
            }

            map = newMap;

            var pos = new Vector3(UnityEngine.Random.Range(-3, 3), 0.5f, UnityEngine.Random.Range(-3, 3));
            var data = new PlayerData();
            data.id = 1;
            data.name = "name_" + data.id;
            data.res = "Hero_CK";
            data.pos = pos;
            map.AddPlayer(data);
        }

        protected override void OnRelease()
        {
            World.Ins.Destroy();
            Log.Debug("xxxxxxxxxxxxxxxxxxxxxxxxx1111");
        }
    }
}