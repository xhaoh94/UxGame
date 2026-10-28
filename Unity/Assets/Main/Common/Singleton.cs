using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// 单例基类。
/// 这里做三件在项目里被反复踩到的事：
/// 1. 构造期间禁止重入（构造函数里间接访问 Ins 会拿到半初始化实例，甚至造出第二个）；
/// 2. 只有构造成功才发布实例，构造抛异常时不会留下一个坏掉的一半对象；
/// 3. 提供 Reset，用于关闭 Domain Reload 的编辑器环境下复位状态。
/// </summary>
public class Singleton<T> where T : class, new()
{
    #region singleton

    private static T _ins;
    private static int _creating;

    public static T Ins
    {
        get
        {
            var current = Volatile.Read(ref _ins);
            if (current != null)
            {
                return current;
            }

            if (Interlocked.CompareExchange(ref _creating, 1, 0) != 0)
            {
                // 构造函数里又访问了 Ins：此时 _ins 还是 null，继续往下走会再造一个实例。
                Debug.LogError($"{typeof(T).Name} 正在构造中，禁止在构造函数里访问 Ins（会造成重入和半初始化实例）。");
                throw new InvalidOperationException($"{typeof(T).Name} 构造期间发生了单例重入。");
            }

            try
            {
                var created = new T();
                // 先发布实例再回调 OnCreated：OnCreated 里访问 Ins 是合法的。
                Volatile.Write(ref _ins, created);
                (created as Singleton<T>)?.OnCreated();
                return created;
            }
            catch (MissingMethodException ex)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
#endif
                throw new Exception($"{ex.Message}(单例模式，构造函数报错){typeof(T).Name}");
            }
            finally
            {
                Volatile.Write(ref _creating, 0);
            }
        }
    }

    /// <summary>
    /// 实例是否已经创建（不会触发创建）。
    /// </summary>
    public static bool IsCreated => Volatile.Read(ref _ins) != null;

    /// <summary>
    /// 清空单例实例。
    /// 仅编辑器/开发版本可用：关闭 Domain Reload（Enter Play Mode Options）时，
    /// 静态字段不会自动复位，需要在启动/退出流程里显式调用，否则第二次 Play 会拿到上次的残留实例。
    /// </summary>
    public static void Reset()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Volatile.Write(ref _ins, null);
        Volatile.Write(ref _creating, 0);
#endif
    }

    protected virtual void OnCreated()
    {

    }
    #endregion
}
