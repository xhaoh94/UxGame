using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class WayfindingMgr : Singleton<WayfindingMgr>
    {
        public void Test()
        {
            int[,] maps = new int[10,10];
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    maps[i, j] = 1;
                }
            }
            for (int i = 0; i < 9; i++)
            {
                maps[i, 5] = 0;
            }
            var astart = Pool.Get<AStart>();
            astart.Init(maps);
            var path = astart.Find(new Vector2Int(0, 0), new Vector2Int(0, 9));
            Log.Info(path);
        }
    }
}
