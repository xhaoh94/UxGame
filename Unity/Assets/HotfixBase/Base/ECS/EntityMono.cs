using System.ComponentModel;
using UnityEngine;

namespace Ux
{
    public interface IEntityMono
    {
#if UNITY_EDITOR
        void SetEntity(Entity entity, GameObject go);
#else
        void SetEntity(Entity entity);
#endif
    }
    public class EntityMono : MonoBehaviour, IEntityMono
    {
        public Entity Entity { get; private set; }

        public T GetEntity<T>() where T : Entity
        {
            if (Entity == null) return null;
            return Entity as T;
        }

#if UNITY_EDITOR
        [SerializeField] GameObject GoViewer;
        public void SetEntity(Entity entity, GameObject goViewer)
        {
            Entity = entity;
            GoViewer = goViewer;
        }
#else
        public void SetEntity(Entity entity)
        {
            Entity = entity;
        }
#endif
    }
}
