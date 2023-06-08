using UnityEngine;

namespace Ux
{
    public static class StringEx
    {
        public static int ToHash(this string str)
        {
            return Animator.StringToHash(str);
        }
    }
}