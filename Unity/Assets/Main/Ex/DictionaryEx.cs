using System;
using System.Collections;
using System.Collections.Generic;

namespace Ux
{
    public static class DictionaryEx
    {
        public static void ForEachKey<TKey, TValue>(this Dictionary<TKey, TValue> dict, Func<TKey, bool> fn)
        {
            if (dict.Count == 0) return;
            foreach (var _key in dict.Keys)
            {
                if (fn.Invoke(_key)) break;
            }
        }
        public static void ForEachKey<TKey, TValue>(this Dictionary<TKey, TValue> dict, Action<TKey> fn)
        {
            if (dict.Count == 0) return;
            foreach (var _key in dict.Keys)
            {
                fn.Invoke(_key);
            }
        }
        public static IEnumerator ForEachKey<TKey, TValue>(this Dictionary<TKey, TValue> dict, Func<TKey, IEnumerator> fn)
        {
            if (dict.Count == 0) yield break;
            foreach (var _key in dict.Keys)
            {
                yield return fn.Invoke(_key);
            }
        }

        public static IEnumerator ForEachKey<TKey, TValue>(this Dictionary<TKey, TValue> dict, FuncEx<TKey, bool, IEnumerator> fn)
        {
            if (dict.Count == 0) yield break;
            foreach (var _key in dict.Keys)
            {
                yield return fn.Invoke(_key, out var isBreak);
                if (isBreak) break;
            }
        }
        public static void ForEachValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, Action<TValue> fn)
        {
            if (dict.Count == 0) return;
            foreach (var _value in dict.Values)
            {
                fn.Invoke(_value);
            }
        }
        public static void ForEachValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, Func<TValue, bool> fn)
        {
            if (dict.Count == 0) return;
            foreach (var _value in dict.Values)
            {
                if (fn.Invoke(_value)) break;
            }
        }
        public static IEnumerator ForEachValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, Func<TValue, IEnumerator> fn)
        {
            if (dict.Count == 0) yield break;
            foreach (var _value in dict.Values)
            {
                yield return fn.Invoke(_value);
            }
        }
        public static IEnumerator ForEachValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, FuncEx<TValue, bool, IEnumerator> fn)
        {
            if (dict.Count == 0) yield break;
            foreach (var _value in dict.Values)
            {
                yield return fn.Invoke(_value, out var isBreak);
                if (isBreak) break;
            }
        }

    }
}