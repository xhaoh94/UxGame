using System;
using UnityEngine;

namespace Ux
{
    public static class GameObjectEx
    {
        public static void SetVisible(this GameObject go, bool visible)
        {
            if (go.activeSelf != visible)
                go.SetActive(visible);
        }

        public static void SetParent(this GameObject go, Transform parent, bool worldPositionStays = true)
        {
            go.transform.SetParent(parent, worldPositionStays);
        }
        public static void SetParent(this GameObject go, GameObject parent, bool worldPositionStays = true)
        {
            go.transform.SetParent(parent.transform, worldPositionStays);
        }
        
        private static readonly System.Collections.Generic.List<Transform> LayerBuffer =
            new System.Collections.Generic.List<Transform>(64);

        public static void SetLayer(this GameObject go, string layerName, bool includeChild = true)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Log.Error("SetLayer 找不到层：{0}", layerName);
                return;
            }

            SetLayer(go, layer, includeChild);
        }

        public static void SetLayer(this GameObject go, int layer, bool includeChild = true)
        {
            if (go == null)
                return;

            if (go.layer != layer)
                go.layer = layer;

            if (!includeChild)
                return;

            LayerBuffer.Clear();            
            go.GetComponentsInChildren(true, LayerBuffer);
            for (int i = 0; i < LayerBuffer.Count; i++)
            {
                var child = LayerBuffer[i].gameObject;
                if (child.layer != layer)
                    child.layer = layer;
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