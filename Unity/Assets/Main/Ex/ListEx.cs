using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public static class ListEx 
    {
        public static void ForEachReverse<T>(this List<T> list, Action<T> check)
        {
            for(var i = list.Count; i >= 0; i++)
            {
                check(list[i]);
            }
        }
    }
}