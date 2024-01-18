using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Ux;

public static class UnityPool
{
    private static readonly Dictionary<string, Queue<UnityEngine.Object>> _unity = new Dictionary<string, Queue<UnityEngine.Object>>();
    static Transform _pool_content;
    public static UnityEngine.Object Get(string location)
    {
        if (!_unity.TryGetValue(location, out var queue) || queue.Count == 0)
        {
            return null;
        }
        else
        {
            var obj = queue.Dequeue();
            if (obj is GameObject go)
            {
                go.Visable(true);
            }
            return obj;
        }
    }
    public static T Get<T>(string location) where T : UnityEngine.Object
    {
        return (T)Get(location);
    }


    public static void Push(UnityEngine.GameObject obj)
    {
        var mono = obj.GetComponent<ResHandleMono>();
        if (mono == null)
        {
#if UNITY_EDITOR
            throw new Exception("对象池添加了非ResHandleMono对象");
#else
            return;
#endif
        }
        var location = mono.Location;
        Push(location, obj);
    }

    public static void Push(string location, UnityEngine.Object obj)
    {
        if (!_unity.TryGetValue(location, out var queue))
        {
            queue = new Queue<UnityEngine.Object>();
            _unity.Add(location, queue);
        }
#if UNITY_EDITOR
        if (queue.Contains(obj))
        {
            throw new Exception("对象池重复添加！请检查代码");
        }
#endif
        if (obj is GameObject go)
        {
#if UNITY_EDITOR
            if (_pool_content == null)
            {
                _pool_content = new GameObject("[UnityPool]").transform;
                UnityEngine.Object.DontDestroyOnLoad(_pool_content);
            }
            go.transform.parent = _pool_content;
#else
            go.transform.parent = null;
#endif
            go.Visable(false);
        }
        queue.Enqueue(obj);
    }

    public static void Clear()
    {
        foreach (var kv in _unity)
        {
            foreach (var obj in kv.Value)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }
        _unity.Clear();
    }

}
