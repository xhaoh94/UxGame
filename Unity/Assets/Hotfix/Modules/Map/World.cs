using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class World : Entity
    {
        private Map map;

        public void EnterMap(Map newMap)
        {
            if (map != null)
            {
                RemoveChild(map);
            }
            map = newMap;
        }
        public void ExitMap()
        {
            if (map != null)
            {
                RemoveChild(map);
            }
        }

        protected override void OnDestroy()
        {
            map = null;
        }
    }

}
