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
        }

        public async UniTask EnterMap(string mapName)
        {
            Log.Debug("EnterMap001-LoadAssetAsync");
            var handle = ResMgr.Ins.LoadAssetAsync<GameObject>(mapName);
            await handle.ToUniTask();
            Log.Debug("EnterMap001-InstantiateSync");
            var go = handle.InstantiateSync();
            handle.Release();
            Log.Debug("EnterMap001-World.Ins.AddChild");
            var newMap = World.Ins.AddChild<Map, GameObject>(go);
            if (map != null)
            {
                World.Ins.RemoveChild(map);
            }

            map = newMap;
            Log.Debug("EnterMap001-New PlayerData");
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
        }
    }
}