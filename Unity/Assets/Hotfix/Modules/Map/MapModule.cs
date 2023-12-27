using Cysharp.Threading.Tasks;
using Ux;
using UnityEngine;

namespace Ux
{    
    [Module]
    public class MapModule : ModuleBase<MapModule>
    {
        public World World { get; private set; }

        public async UniTask EnterMap(string mapName)
        {
            if (World == null)
            {
                World = Entity.Create<World>();
            }
            var go = await ResMgr.Ins.LoadAssetAsync<GameObject>(mapName);
            var map = World.AddChild<Map, GameObject>(go);
            World.EnterMap(map);
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
            World.ExitMap();
        }

        protected override void OnRelease()
        {
            World.Destroy();
            World = null;
        }
    }
}