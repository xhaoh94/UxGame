using System.Collections;
using UnityEngine;

namespace Ux
{
    public static class VectorEx
    {
        public static Vector2 XZ(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.z);
        }
    }
}