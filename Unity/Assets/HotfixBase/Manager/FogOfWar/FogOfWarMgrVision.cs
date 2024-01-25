using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    partial class FogOfWarMgr
    {
        public bool IsVisible(IFogOfWarUnit unit)
        {
            if ((_visionMask & unit.Mask) > 0)
                return true;

            var index = Index(unit.TilePos);
            if (IsVisible(index, _visionMask))
                return true;
            if (_wasVision && WasVisible(index, _visionMask))
                return true;

            return false;
        }

        public bool IsVisible(int index, int entityMask)
        {
            return _visionGrid.IsVisible(index, entityMask);
        }

        public bool WasVisible(int index, int entityMask)
        {
            return  _visionGrid.WasVisible(index, entityMask);
        }
    }
}
