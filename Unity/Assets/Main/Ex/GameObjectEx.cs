using System;
using UnityEngine;

namespace Ux
{
    public static class GameObjectEx
    {
        public static void Visable(this GameObject go, bool b)
        {
            if (go != null && go.activeInHierarchy != b)
            {
                go.SetActive(b);
            }
        }
        public static void SetParent(this GameObject go, Transform parent, bool worldPositionStays = true)
        {
            go.transform.SetParent(parent, worldPositionStays);
        }
        public static void SetLayer(this GameObject go, int layer, bool includeChild = true)
        {
            go.layer = layer;
            if (includeChild)
            {
                foreach (var tran in go.GetComponentsInChildren<Transform>())
                {
                    tran.gameObject.layer = layer;
                }
            }
        }
        public static void SetTag(this GameObject go, string tag, bool includeChild = true)
        {
            go.tag = tag;
            if (includeChild)
            {
                foreach (var tran in go.GetComponentsInChildren<Transform>())
                {
                    tran.gameObject.tag = tag;
                }
            }
        }

        public static T Get<T>(this GameObject gameObject, string key) where T : class
        {
            try
            {
                return gameObject.GetComponent<ReferenceCollector>()?.Get<T>(key);
            }
            catch (Exception e)
            {
                throw new Exception($"获取{gameObject.name}的ReferenceCollector key失败, key: {key}", e);
            }
        }

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}