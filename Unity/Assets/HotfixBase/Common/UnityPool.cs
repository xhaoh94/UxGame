using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ux;

public static class UnityPool
{
    private static readonly Dictionary<string, Queue<UnityEngine.Object>> _unity = new Dictionary<string, Queue<UnityEngine.Object>>();
    static Transform _pool_content;

    /// <summary>
    /// 池根：DontDestroyOnLoad，池对象跨场景存活。
    /// 不能把对象丢到 parent = null（会落在当前场景根，切场景被销毁）。
    /// </summary>
    public static Transform PoolContent
    {
        get
        {
            if (_pool_content == null)
            {
                _pool_content = new GameObject("[UnityPool]").transform;
                UnityEngine.Object.DontDestroyOnLoad(_pool_content);
            }
            return _pool_content;
        }
    }
#if UNITY_EDITOR
    // 编辑器下按 location 分子目录，只是为了方便在 Hierarchy 里翻。
    static Dictionary<string, Transform> _pool_location_content = new Dictionary<string, Transform>();

    static Transform _hidden_ui_root;

    /// <summary>
    /// 编辑器下隐藏界面的收纳节点（[UnityPool]/[UI]）。
    /// 只是为了让 Hierarchy 里能一眼看出哪些界面还在显示，运行逻辑不依赖它。
    /// </summary>
    public static Transform HiddenUIRoot
    {
        get
        {
            if (_hidden_ui_root == null)
            {
                _hidden_ui_root = new GameObject("[UI]").transform;
                _hidden_ui_root.SetParent(PoolContent, false);
            }
            return _hidden_ui_root;
        }
    }
#endif
    public static void Init()
    {
        GameMain.Ins.LowMemory += Clear;        
    }
    /// <summary>
    /// 从池里取对象。给了 <paramref name="parent"/> 就直接挂到目标父级：
    /// 跨场景的 SetParent 会顺带把对象搬进父级所在场景，省掉 MoveGameObjectToScene 和调用方再 SetParent。
    /// </summary>
    public static T Get<T>(string location, Transform parent = null, Func<T> create = null) where T : UnityEngine.Object
    {
        if (!_unity.TryGetValue(location, out var queue) || queue.Count == 0)
        {
            if (create != null)
            {
                return CreateWithParent(create, parent);
            }
            return null;
        }
        else
        {
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                if (obj == null)
                {
                    continue;
                }
                if (obj is GameObject go)
                {
                    if (parent != null)
                    {
                        go.transform.SetParent(parent, false);
                        go.SetVisible(true);
                        return obj as T;
                    }

                    go.transform.SetParent(null);
                    SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
                    go.SetVisible(true);
                }
                return obj as T;
            }
            return create != null ? CreateWithParent(create, parent) : null;
        }
    }

    private static T CreateWithParent<T>(Func<T> create, Transform parent) where T : UnityEngine.Object
    {
        var created = create.Invoke();
        if (parent != null && created is GameObject go)
        {
            go.transform.SetParent(parent, false);
        }
        return created;
    }

    public static void Push(UnityEngine.GameObject obj)
    {
        if (obj == null)
        {
            Log.Error("将Null的UnityEngine.GameObject 放入了对象池!");
            return;
        }
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
        if (obj == null)
        {
            Log.Error("将Null的UnityEngine.Object 放入了对象池!");
            return;
        }

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
            var entityMono = go.GetComponent<EntityViewer>();
            if (entityMono != null)
            {
                UnityEngine.Object.Destroy(entityMono);
            }
#if UNITY_EDITOR
            if (!_pool_location_content.TryGetValue(location, out var locationContent))
            {
                locationContent = new GameObject($"[{location}]").transform;
                locationContent.SetParent(PoolContent);
                _pool_location_content.Add(location, locationContent);
            }
            go.transform.SetParent(locationContent, false);
#else
            go.transform.SetParent(PoolContent, false);
#endif
            go.SetVisible(false);
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
