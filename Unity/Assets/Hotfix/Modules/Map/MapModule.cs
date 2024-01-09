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

            var data = new PlayerData();
            data.id = 1;
            data.name = "name_" + data.id;
            data.res = "Hero_CK";
            data.pos = new Vector3(UnityEngine.Random.Range(-3, 3), 0.5f, UnityEngine.Random.Range(-3, 3));
            data.mask = 1;
            map.AddPlayer(data);

            var data2 = new PlayerData();
            data2.id = 2;
            data2.name = "name_" + data2.id;
            data2.res = "Hero_CK";
            data2.pos = new Vector3(UnityEngine.Random.Range(-3, 3), 0.5f, UnityEngine.Random.Range(-3, 3));
            data2.mask = 4;
            map.AddPlayer(data2);
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