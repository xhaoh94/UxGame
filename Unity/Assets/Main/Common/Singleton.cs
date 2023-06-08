using Ux;
using System;
using UnityEngine;

public class Singleton<T> where T : class, new()
{
    #region singleton

    private static T _ins;

    public static T Ins
    {
        get
        {
            if (_ins != null) return _ins;
            try
            {
                _ins = new T();
                (_ins as Singleton<T>).OnInit();
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

            return _ins;
        }
    }

    protected virtual void OnInit()
    {

    }
    #endregion
}