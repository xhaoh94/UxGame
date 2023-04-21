using Ux;
using System;
using UnityEngine;

public class Singleton<T> where T : class, new()
{
    #region singleton

    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance != null) return _instance;
            try
            {
                _instance = new T();
            }
            catch (MissingMethodException ex)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
#endif
                throw new System.Exception($"{ex.Message}(单例模式，构造函数报错){typeof(T).Name}");
            }

            return _instance;
        }
    }

    #endregion
}