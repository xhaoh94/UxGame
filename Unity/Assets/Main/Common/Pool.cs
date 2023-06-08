using System;
using System.Collections.Generic;

public class Pool
{
    private static readonly Dictionary<Type, Queue<object>> dictionary = new Dictionary<Type, Queue<object>>();

    public static object Get(Type type)
    {
        object obj;
        if (!dictionary.TryGetValue(type, out Queue<object> queue))
        {
            obj = Activator.CreateInstance(type);
        }
        else if (queue.Count == 0)
        {
            obj = Activator.CreateInstance(type);
        }
        else
        {
            obj = queue.Dequeue();
        }

        return obj;
    }

    public static T Get<T>(Type type)
    {
        return (T)Get(type);
    }

    public static T Get<T>()
    {
        return Get<T>(typeof(T));
    }

    public static void Push(object obj)
    {
        Type type = obj.GetType();
        if (!dictionary.TryGetValue(type, out var queue))
        {
            queue = new Queue<object>();
            dictionary.Add(type, queue);
        }
#if UNITY_EDITOR
        if (queue.Contains(obj))
        {
            throw new Exception("对象池重复添加！请检查代码");            
        }
#endif
        queue.Enqueue(obj);
    }

    public static void Clear()
    {
        dictionary.Clear();
    }
}
