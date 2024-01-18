using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Ux
{
    partial class FogOfWarMgr
    {
        #region Altitude
        public void SetAltitude(int index, short value)
        {
            _terrainGrid.SetAltitude(index, value);
        }        
        public short GetAltitude(int index)
        {
            return _terrainGrid.GetAltitude(index);
        }        
        #endregion

        #region Grass
        public void SetGrass(int index, short value)
        {
            _terrainGrid.SetGrass(index, value);
        }       
        public short GetGrass(int index)
        {
            return _terrainGrid.GetGrass(index);
        }        
        #endregion       
    }
}
