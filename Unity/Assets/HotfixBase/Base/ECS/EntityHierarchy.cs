#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ux
{

    public class EntityHierarchy : MonoBehaviour
    {
        const string _location = "$ECS_EMPTY_VIEWER$";
        static Transform _ecs;
        EEInfos infos;

        public void SetParent(Transform _parent)
        {
            transform.SetParent(_parent);
        }
        public void Layout()
        {
            infos?.Layout();
        }

        public static EntityHierarchy Create(Entity entity = null)
        {
            var viewer = UnityPool.Get(_location, () => new GameObject())
                .GetOrAddComponent<EntityHierarchy>();
            if (entity != null)
            {
                viewer.infos = new EEInfos(entity, true);
            }
            if (Application.isPlaying)
            {
                if (_ecs == null)
                {
                    _ecs = new GameObject("ECS").transform;
                    DontDestroyOnLoad(_ecs);
                }
                viewer.SetParent(_ecs);
            }
            return viewer;
        }  

        public void Release()
        {
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }

            Destroy(this);
            UnityPool.Push(_location, gameObject);
        }
    }
}

#endif